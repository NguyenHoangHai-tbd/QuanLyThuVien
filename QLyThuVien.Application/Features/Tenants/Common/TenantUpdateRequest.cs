namespace QLyThuVien.Application.Features.Tenants.Common;

public sealed record TenantUpdateRequest(string Key, string Name, string Plan, string DefaultLocale, string PrimaryColor, bool IsActive);

