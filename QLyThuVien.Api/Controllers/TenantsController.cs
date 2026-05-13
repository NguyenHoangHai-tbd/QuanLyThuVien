using Microsoft.AspNetCore.Mvc;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Application.Services;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class TenantsController : ControllerBase
{
    private readonly TenantService _tenantService;

    public TenantsController(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("tenants/current")]
    public Task<TenantDto> GetCurrentTenant(CancellationToken cancellationToken)
        => _tenantService.GetCurrentTenantAsync(cancellationToken);

    [HttpGet("tenants")]
    public Task<IReadOnlyCollection<TenantDto>> GetTenants(CancellationToken cancellationToken)
        => _tenantService.GetTenantsAsync(cancellationToken);

    [HttpPost("tenants")]
    public Task<TenantDto> CreateTenant(TenantCreateRequest request, CancellationToken cancellationToken)
        => _tenantService.CreateTenantAsync(request, cancellationToken);

    [HttpGet("branches")]
    public Task<IReadOnlyCollection<BranchDto>> GetBranches(CancellationToken cancellationToken)
        => _tenantService.GetBranchesAsync(cancellationToken);

    [HttpPost("branches")]
    public Task<BranchDto> CreateBranch(BranchRequest request, CancellationToken cancellationToken)
        => _tenantService.CreateBranchAsync(request, cancellationToken);

    [HttpGet("policies/current")]
    public Task<LibraryPolicyDto> GetPolicy(CancellationToken cancellationToken)
        => _tenantService.GetPolicyAsync(cancellationToken);
}
