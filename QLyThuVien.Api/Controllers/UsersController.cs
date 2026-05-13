using Microsoft.AspNetCore.Mvc;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Application.Services;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public Task<IReadOnlyCollection<UserAccountDto>> SearchUsers([FromQuery] string? search, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _userService.SearchUsersAsync(search, branchId, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<UserAccountDto> GetUser(Guid id, CancellationToken cancellationToken)
        => _userService.GetUserAsync(id, cancellationToken);

    [HttpPost]
    public Task<UserAccountDto> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
        => _userService.CreateUserAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<UserAccountDto> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
        => _userService.UpdateUserAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsync(id, cancellationToken);
        return NoContent();
    }
}
