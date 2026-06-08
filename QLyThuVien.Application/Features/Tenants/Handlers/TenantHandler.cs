using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Tenants.Commands.Create;
using QLyThuVien.Application.Features.Tenants.Commands.Delete;
using QLyThuVien.Application.Features.Tenants.Commands.Update;
using QLyThuVien.Application.Features.Tenants.Common;
using QLyThuVien.Application.Features.Tenants.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Tenants.Handlers;

public sealed class TenantHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetCurrentTenantQuery, TenantDto>,
    IRequestHandler<GetTenantsQuery, IReadOnlyCollection<TenantDto>>,
    IRequestHandler<CreateTenantCommand, TenantDto>,
    IRequestHandler<UpdateTenantCommand, TenantDto>,
    IRequestHandler<DeleteTenantCommand>
{
    public TenantHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<TenantDto> Handle(GetCurrentTenantQuery query, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var tenant = Repository.Tenants.First(x => x.Id == TenantId);
        return Task.FromResult(MapTenant(tenant));
    }

    public Task<IReadOnlyCollection<TenantDto>> Handle(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();

        var tenants = CurrentUser.Role == UserRole.SuperAdmin
            ? Repository.Tenants.Where(x => !x.IsDeleted)
            : Repository.Tenants.Where(x => x.Id == TenantId && !x.IsDeleted);

        return Task.FromResult<IReadOnlyCollection<TenantDto>>(tenants.Select(MapTenant).ToArray());
    }

    public async Task<TenantDto> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        EnsureSuperAdmin("Only Super Admin can create tenants.");

        var request = command.Request;
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

    public async Task<TenantDto> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        EnsureSuperAdmin("Only Super Admin can update tenants.");

        var request = command.Request;
        var tenant = Repository.Tenants.FirstOrDefault(x => x.Id == command.Id && !x.IsDeleted);
        if (tenant is null)
        {
            throw AppException.NotFound("Tenant not found.");
        }

        var key = Clean(request.Key).ToLowerInvariant();
        var name = Clean(request.Name);

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Tenant key and name are required.");
        }

        if (Repository.Tenants.Any(x =>
                x.Id != command.Id &&
                !x.IsDeleted &&
                x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Tenant key already exists.");
        }

        tenant.Key = key;
        tenant.Name = name;
        tenant.Plan = string.IsNullOrWhiteSpace(request.Plan) ? "MVP" : request.Plan.Trim();
        tenant.DefaultLocale = string.IsNullOrWhiteSpace(request.DefaultLocale) ? "vi" : request.DefaultLocale.Trim();
        tenant.PrimaryColor = string.IsNullOrWhiteSpace(request.PrimaryColor) ? "#155e75" : request.PrimaryColor.Trim();
        tenant.IsActive = request.IsActive;
        tenant.UpdatedAt = Clock.UtcNow;
        tenant.UpdatedBy = CurrentUser.Email;

        AddAudit("tenant.updated", "Tenant", tenant.Id, $"Updated tenant {tenant.Name}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapTenant(tenant);
    }

    public async Task Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        EnsureSuperAdmin("Only Super Admin can delete tenants.");

        if (command.Id == TenantId)
        {
            throw AppException.BadRequest("You cannot delete your current tenant.");
        }

        var tenant = Repository.Tenants.FirstOrDefault(x => x.Id == command.Id && !x.IsDeleted);
        if (tenant is null)
        {
            throw AppException.NotFound("Tenant not found.");
        }

        tenant.IsDeleted = true;
        tenant.IsActive = false;
        tenant.UpdatedAt = Clock.UtcNow;
        tenant.UpdatedBy = CurrentUser.Email;

        AddAudit("tenant.deleted", "Tenant", tenant.Id, $"Deleted tenant {tenant.Name}");
        await Repository.SaveChangesAsync(cancellationToken);
    }

    private void EnsureSuperAdmin(string message)
    {
        EnsureAuthenticated();

        if (CurrentUser.Role != UserRole.SuperAdmin)
        {
            throw AppException.Forbidden(message);
        }
    }

    private static TenantDto MapTenant(Tenant tenant)
        => new(tenant.Id, tenant.Key, tenant.Name, tenant.Plan, tenant.DefaultLocale, tenant.PrimaryColor, tenant.IsActive);
}
