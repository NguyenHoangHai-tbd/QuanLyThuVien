using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Common;

public sealed record AccessTokenPayload(
    Guid TenantId,
    string TenantKey,
    Guid UserId,
    string UserName,
    string Email,
    UserRole Role,
    IReadOnlyCollection<Guid> BranchIds,
    DateTimeOffset ExpiresAt);
