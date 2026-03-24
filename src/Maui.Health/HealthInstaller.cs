using Maui.Health.Services;

namespace Maui.Health;

/// <summary>
/// DI registration for Maui.Health services.
/// </summary>
public static class HealthInstaller
{
    /// <summary>
    /// Registers <see cref="IHealthService"/> and its dependencies into the service collection.
    /// </summary>
    public static IServiceCollection AddHealth(this IServiceCollection services)
    {
        services.AddSingleton<HealthWorkoutService>();
        services.AddSingleton<IHealthService, HealthService>();
        //services.AddSingleton<HealthService>();//for testing

        services.AddLogging();

        return services;
    }
}
