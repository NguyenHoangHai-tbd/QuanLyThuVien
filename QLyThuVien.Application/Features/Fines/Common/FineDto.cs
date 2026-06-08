using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Fines.Common;

public sealed record FineDto(
    Guid LoanId,
    Guid MemberId,
    string MemberName,
    Guid BookCopyId,
    string Barcode,
    string BookTitle,
    Guid BranchId,
    string BranchName,
    DateTimeOffset DueAt,
    DateTimeOffset? ReturnedAt,
    LoanStatus LoanStatus,
    int DaysLate,
    decimal Amount,
    bool IsPaid);
