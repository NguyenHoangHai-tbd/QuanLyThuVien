using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Domain.Common;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Services;

public sealed class CatalogService : ApplicationServiceBase
{
    public CatalogService(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<BookListItemDto>> SearchBooksAsync(string? search, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var query = Clean(search);
        var books = TenantScope(Repository.Books);

        if (!string.IsNullOrWhiteSpace(query))
        {
            books = books.Where(book =>
                HasText(book.Title, query) ||
                HasText(book.Isbn, query) ||
                HasText(book.Description, query) ||
                book.Tags.Any(tag => HasText(tag, query)) ||
                Names(Repository.Authors, book.AuthorIds).Any(name => HasText(name, query)) ||
                Names(Repository.Categories, book.CategoryIds).Any(name => HasText(name, query)));
        }

        var result = books
            .OrderBy(x => x.Title)
            .Select(book => MapBookListItem(book, branchId))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<BookListItemDto>>(result);
    }

    public Task<BookDto> GetBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = TenantScope(Repository.Books).FirstOrDefault(x => x.Id == id);
        if (book is null)
        {
            throw AppException.NotFound("Book not found.");
        }

        return Task.FromResult(MapBook(book));
    }

    public async Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var title = Clean(request.Title);
        var isbn = Clean(request.Isbn);

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(isbn))
        {
            throw AppException.BadRequest("Book title and ISBN are required.");
        }

        if (TenantScope(Repository.Books).Any(x => x.Isbn.Equals(isbn, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("ISBN already exists in this tenant.");
        }

        var publisher = GetOrCreatePublisher(request.Publisher);
        var authorIds = request.Authors.Select(GetOrCreateAuthor).Select(x => x.Id).Distinct().ToList();
        var categoryIds = request.Categories.Select(GetOrCreateCategory).Select(x => x.Id).Distinct().ToList();
        var tags = request.Tags.Select(Clean).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var book = new Book
        {
            TenantId = TenantId,
            Title = title,
            Isbn = isbn,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? $"AI fallback: Mo ta ngan cho sach {title}."
                : request.Description.Trim(),
            PublishedYear = request.PublishedYear,
            Language = string.IsNullOrWhiteSpace(request.Language) ? CurrentUser.Locale : request.Language.Trim(),
            PublisherId = publisher?.Id,
            AuthorIds = authorIds,
            CategoryIds = categoryIds,
            Tags = tags,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddBook(book);
        AddAudit("catalog.book.created", "Book", book.Id, $"Created book {book.Title}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapBook(book);
    }

    public async Task<BookDto> UpdateBookAsync(Guid id, UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var book = TenantScope(Repository.Books).FirstOrDefault(x => x.Id == id);
        if (book is null)
        {
            throw AppException.NotFound("Book not found.");
        }

        var title = Clean(request.Title);
        var isbn = Clean(request.Isbn);

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(isbn))
        {
            throw AppException.BadRequest("Book title and ISBN are required.");
        }

        if (TenantScope(Repository.Books).Any(x => x.Id != id && x.Isbn.Equals(isbn, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("ISBN already exists in this tenant.");
        }

        var publisher = GetOrCreatePublisher(request.Publisher);
        var authorIds = request.Authors.Select(GetOrCreateAuthor).Select(x => x.Id).Distinct().ToList();
        var categoryIds = request.Categories.Select(GetOrCreateCategory).Select(x => x.Id).Distinct().ToList();

        book.Title = title;
        book.Isbn = isbn;
        book.Description = string.IsNullOrWhiteSpace(request.Description)
            ? $"AI fallback: Mo ta ngan cho sach {title}."
            : request.Description.Trim();
        book.PublishedYear = request.PublishedYear;
        book.Language = string.IsNullOrWhiteSpace(request.Language) ? CurrentUser.Locale : request.Language.Trim();
        book.PublisherId = publisher?.Id;
        book.AuthorIds = authorIds;
        book.CategoryIds = categoryIds;
        book.Tags = request.Tags.Select(Clean).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        book.UpdatedAt = Clock.UtcNow;
        book.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.book.updated", "Book", book.Id, $"Updated book {book.Title}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapBook(book);
    }

    public async Task DeleteBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var book = TenantScope(Repository.Books).FirstOrDefault(x => x.Id == id);
        if (book is null)
        {
            throw AppException.NotFound("Book not found.");
        }

        var hasCopies = TenantScope(Repository.BookCopies).Any(x => x.BookId == id);
        if (hasCopies)
        {
            throw AppException.BadRequest("Delete book copies before deleting this book.");
        }

        book.IsDeleted = true;
        book.UpdatedAt = Clock.UtcNow;
        book.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.book.deleted", "Book", book.Id, $"Deleted book {book.Title}");
        await Repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<BookCopyDto> CreateCopyAsync(CreateCopyRequest request, CancellationToken cancellationToken = default)
    {
        var branch = GetBranch(request.BranchId);
        var book = TenantScope(Repository.Books).FirstOrDefault(x => x.Id == request.BookId);
        if (book is null)
        {
            throw AppException.NotFound("Book not found.");
        }

        var barcode = Clean(request.Barcode);
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw AppException.BadRequest("Barcode is required.");
        }

        if (TenantScope(Repository.BookCopies).Any(x => x.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Barcode already exists in this tenant.");
        }

        var copy = new BookCopy
        {
            TenantId = TenantId,
            BranchId = branch.Id,
            BookId = book.Id,
            Barcode = barcode,
            QrCode = $"LIB://{CurrentUser.TenantKey}/{barcode}",
            Location = Clean(request.Location),
            Status = BookCopyStatus.Available,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddBookCopy(copy);
        AddAudit("catalog.copy.created", "BookCopy", copy.Id, $"Created copy {copy.Barcode} for {book.Title}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapCopy(copy);
    }

    public async Task<BookCopyDto> UpdateCopyAsync(Guid id, UpdateCopyRequest request, CancellationToken cancellationToken = default)
    {
        var copy = BranchScope(Repository.BookCopies).FirstOrDefault(x => x.Id == id);
        if (copy is null)
        {
            throw AppException.NotFound("Book copy not found.");
        }

        var branch = GetBranch(request.BranchId);
        var barcode = Clean(request.Barcode);

        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw AppException.BadRequest("Barcode is required.");
        }

        if (TenantScope(Repository.BookCopies).Any(x => x.Id != id && x.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Barcode already exists in this tenant.");
        }

        if (request.Status == BookCopyStatus.Available && TenantScope(Repository.Loans).Any(x =>
                x.BookCopyId == copy.Id &&
                x.Status is LoanStatus.Active or LoanStatus.Overdue))
        {
            throw AppException.BadRequest("Return the active loan before marking this copy available.");
        }

        copy.BranchId = branch.Id;
        copy.Barcode = barcode;
        copy.QrCode = $"LIB://{CurrentUser.TenantKey}/{barcode}";
        copy.Location = Clean(request.Location);
        copy.Status = request.Status;
        copy.UpdatedAt = Clock.UtcNow;
        copy.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.copy.updated", "BookCopy", copy.Id, $"Updated copy {copy.Barcode}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapCopy(copy);
    }

    public async Task DeleteCopyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var copy = BranchScope(Repository.BookCopies).FirstOrDefault(x => x.Id == id);
        if (copy is null)
        {
            throw AppException.NotFound("Book copy not found.");
        }

        var hasActiveLoan = TenantScope(Repository.Loans).Any(x =>
            x.BookCopyId == copy.Id &&
            x.Status is LoanStatus.Active or LoanStatus.Overdue);

        if (hasActiveLoan)
        {
            throw AppException.BadRequest("Cannot delete a copy with an active loan.");
        }

        copy.IsDeleted = true;
        copy.UpdatedAt = Clock.UtcNow;
        copy.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.copy.deleted", "BookCopy", copy.Id, $"Deleted copy {copy.Barcode}", copy.BranchId);
        await Repository.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<BookCopyDto>> GetCopiesAsync(Guid? bookId, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var copies = BranchScope(Repository.BookCopies);

        if (bookId.HasValue)
        {
            copies = copies.Where(x => x.BookId == bookId.Value);
        }

        if (branchId.HasValue)
        {
            EnsureBranchAccess(branchId.Value);
            copies = copies.Where(x => x.BranchId == branchId.Value);
        }

        var result = copies.OrderBy(x => x.Barcode).Select(MapCopy).ToArray();
        return Task.FromResult<IReadOnlyCollection<BookCopyDto>>(result);
    }

    private Publisher? GetOrCreatePublisher(string? name)
    {
        var clean = Clean(name);
        if (string.IsNullOrWhiteSpace(clean))
        {
            return null;
        }

        var existing = TenantScope(Repository.Publishers).FirstOrDefault(x => x.Name.Equals(clean, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var publisher = new Publisher
        {
            TenantId = TenantId,
            Name = clean,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };
        Repository.AddPublisher(publisher);
        return publisher;
    }

    private Author GetOrCreateAuthor(string? name)
    {
        var clean = Clean(name);
        if (string.IsNullOrWhiteSpace(clean))
        {
            throw AppException.BadRequest("At least one author is required.");
        }

        var existing = TenantScope(Repository.Authors).FirstOrDefault(x => x.Name.Equals(clean, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var author = new Author
        {
            TenantId = TenantId,
            Name = clean,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };
        Repository.AddAuthor(author);
        return author;
    }

    private Category GetOrCreateCategory(string? name)
    {
        var clean = Clean(name);
        if (string.IsNullOrWhiteSpace(clean))
        {
            throw AppException.BadRequest("At least one category is required.");
        }

        var existing = TenantScope(Repository.Categories).FirstOrDefault(x => x.Name.Equals(clean, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var category = new Category
        {
            TenantId = TenantId,
            Name = clean,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };
        Repository.AddCategory(category);
        return category;
    }

    private BookListItemDto MapBookListItem(Book book, Guid? branchId)
    {
        var copies = BranchScope(Repository.BookCopies).Where(x => x.BookId == book.Id);
        if (branchId.HasValue)
        {
            copies = copies.Where(x => x.BranchId == branchId.Value);
        }

        var copyList = copies.ToArray();
        return new BookListItemDto(
            book.Id,
            book.Title,
            book.Isbn,
            book.Description,
            book.PublishedYear,
            book.Language,
            Names(Repository.Authors, book.AuthorIds),
            Names(Repository.Categories, book.CategoryIds),
            Repository.Publishers.FirstOrDefault(x => x.Id == book.PublisherId)?.Name ?? string.Empty,
            book.Tags,
            copyList.Length,
            copyList.Count(x => x.Status == BookCopyStatus.Available));
    }

    private BookDto MapBook(Book book)
    {
        var copies = BranchScope(Repository.BookCopies)
            .Where(x => x.BookId == book.Id)
            .OrderBy(x => x.Barcode)
            .Select(MapCopy)
            .ToArray();

        return new BookDto(
            book.Id,
            book.Title,
            book.Isbn,
            book.Description,
            book.PublishedYear,
            book.Language,
            Names(Repository.Authors, book.AuthorIds),
            Names(Repository.Categories, book.CategoryIds),
            Repository.Publishers.FirstOrDefault(x => x.Id == book.PublisherId)?.Name ?? string.Empty,
            book.Tags,
            copies);
    }

    private BookCopyDto MapCopy(BookCopy copy)
    {
        var branch = Repository.Branches.FirstOrDefault(x => x.Id == copy.BranchId);
        return new BookCopyDto(
            copy.Id,
            copy.BookId,
            copy.BranchId,
            branch?.Name ?? string.Empty,
            copy.Barcode,
            copy.QrCode,
            copy.Status,
            copy.Location);
    }

    private static string[] Names<T>(IEnumerable<T> entities, IReadOnlyCollection<Guid> ids)
        where T : TenantEntity
    {
        return entities
            .Where(x => ids.Contains(x.Id))
            .Select(x => x switch
            {
                Author author => author.Name,
                Category category => category.Name,
                _ => string.Empty
            })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }
}
