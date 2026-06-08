using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Domain.Entities;

public sealed class MemberProfile : BranchEntity
{
    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public MemberStatus Status { get; set; } = MemberStatus.Active;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Loan : BranchEntity
{
    public Guid MemberId { get; set; }

    public Guid BookCopyId { get; set; }

    public DateTimeOffset LoanedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset DueAt { get; set; }

    public DateTimeOffset? ReturnedAt { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Active;

    public int RenewalCount { get; set; }

    public decimal FineAmount { get; set; }
}

public sealed class HoldRequest : BranchEntity
{
    public Guid MemberId { get; set; }

    public Guid BookId { get; set; }

    public HoldStatus Status { get; set; } = HoldStatus.Waiting;

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ExpiresAt { get; set; }
}
