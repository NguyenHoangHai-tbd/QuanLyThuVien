using Microsoft.Extensions.DependencyInjection;

namespace QLyThuVien.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(Features.Auth.Commands.Login.LoginCommand).Assembly));

        return services;
    }
}
