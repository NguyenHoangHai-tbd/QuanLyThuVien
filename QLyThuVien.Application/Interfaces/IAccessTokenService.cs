using QLyThuVien.Application.Common;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Interfaces;

public interface IAccessTokenService
{
    string CreateToken(
        Guid tenantId,
        string tenantKey,
        Guid userId,
        string userName,
        string email,
        UserRole role,
        IReadOnlyCollection<Guid> branchIds,
        DateTimeOffset expiresAt);

    AccessTokenPayload? TryReadToken(string token);
}
