using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Inventory.Commands.Stocktake;
using QLyThuVien.Application.Features.Inventory.Common;
using QLyThuVien.Application.Features.Inventory.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Inventory.Handlers;

public sealed class InventoryHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetInventoryCopiesQuery, IReadOnlyCollection<InventoryItemDto>>,
    IRequestHandler<GetInventorySummaryQuery, InventorySummaryDto>,
    IRequestHandler<StocktakeCopyCommand, InventoryItemDto>
{
    public InventoryHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<InventoryItemDto>> Handle(GetInventoryCopiesQuery query, CancellationToken cancellationToken)
    {
        var copies = FilterCopies(query.BranchId, query.Status)
            .OrderBy(x => x.Barcode)
            .Select(MapItem)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<InventoryItemDto>>(copies);
    }

    public Task<InventorySummaryDto> Handle(GetInventorySummaryQuery query, CancellationToken cancellationToken)
    {
        var copies = FilterCopies(query.BranchId, null).ToArray();
        var branchName = "Tat ca chi nhanh";

        if (query.BranchId.HasValue)
        {
            var branch = GetBranch(query.BranchId.Value);
            branchName = branch.Name;
        }

        var summary = new InventorySummaryDto(
            query.BranchId,
            branchName,
            copies.Length,
            copies.Count(x => x.Status == BookCopyStatus.Available),
            copies.Count(x => x.Status == BookCopyStatus.OnLoan),
            copies.Count(x => x.Status == BookCopyStatus.Reserved),
            copies.Count(x => x.Status == BookCopyStatus.Damaged),
            copies.Count(x => x.Status == BookCopyStatus.Lost),
            copies.Count(x => x.Status == BookCopyStatus.Liquidated));

        return Task.FromResult(summary);
    }

    public async Task<InventoryItemDto> Handle(StocktakeCopyCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var branch = GetBranch(request.BranchId);
        var barcode = Clean(request.Barcode);

        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw AppException.BadRequest("Barcode is required.");
        }

        var copy = BranchScope(Repository.BookCopies).FirstOrDefault(x =>
            x.BranchId == branch.Id &&
            x.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase));

        if (copy is null)
        {
            throw AppException.NotFound("Book copy not found in this branch.");
        }

        var hasActiveLoan = TenantScope(Repository.Loans).Any(x =>
            x.BookCopyId == copy.Id &&
            x.Status is LoanStatus.Active or LoanStatus.Overdue);

        if (hasActiveLoan && request.Status != BookCopyStatus.OnLoan)
        {
            throw AppException.BadRequest("Return the active loan before changing this copy inventory status.");
        }

        copy.Status = request.Status;
        copy.Location = Clean(request.Location);
        copy.UpdatedAt = Clock.UtcNow;
        copy.UpdatedBy = CurrentUser.Email;

        var note = Clean(request.Note);
        var summary = string.IsNullOrWhiteSpace(note)
            ? $"Stocktaked copy {copy.Barcode} as {copy.Status}"
            : $"Stocktaked copy {copy.Barcode} as {copy.Status}. Note: {note}";
        AddAudit("inventory.copy.stocktaked", "BookCopy", copy.Id, summary, copy.BranchId);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapItem(copy);
    }

    private IEnumerable<BookCopy> FilterCopies(Guid? branchId, BookCopyStatus? status)
    {
        var copies = BranchScope(Repository.BookCopies);

        if (branchId.HasValue)
        {
            EnsureBranchAccess(branchId.Value);
            copies = copies.Where(x => x.BranchId == branchId.Value);
        }

        if (status.HasValue)
        {
            copies = copies.Where(x => x.Status == status.Value);
        }

        return copies;
    }

    private InventoryItemDto MapItem(BookCopy copy)
    {
        var branch = Repository.Branches.FirstOrDefault(x => x.Id == copy.BranchId);
        var book = Repository.Books.FirstOrDefault(x => x.Id == copy.BookId);

        return new InventoryItemDto(
            copy.Id,
            copy.BookId,
            book?.Title ?? string.Empty,
            copy.BranchId,
            branch?.Name ?? string.Empty,
            copy.Barcode,
            copy.QrCode,
            copy.Status,
            copy.Location);
    }
}
