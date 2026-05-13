namespace QLyThuVien.Domain.Enums;

public enum UserRole
{
    SuperAdmin = 1,
    TenantAdmin = 2,
    Librarian = 3,
    InventoryStaff = 4,
    Member = 5
}

public enum BookCopyStatus
{
    Available = 1,
    OnLoan = 2,
    Reserved = 3,
    Damaged = 4,
    Lost = 5,
    Liquidated = 6
}

public enum LoanStatus
{
    Active = 1,
    Returned = 2,
    Overdue = 3
}

public enum HoldStatus
{
    Waiting = 1,
    Ready = 2,
    Fulfilled = 3,
    Cancelled = 4,
    Expired = 5
}

public enum NotificationStatus
{
    Queued = 1,
    Sent = 2,
    Failed = 3,
    Read = 4
}

public enum MemberStatus
{
    Active = 1,
    Blocked = 2,
    Expired = 3
}
