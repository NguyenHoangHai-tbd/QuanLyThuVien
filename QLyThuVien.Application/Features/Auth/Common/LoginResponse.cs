namespace QLyThuVien.Application.Features.Auth.Common;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserContextDto User);
