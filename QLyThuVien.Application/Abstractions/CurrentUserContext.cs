using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Abstractions;

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

public interface ICurrentUserContextWriter
{
    void Set(CurrentUserSnapshot snapshot);

    void Clear();
}

public sealed record CurrentUserSnapshot(
    Guid TenantId,
    string TenantKey,
    string TenantName,
    Guid UserId,
    string UserName,
    string Email,
    UserRole Role,
    IReadOnlyCollection<Guid> BranchIds,
    string Locale);
