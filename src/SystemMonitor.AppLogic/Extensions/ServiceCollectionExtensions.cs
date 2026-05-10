using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SystemMonitor.AppLogic.Configuration;
using SystemMonitor.Core.Interfaces;

namespace SystemMonitor.AppLogic.Extensions;

/// <summary>
/// Extension methods for registering Application-layer services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MonitoringService"/> and binds <see cref="MonitoringOptions"/>
    /// from the supplied configuration section.
    /// </summary>
    public static IServiceCollection AddMonitoringServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MonitoringOptions>(
            configuration.GetSection(MonitoringOptions.SectionName));

        services.AddSingleton<MonitoringService>();
        services.AddSingleton<IMonitoringService>(sp =>
            sp.GetRequiredService<MonitoringService>());

        return services;
    }
}
