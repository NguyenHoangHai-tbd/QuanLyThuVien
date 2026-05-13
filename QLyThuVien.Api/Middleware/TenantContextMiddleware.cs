using QLyThuVien.Application.Abstractions;

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
        IAccessTokenService accessTokenService,
        ILibraryRepository repository,
        ICurrentUserContextWriter currentUserWriter)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api") || path.StartsWithSegments("/api/auth/login"))
        {
            await _next(context);
            return;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : string.Empty;

        var payload = string.IsNullOrWhiteSpace(token) ? null : accessTokenService.TryReadToken(token);
        if (payload is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { status = 401, detail = "Missing or invalid access token." });
            return;
        }

        var tenant = repository.Tenants.FirstOrDefault(x =>
            !x.IsDeleted &&
            x.IsActive &&
            x.Id == payload.TenantId &&
            x.Key.Equals(payload.TenantKey, StringComparison.OrdinalIgnoreCase));

        var user = repository.Users.FirstOrDefault(x =>
            !x.IsDeleted &&
            x.IsActive &&
            x.TenantId == payload.TenantId &&
            x.Id == payload.UserId);

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
