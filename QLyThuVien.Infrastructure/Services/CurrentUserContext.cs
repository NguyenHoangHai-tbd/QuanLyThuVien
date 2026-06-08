using QLyThuVien.Application.Common;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Infrastructure.Services;

public sealed class CurrentUserContext : ICurrentUserContext, ICurrentUserContextWriter
{
    private CurrentUserSnapshot? _snapshot;

    public bool IsAuthenticated => _snapshot is not null;

    public Guid TenantId => _snapshot?.TenantId ?? Guid.Empty;

    public string TenantKey => _snapshot?.TenantKey ?? string.Empty;

    public string TenantName => _snapshot?.TenantName ?? string.Empty;

    public Guid UserId => _snapshot?.UserId ?? Guid.Empty;

    public string UserName => _snapshot?.UserName ?? string.Empty;

    public string Email => _snapshot?.Email ?? string.Empty;

    public UserRole Role => _snapshot?.Role ?? UserRole.Member;

    public IReadOnlyCollection<Guid> BranchIds => _snapshot?.BranchIds ?? Array.Empty<Guid>();

    public string Locale => _snapshot?.Locale ?? "vi";

    public bool CanAccessBranch(Guid branchId)
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        if (Role is UserRole.SuperAdmin or UserRole.TenantAdmin)
        {
            return true;
        }

        return BranchIds.Contains(branchId);
    }

    public void Set(CurrentUserSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public void Clear()
    {
        _snapshot = null;
    }
}
