using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Services;

public sealed class TenantService : ApplicationServiceBase
{
    public TenantService(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<TenantDto> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var tenant = Repository.Tenants.First(x => x.Id == TenantId);
        return Task.FromResult(MapTenant(tenant));
    }

    public Task<IReadOnlyCollection<TenantDto>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var tenants = CurrentUser.Role == UserRole.SuperAdmin
            ? Repository.Tenants.Where(x => !x.IsDeleted)
            : Repository.Tenants.Where(x => x.Id == TenantId && !x.IsDeleted);

        return Task.FromResult<IReadOnlyCollection<TenantDto>>(tenants.Select(MapTenant).ToArray());
    }

    public async Task<TenantDto> CreateTenantAsync(TenantCreateRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (CurrentUser.Role != UserRole.SuperAdmin)
        {
            throw AppException.Forbidden("Only Super Admin can create tenants.");
        }

        var key = Clean(request.Key).ToLowerInvariant();
        var name = Clean(request.Name);

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Tenant key and name are required.");
        }

        if (Repository.Tenants.Any(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Tenant key already exists.");
        }

        var tenant = new Tenant
        {
            Key = key,
            Name = name,
            Plan = string.IsNullOrWhiteSpace(request.Plan) ? "MVP" : request.Plan.Trim(),
            DefaultLocale = string.IsNullOrWhiteSpace(request.DefaultLocale) ? "vi" : request.DefaultLocale.Trim(),
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddTenant(tenant);
        Repository.AddPolicy(new LibraryPolicy
        {
            TenantId = tenant.Id,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        });
        AddAudit("tenant.created", "Tenant", tenant.Id, $"Created tenant {tenant.Name}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapTenant(tenant);
    }

    public Task<IReadOnlyCollection<BranchDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
    {
        var branches = TenantScope(Repository.Branches)
            .Where(x => CurrentUser.CanAccessBranch(x.Id))
            .OrderBy(x => x.Name)
            .Select(MapBranch)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<BranchDto>>(branches);
    }

    public async Task<BranchDto> CreateBranchAsync(BranchRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (CurrentUser.Role is not (UserRole.SuperAdmin or UserRole.TenantAdmin))
        {
            throw AppException.Forbidden("Only tenant admins can create branches.");
        }

        var code = Clean(request.Code).ToUpperInvariant();
        var name = Clean(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Branch code and name are required.");
        }

        if (TenantScope(Repository.Branches).Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Branch code already exists.");
        }

        var branch = new Branch
        {
            TenantId = TenantId,
            Code = code,
            Name = name,
            Address = Clean(request.Address),
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddBranch(branch);
        AddAudit("branch.created", "Branch", branch.Id, $"Created branch {branch.Name}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapBranch(branch);
    }

    public Task<LibraryPolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var policy = Repository.Policies.FirstOrDefault(x => x.TenantId == TenantId && !x.IsDeleted)
            ?? new LibraryPolicy { TenantId = TenantId };

        return Task.FromResult(new LibraryPolicyDto(
            policy.MaxLoanDays,
            policy.MaxRenewals,
            policy.DailyFineAmount,
            policy.MaxActiveLoansPerMember));
    }

    private static TenantDto MapTenant(Tenant tenant)
        => new(tenant.Id, tenant.Key, tenant.Name, tenant.Plan, tenant.DefaultLocale, tenant.PrimaryColor, tenant.IsActive);

    private static BranchDto MapBranch(Branch branch)
        => new(branch.Id, branch.Code, branch.Name, branch.Address, branch.IsActive);
}
