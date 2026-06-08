using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.Branches.Commands.Create;
using QLyThuVien.Application.Features.Branches.Commands.Delete;
using QLyThuVien.Application.Features.Branches.Commands.Update;
using QLyThuVien.Application.Features.Branches.Common;
using QLyThuVien.Application.Features.Branches.Queries;
using QLyThuVien.Application.Features.Policies.Common;
using QLyThuVien.Application.Features.Policies.Queries;
using QLyThuVien.Application.Features.Tenants.Common;
using QLyThuVien.Application.Features.Tenants.Commands.Create;
using QLyThuVien.Application.Features.Tenants.Commands.Delete;
using QLyThuVien.Application.Features.Tenants.Commands.Update;
using QLyThuVien.Application.Features.Tenants.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("tenants/current")]
    public Task<TenantDto> GetCurrentTenant(CancellationToken cancellationToken)
        => _sender.Send(new GetCurrentTenantQuery(), cancellationToken);

    [HttpGet("tenants")]
    public Task<IReadOnlyCollection<TenantDto>> GetTenants(CancellationToken cancellationToken)
        => _sender.Send(new GetTenantsQuery(), cancellationToken);

    [HttpPost("tenants")]
    [Authorize(Roles = "SuperAdmin")]
    public Task<TenantDto> CreateTenant(TenantCreateRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateTenantCommand(request), cancellationToken);

    [HttpPut("tenants/{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public Task<TenantDto> UpdateTenant(Guid id, TenantUpdateRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateTenantCommand(id, request), cancellationToken);

    [HttpDelete("tenants/{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteTenant(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteTenantCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("branches")]
    public Task<IReadOnlyCollection<BranchDto>> GetBranches(CancellationToken cancellationToken)
        => _sender.Send(new GetBranchesQuery(), cancellationToken);

    [HttpPost("branches")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public Task<BranchDto> CreateBranch(BranchRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateBranchCommand(request), cancellationToken);

    [HttpPut("branches/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public Task<BranchDto> UpdateBranch(Guid id, BranchUpdateRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateBranchCommand(id, request), cancellationToken);

    [HttpDelete("branches/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<IActionResult> DeleteBranch(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteBranchCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("policies/current")]
    public Task<LibraryPolicyDto> GetPolicy(CancellationToken cancellationToken)
        => _sender.Send(new GetCurrentPolicyQuery(), cancellationToken);
}
