namespace QLyThuVien.Application.Features.Dashboard.Common;

public sealed record DashboardSummaryDto(
    int BookCount,
    int CopyCount,
    int AvailableCopies,
    int LoanedCopies,
    int OverdueLoans,
    int MemberCount,
    decimal OpenFineAmount,
    IReadOnlyCollection<BranchKpiDto> Branches,
    IReadOnlyCollection<RecentActivityDto> RecentActivities,
    IReadOnlyCollection<PopularBookDto> PopularBooks);

