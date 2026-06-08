using QLyThuVien.Application.Common;
using QLyThuVien.Application.Interfaces;

namespace QLyThuVien.Api.Middleware;

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ILibraryRepository repository,
        ICurrentUserContextWriter currentUserWriter)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api") || path.StartsWithSegments("/api/auth/login"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { status = 401, detail = "Missing or invalid JWT access token." });
            return;
        }

        var tenantIdValue = context.User.FindFirst(JwtClaimNames.TenantId)?.Value;
        var tenantKey = context.User.FindFirst(JwtClaimNames.TenantKey)?.Value ?? string.Empty;
        var userIdValue = context.User.FindFirst(JwtClaimNames.UserId)?.Value;
        if (!Guid.TryParse(tenantIdValue, out var tenantId) || !Guid.TryParse(userIdValue, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { status = 401, detail = "JWT token is missing tenant or user claims." });
            return;
        }

        var tenant = repository.Tenants.FirstOrDefault(x =>
            !x.IsDeleted &&
            x.IsActive &&
            x.Id == tenantId &&
            x.Key.Equals(tenantKey, StringComparison.OrdinalIgnoreCase));

        var user = repository.Users.FirstOrDefault(x =>
            !x.IsDeleted &&
            x.IsActive &&
            x.TenantId == tenantId &&
            x.Id == userId);

        if (tenant is null || user is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { status = 401, detail = "Tenant context could not be resolved." });
            return;
        }

        currentUserWriter.Set(new CurrentUserSnapshot(
            tenant.Id,
            tenant.Key,
            tenant.Name,
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.BranchIds,
            user.Locale));

        await _next(context);
    }
}
