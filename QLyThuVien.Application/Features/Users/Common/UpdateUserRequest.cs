using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Users.Common;

public sealed record UpdateUserRequest(
    string FullName,
    string Email,
    string? Password,
    UserRole Role,
    IReadOnlyCollection<Guid> BranchIds,
    string Locale,
    bool IsActive);
