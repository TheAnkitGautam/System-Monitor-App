using System.Runtime.InteropServices;
using SystemMonitor.Core.Interfaces;

namespace SystemMonitor.Infrastructure.Providers;

/// <summary>
/// Selects the correct <see cref="ISystemMetricsProvider"/> implementation at runtime
/// based on the current operating system using <see cref="RuntimeInformation.IsOSPlatform"/>.
///
/// This is the single place where platform branching occurs. All other code
/// depends only on <see cref="ISystemMetricsProvider"/> and is platform-agnostic.
/// Adding support for a new OS requires only a new provider class and one line here.
/// </summary>
public static class MetricsProviderFactory
{
    /// <summary>
    /// Creates and returns the appropriate <see cref="ISystemMetricsProvider"/> for the
    /// current OS. Called once during DI container registration.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown at startup if the OS is not Windows, Linux, or macOS.
    /// Fail-fast is intentional: running with no metrics provider would produce
    /// silent incorrect data, which is worse than an immediate clear error.
    /// </exception>
    public static ISystemMetricsProvider Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsMetricsProvider();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxMetricsProvider();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOsMetricsProvider();

        throw new PlatformNotSupportedException(
            $"No metrics provider available for OS: {RuntimeInformation.OSDescription}. " +
            $"Implement a new ISystemMetricsProvider and register it in MetricsProviderFactory.");
    }
}
