using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.Auth.Common;
using QLyThuVien.Application.Features.Auth.Commands.Login;
using QLyThuVien.Application.Features.Auth.Commands.Logout;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken)
        => _sender.Send(new LoginCommand(request), cancellationToken);

    [HttpPost("logout")]
    public Task<LogoutResponse> Logout(CancellationToken cancellationToken)
        => _sender.Send(new LogoutCommand(), cancellationToken);
}
