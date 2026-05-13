using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Dtos;

namespace QLyThuVien.Application.Services;

public sealed class AuthService
{
    private readonly IAccessTokenService _accessTokenService;
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILibraryRepository _repository;

    public AuthService(
        ILibraryRepository repository,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IClock clock)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _accessTokenService = accessTokenService;
        _clock = clock;
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var tenantKey = (request.TenantKey ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();

        var tenant = _repository.Tenants.FirstOrDefault(x =>
            !x.IsDeleted &&
            x.IsActive &&
            x.Key.Equals(tenantKey, StringComparison.OrdinalIgnoreCase));

        if (tenant is null)
        {
            throw AppException.Unauthorized("Tenant or account is invalid.");
        }

        var user = _repository.Users.FirstOrDefault(x =>
            !x.IsDeleted &&
            x.IsActive &&
            x.TenantId == tenant.Id &&
            x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw AppException.Unauthorized("Tenant or account is invalid.");
        }

        var expiresAt = _clock.UtcNow.AddHours(8);
        var accessToken = _accessTokenService.CreateToken(tenant.Id, tenant.Key, user.Id, expiresAt);
        var response = new LoginResponse(
            accessToken,
            expiresAt,
            new UserContextDto(
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                tenant.Id,
                tenant.Key,
                tenant.Name,
                user.BranchIds,
                user.Locale));

        return Task.FromResult(response);
    }
}
