namespace QLyThuVien.Application.Features.Auth.Common;

public sealed record LoginRequest(string TenantKey, string Email, string Password);
