using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.System.Common;
using QLyThuVien.Application.Features.System.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/system")]
[Authorize(Roles = "SuperAdmin,TenantAdmin")]
public sealed class SystemController : ControllerBase
{
    private readonly ISender _sender;

    public SystemController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("database")]
    public Task<DatabaseConnectionStatusDto> GetDatabaseStatus(CancellationToken cancellationToken)
        => _sender.Send(new GetDatabaseConnectionStatusQuery(), cancellationToken);
}
