using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.BookCopies.Common;
using QLyThuVien.Application.Features.Books.Commands.Create;
using QLyThuVien.Application.Features.Books.Commands.Delete;
using QLyThuVien.Application.Features.Books.Commands.Update;
using QLyThuVien.Application.Features.Books.Common;
using QLyThuVien.Application.Features.Books.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Books.Handlers;

public sealed class BooksHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<SearchBooksQuery, IReadOnlyCollection<BookListItemDto>>,
    IRequestHandler<GetBookQuery, BookDto>,
    IRequestHandler<CreateBookCommand, BookDto>,
    IRequestHandler<UpdateBookCommand, BookDto>,
    IRequestHandler<DeleteBookCommand>
{
    public BooksHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<BookListItemDto>> Handle(SearchBooksQuery query, CancellationToken cancellationToken)
    {
        var search = Clean(query.Search);
        var books = TenantScope(Repository.Books);

        if (!string.IsNullOrWhiteSpace(search))
        {
            books = books.Where(book =>
                HasText(book.Title, search) ||
                HasText(book.Isbn, search) ||
                HasText(book.Description, search) ||
                book.Tags.Any(tag => HasText(tag, search)) ||
                Names(Repository.Authors, book.AuthorIds).Any(name => HasText(name, search)) ||
                Names(Repository.Categories, book.CategoryIds).Any(name => HasText(name, search)));
        }

        var result = books
            .OrderBy(x => x.Title)
            .Select(book => MapBookListItem(book, query.BranchId))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<BookListItemDto>>(result);
    }

    public Task<BookDto> Handle(GetBookQuery query, CancellationToken cancellationToken)
    {
        var book = TenantScope(Repository.Books).FirstOrDefault(x => x.Id == query.Id);
        if (book is null)
        {
            throw AppException.NotFound("Book not found.");
        }

        return Task.FromResult(MapBook(book));
    }

    public async Task<BookDto> Handle(CreateBookCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var request = command.Request;
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

    public async Task<BookDto> Handle(UpdateBookCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var request = command.Request;
        var book = TenantScope(Repository.Books).FirstOrDefault(x => x.Id == command.Id);
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

        if (TenantScope(Repository.Books).Any(x => x.Id != command.Id && x.Isbn.Equals(isbn, StringComparison.OrdinalIgnoreCase)))
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

    public async Task Handle(DeleteBookCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();

        var book = TenantScope(Repository.Books).FirstOrDefault(x => x.Id == command.Id);
        if (book is null)
        {
            throw AppException.NotFound("Book not found.");
        }

        var hasCopies = TenantScope(Repository.BookCopies).Any(x => x.BookId == command.Id);
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
