using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Fines.Commands.Pay;
using QLyThuVien.Application.Features.Fines.Common;
using QLyThuVien.Application.Features.Fines.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Fines.Handlers;

public sealed class FineHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetFinesQuery, IReadOnlyCollection<FineDto>>,
    IRequestHandler<PayFineCommand, FineDto>
{
    public FineHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<FineDto>> Handle(GetFinesQuery query, CancellationToken cancellationToken)
    {
        RefreshOverdueFines();
        var loans = BranchScope(Repository.Loans)
            .Where(x => x.Status is LoanStatus.Overdue or LoanStatus.Returned && x.DueAt < (x.ReturnedAt ?? Clock.UtcNow));

        if (query.BranchId.HasValue)
        {
            EnsureBranchAccess(query.BranchId.Value);
            loans = loans.Where(x => x.BranchId == query.BranchId.Value);
        }

        if (query.UnpaidOnly)
        {
            loans = loans.Where(x => x.FineAmount > 0);
        }

        var result = loans
            .OrderByDescending(x => x.FineAmount > 0)
            .ThenByDescending(x => x.DueAt)
            .Select(MapFine)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<FineDto>>(result);
    }

    public async Task<FineDto> Handle(PayFineCommand command, CancellationToken cancellationToken)
    {
        RefreshOverdueFines();
        var loan = BranchScope(Repository.Loans).FirstOrDefault(x => x.Id == command.LoanId);
        if (loan is null)
        {
            throw AppException.NotFound("Loan fine not found.");
        }

        if (loan.FineAmount <= 0)
        {
            throw AppException.BadRequest("This loan has no unpaid fine.");
        }

        if (command.Request.AmountPaid < loan.FineAmount)
        {
            throw AppException.BadRequest("Paid amount must be greater than or equal to the fine amount.");
        }

        var paidAmount = loan.FineAmount;
        loan.FineAmount = 0;
        loan.UpdatedAt = Clock.UtcNow;
        loan.UpdatedBy = CurrentUser.Email;

        AddAudit("circulation.fine.paid", "Loan", loan.Id, $"Paid fine {paidAmount:N0}", loan.BranchId);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapFine(loan);
    }

    private void RefreshOverdueFines()
    {
        var policy = GetPolicy();
        foreach (var loan in TenantScope(Repository.Loans).Where(x => x.Status == LoanStatus.Active && x.DueAt < Clock.UtcNow))
        {
            loan.Status = LoanStatus.Overdue;
        }

        foreach (var loan in TenantScope(Repository.Loans).Where(x => x.Status == LoanStatus.Overdue && x.FineAmount <= 0))
        {
            loan.FineAmount = CalculateFine(loan, policy);
        }
    }

    private decimal CalculateFine(Loan loan, LibraryPolicy policy)
    {
        var endDate = loan.ReturnedAt ?? Clock.UtcNow;
        var lateDays = Math.Max(0, (endDate.Date - loan.DueAt.Date).Days);
        return lateDays * policy.DailyFineAmount;
    }

    private LibraryPolicy GetPolicy()
        => Repository.Policies.FirstOrDefault(x => x.TenantId == TenantId && !x.IsDeleted)
            ?? new LibraryPolicy { TenantId = TenantId };

    private FineDto MapFine(Loan loan)
    {
        var member = Repository.Members.FirstOrDefault(x => x.Id == loan.MemberId);
        var copy = Repository.BookCopies.FirstOrDefault(x => x.Id == loan.BookCopyId);
        var book = copy is null ? null : Repository.Books.FirstOrDefault(x => x.Id == copy.BookId);
        var branch = Repository.Branches.FirstOrDefault(x => x.Id == loan.BranchId);
        var endDate = loan.ReturnedAt ?? Clock.UtcNow;
        var daysLate = Math.Max(0, (endDate.Date - loan.DueAt.Date).Days);

        return new FineDto(
            loan.Id,
            loan.MemberId,
            member?.FullName ?? string.Empty,
            loan.BookCopyId,
            copy?.Barcode ?? string.Empty,
            book?.Title ?? string.Empty,
            loan.BranchId,
            branch?.Name ?? string.Empty,
            loan.DueAt,
            loan.ReturnedAt,
            loan.Status,
            daysLate,
            loan.FineAmount,
            loan.FineAmount <= 0);
    }
}
