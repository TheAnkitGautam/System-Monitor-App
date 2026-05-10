using System.Runtime.InteropServices;
using SystemMonitor.Core.Interfaces;

namespace SystemMonitor.Infrastructure.Providers;

public static class MetricsProviderFactory
{
    public static ISystemMetricsProvider Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsMetricsProvider();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxMetricsProvider();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOsMetricsProvider();

        throw new PlatformNotSupportedException(
            $"No metrics provider available for OS: {RuntimeInformation.OSDescription}.");
    }
}
