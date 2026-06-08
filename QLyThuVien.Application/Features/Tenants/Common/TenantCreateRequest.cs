namespace QLyThuVien.Application.Features.Tenants.Common;

public sealed record TenantCreateRequest(string Key, string Name, string Plan, string DefaultLocale);

