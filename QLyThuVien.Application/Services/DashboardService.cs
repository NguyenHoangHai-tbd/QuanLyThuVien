using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Services;

public sealed class DashboardService : ApplicationServiceBase
{
    public DashboardService(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var books = TenantScope(Repository.Books).ToArray();
        var copies = BranchScope(Repository.BookCopies).ToArray();
        var members = BranchScope(Repository.Members).ToArray();
        var loans = BranchScope(Repository.Loans).ToArray();
        var activeLoans = loans.Where(x => x.Status is LoanStatus.Active or LoanStatus.Overdue).ToArray();
        var overdueLoans = activeLoans.Where(x => x.Status == LoanStatus.Overdue || x.DueAt < Clock.UtcNow).ToArray();

        var branchKpis = TenantScope(Repository.Branches)
            .Where(x => CurrentUser.CanAccessBranch(x.Id))
            .OrderBy(x => x.Name)
            .Select(branch => new BranchKpiDto(
                branch.Id,
                branch.Name,
                copies.Count(copy => copy.BranchId == branch.Id),
                activeLoans.Count(loan => loan.BranchId == branch.Id),
                overdueLoans.Count(loan => loan.BranchId == branch.Id)))
            .ToArray();

        var popularBooks = loans
            .GroupBy(loan => Repository.BookCopies.FirstOrDefault(copy => copy.Id == loan.BookCopyId)?.BookId)
            .Where(group => group.Key.HasValue)
            .Select(group =>
            {
                var book = books.FirstOrDefault(x => x.Id == group.Key!.Value);
                return new PopularBookDto(group.Key!.Value, book?.Title ?? string.Empty, group.Count());
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Title))
            .OrderByDescending(x => x.LoanCount)
            .Take(5)
            .ToArray();

        var recentActivities = TenantScope(Repository.AuditLogs)
            .Where(x => !x.BranchId.HasValue || CurrentUser.CanAccessBranch(x.BranchId.Value))
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => new RecentActivityDto(x.Action, x.Summary, x.CreatedAt))
            .ToArray();

        var summary = new DashboardSummaryDto(
            books.Length,
            copies.Length,
            copies.Count(x => x.Status == BookCopyStatus.Available),
            copies.Count(x => x.Status == BookCopyStatus.OnLoan),
            overdueLoans.Length,
            members.Length,
            loans.Where(x => x.Status != LoanStatus.Returned || x.FineAmount > 0).Sum(x => x.FineAmount),
            branchKpis,
            recentActivities,
            popularBooks);

        return Task.FromResult(summary);
    }
}
