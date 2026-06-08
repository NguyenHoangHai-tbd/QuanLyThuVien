using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.Users.Common;
using QLyThuVien.Application.Features.Users.Commands.Create;
using QLyThuVien.Application.Features.Users.Commands.Delete;
using QLyThuVien.Application.Features.Users.Commands.Update;
using QLyThuVien.Application.Features.Users.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "SuperAdmin,TenantAdmin")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public Task<IReadOnlyCollection<UserAccountDto>> SearchUsers([FromQuery] string? search, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _sender.Send(new SearchUsersQuery(search, branchId), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<UserAccountDto> GetUser(Guid id, CancellationToken cancellationToken)
        => _sender.Send(new GetUserQuery(id), cancellationToken);

    [HttpPost]
    public Task<UserAccountDto> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateUserCommand(request), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<UserAccountDto> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateUserCommand(id, request), cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }
}
