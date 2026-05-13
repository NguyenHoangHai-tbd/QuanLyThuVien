using Microsoft.Extensions.DependencyInjection;
using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Services;
using QLyThuVien.Infrastructure.Authentication;
using QLyThuVien.Infrastructure.Persistence;

namespace QLyThuVien.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ILibraryRepository, InMemoryLibraryRepository>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddSingleton<IAccessTokenService, AccessTokenService>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<CurrentUserContext>();
        services.AddScoped<ICurrentUserContext>(provider => provider.GetRequiredService<CurrentUserContext>());
        services.AddScoped<ICurrentUserContextWriter>(provider => provider.GetRequiredService<CurrentUserContext>());

        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<TenantService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<MemberService>();
        services.AddScoped<CirculationService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<AuditService>();
        services.AddScoped<AiService>();

        return services;
    }
}
