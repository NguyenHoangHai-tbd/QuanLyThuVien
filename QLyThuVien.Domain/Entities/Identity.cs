using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Domain.Entities;

public sealed class UserAccount : TenantEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public List<Guid> BranchIds { get; set; } = [];

    public string Locale { get; set; } = "vi";

    public bool IsActive { get; set; } = true;
}
