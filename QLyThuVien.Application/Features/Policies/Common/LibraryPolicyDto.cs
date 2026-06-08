namespace QLyThuVien.Application.Features.Policies.Common;

public sealed record LibraryPolicyDto(int MaxLoanDays, int MaxRenewals, decimal DailyFineAmount, int MaxActiveLoansPerMember);

