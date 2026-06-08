using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Loans.Common;

public sealed record LoanDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    Guid BookCopyId,
    string Barcode,
    string BookTitle,
    Guid BranchId,
    string BranchName,
    DateTimeOffset LoanedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? ReturnedAt,
    LoanStatus Status,
    int RenewalCount,
    decimal FineAmount);

