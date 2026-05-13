namespace QLyThuVien.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string value);

    bool Verify(string value, string hash);
}

public interface IAccessTokenService
{
    string CreateToken(Guid tenantId, string tenantKey, Guid userId, DateTimeOffset expiresAt);

    AccessTokenPayload? TryReadToken(string token);
}

public sealed record AccessTokenPayload(Guid TenantId, string TenantKey, Guid UserId, DateTimeOffset ExpiresAt);

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
