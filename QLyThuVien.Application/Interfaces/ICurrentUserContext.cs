using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Interfaces;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid TenantId { get; }

    string TenantKey { get; }

    string TenantName { get; }

    Guid UserId { get; }

    string UserName { get; }

    string Email { get; }

    UserRole Role { get; }

    IReadOnlyCollection<Guid> BranchIds { get; }

    string Locale { get; }

    bool CanAccessBranch(Guid branchId);
}
