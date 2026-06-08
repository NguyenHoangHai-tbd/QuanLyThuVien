using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Infrastructure.Services;

public sealed class AccessTokenService : IAccessTokenService
{
    private const string DefaultAudience = "QLyThuVien.Api";
    private const string DefaultIssuer = "QLyThuVien";
    private const string DefaultSecret = "ql-thu-vien-demo-jwt-secret-key-for-development-2026";

    private readonly string _audience;
    private readonly string _issuer;
    private readonly byte[] _secretBytes;

    public AccessTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? DefaultIssuer;
        _audience = configuration["Jwt:Audience"] ?? DefaultAudience;
        _secretBytes = Encoding.UTF8.GetBytes(configuration["Jwt:Secret"] ?? DefaultSecret);
    }

    public string CreateToken(
        Guid tenantId,
        string tenantKey,
        Guid userId,
        string userName,
        string email,
        UserRole role,
        IReadOnlyCollection<Guid> branchIds,
        DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };
        var payload = new Dictionary<string, object?>
        {
            ["iss"] = _issuer,
            ["aud"] = _audience,
            [JwtClaimNames.UserId] = userId.ToString("D"),
            [JwtClaimNames.UserName] = userName,
            [JwtClaimNames.Email] = email,
            [JwtClaimNames.Role] = role.ToString(),
            [JwtClaimNames.TenantId] = tenantId.ToString("D"),
            [JwtClaimNames.TenantKey] = tenantKey,
            [JwtClaimNames.BranchIds] = branchIds.Select(x => x.ToString("D")).ToArray(),
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds()
        };

        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signature = Sign(signingInput);

        return $"{signingInput}.{signature}";
    }

    public AccessTokenPayload? TryReadToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var expectedSignature = Sign($"{parts[0]}.{parts[1]}");
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedSignature),
                Encoding.ASCII.GetBytes(parts[2])))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            var root = document.RootElement;

            if (!StringClaim(root, "iss").Equals(_issuer, StringComparison.Ordinal) ||
                !StringClaim(root, "aud").Equals(_audience, StringComparison.Ordinal))
            {
                return null;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(LongClaim(root, "exp"));
            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            if (!Guid.TryParse(StringClaim(root, JwtClaimNames.TenantId), out var tenantId) ||
                !Guid.TryParse(StringClaim(root, JwtClaimNames.UserId), out var userId) ||
                !Enum.TryParse<UserRole>(StringClaim(root, JwtClaimNames.Role), out var role))
            {
                return null;
            }

            return new AccessTokenPayload(
                tenantId,
                StringClaim(root, JwtClaimNames.TenantKey),
                userId,
                StringClaim(root, JwtClaimNames.UserName),
                StringClaim(root, JwtClaimNames.Email),
                role,
                GuidArrayClaim(root, JwtClaimNames.BranchIds),
                expiresAt);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private string Sign(string input)
    {
        using var hmac = new HMACSHA256(_secretBytes);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.ASCII.GetBytes(input)));
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
        return Convert.FromBase64String(base64);
    }

    private static string StringClaim(JsonElement root, string name)
        => root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static long LongClaim(JsonElement root, string name)
        => root.TryGetProperty(name, out var property) && property.TryGetInt64(out var value)
            ? value
            : 0;

    private static IReadOnlyCollection<Guid> GuidArrayClaim(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<Guid>();
        }

        return property
            .EnumerateArray()
            .Select(x => Guid.TryParse(x.GetString(), out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToArray();
    }
}
