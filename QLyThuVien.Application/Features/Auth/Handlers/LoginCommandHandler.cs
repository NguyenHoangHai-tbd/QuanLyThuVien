using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Application.Features.Auth.Common;
using QLyThuVien.Application.Features.Auth.Commands.Login;

namespace QLyThuVien.Application.Features.Auth.Handlers;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAccessTokenService _accessTokenService;
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILibraryRepository _repository;

    public LoginCommandHandler(
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

    public Task<LoginResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
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
        var accessToken = _accessTokenService.CreateToken(
            tenant.Id,
            tenant.Key,
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.BranchIds,
            expiresAt);
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
