using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Infrastructure.Persistence;
using QLyThuVien.Infrastructure.Services;

namespace QLyThuVien.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ILibraryRepository, InMemoryLibraryRepository>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddSingleton<IAccessTokenService, AccessTokenService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDatabaseConnectionChecker, SqlServerDatabaseConnectionChecker>();
        services.AddDbContext<LibraryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        if (configuration.GetValue<bool>("Database:AutoInitialize"))
        {
            services.AddHostedService<DatabaseInitializerHostedService>();
        }

        services.AddScoped<CurrentUserContext>();
        services.AddScoped<ICurrentUserContext>(provider => provider.GetRequiredService<CurrentUserContext>());
        services.AddScoped<ICurrentUserContextWriter>(provider => provider.GetRequiredService<CurrentUserContext>());

        return services;
    }
}
