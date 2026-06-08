using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Common;

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
