using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Auth.Common;

public sealed record UserContextDto(
    Guid UserId,
    string FullName,
    string Email,
    UserRole Role,
    Guid TenantId,
    string TenantKey,
    string TenantName,
    IReadOnlyCollection<Guid> BranchIds,
    string Locale);
