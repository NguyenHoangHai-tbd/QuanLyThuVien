namespace QLyThuVien.Application.Features.Loans.Common;

public sealed record LoanRequest(Guid MemberId, Guid BranchId, string CopyBarcode);

