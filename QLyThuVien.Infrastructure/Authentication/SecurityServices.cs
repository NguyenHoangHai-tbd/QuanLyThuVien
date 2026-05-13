using System.Security.Cryptography;
using System.Text;
using QLyThuVien.Application.Abstractions;

namespace QLyThuVien.Infrastructure.Authentication;

public sealed class Sha256PasswordHasher : IPasswordHasher
{
    private const string Prefix = "QLyThuVien:v1:";

    public string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Prefix + value));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string value, string hash)
    {
        return Hash(value).Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class AccessTokenService : IAccessTokenService
{
    private const string Secret = "ql-thu-vien-demo-token-secret";

    public string CreateToken(Guid tenantId, string tenantKey, Guid userId, DateTimeOffset expiresAt)
    {
        var body = $"{tenantId:N}|{tenantKey}|{userId:N}|{expiresAt.ToUnixTimeSeconds()}";
        var signature = Sign(body);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{body}|{signature}"));
    }

    public AccessTokenPayload? TryReadToken(string token)
    {
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = raw.Split('|');
            if (parts.Length != 5)
            {
                return null;
            }

            var body = string.Join('|', parts.Take(4));
            if (!Sign(body).Equals(parts[4], StringComparison.Ordinal))
            {
                return null;
            }

            if (!Guid.TryParseExact(parts[0], "N", out var tenantId) ||
                !Guid.TryParseExact(parts[2], "N", out var userId) ||
                !long.TryParse(parts[3], out var expiresUnix))
            {
                return null;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix);
            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            return new AccessTokenPayload(tenantId, parts[1], userId, expiresAt);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
