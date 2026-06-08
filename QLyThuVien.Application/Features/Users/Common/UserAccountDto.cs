using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Users.Common;

public sealed record UserAccountDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    IReadOnlyCollection<Guid> BranchIds,
    IReadOnlyCollection<string> BranchNames,
    string Locale,
    bool IsActive,
    DateTimeOffset CreatedAt);
