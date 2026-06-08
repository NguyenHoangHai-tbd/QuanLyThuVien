using System.Security.Cryptography;
using System.Text;
using QLyThuVien.Application.Interfaces;

namespace QLyThuVien.Infrastructure.Services;

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
