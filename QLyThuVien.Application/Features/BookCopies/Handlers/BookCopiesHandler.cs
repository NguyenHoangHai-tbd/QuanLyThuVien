using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.BookCopies.Commands.Create;
using QLyThuVien.Application.Features.BookCopies.Commands.Delete;
using QLyThuVien.Application.Features.BookCopies.Commands.Update;
using QLyThuVien.Application.Features.BookCopies.Common;
using QLyThuVien.Application.Features.BookCopies.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.BookCopies.Handlers;

public sealed class BookCopiesHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetCopiesQuery, IReadOnlyCollection<BookCopyDto>>,
    IRequestHandler<CreateCopyCommand, BookCopyDto>,
    IRequestHandler<UpdateCopyCommand, BookCopyDto>,
    IRequestHandler<DeleteCopyCommand>
{
    public BookCopiesHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<BookCopyDto>> Handle(GetCopiesQuery query, CancellationToken cancellationToken)
    {
        var copies = BranchScope(Repository.BookCopies);

        if (query.BookId.HasValue)
        {
            copies = copies.Where(x => x.BookId == query.BookId.Value);
        }

        if (query.BranchId.HasValue)
        {
            EnsureBranchAccess(query.BranchId.Value);
            copies = copies.Where(x => x.BranchId == query.BranchId.Value);
        }

        var result = copies.OrderBy(x => x.Barcode).Select(MapCopy).ToArray();
        return Task.FromResult<IReadOnlyCollection<BookCopyDto>>(result);
    }

    public async Task<BookCopyDto> Handle(CreateCopyCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
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

    public async Task<BookCopyDto> Handle(UpdateCopyCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var copy = BranchScope(Repository.BookCopies).FirstOrDefault(x => x.Id == command.Id);
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

        if (TenantScope(Repository.BookCopies).Any(x => x.Id != command.Id && x.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase)))
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

    public async Task Handle(DeleteCopyCommand command, CancellationToken cancellationToken)
    {
        var copy = BranchScope(Repository.BookCopies).FirstOrDefault(x => x.Id == command.Id);
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
}
