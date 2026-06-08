namespace QLyThuVien.Application.Features.Dashboard.Common;

public sealed record BranchKpiDto(Guid BranchId, string BranchName, int Copies, int ActiveLoans, int OverdueLoans);

