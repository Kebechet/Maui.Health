using Maui.Health.Services;

namespace Maui.Health;

public static class HealthInstaller
{
    public static IServiceCollection AddHealth(this IServiceCollection services)
    {
        services.AddSingleton<IHealthService, HealthService>();
        //services.AddSingleton<HealthService>();//for testing

        services.AddLogging();

        return services;
    }

}
