using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SystemMonitor.AppLogic.Extensions;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Infrastructure.Providers;
using SystemMonitor.Plugin.ApiPost;
using SystemMonitor.Plugin.FileLogger;
using SystemMonitor.Plugin.RealtimeData;
using SystemMonitor.Plugin.ThresholdAlert;
using SystemMonitor.WPF.ViewModels;
using SystemMonitor.WPF.Views;

namespace SystemMonitor.WPF;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                // Infrastructure
                services.AddSingleton<ISystemMetricsProvider>(
                    _ => MetricsProviderFactory.Create());

                // Application
                services.AddMonitoringServices(ctx.Configuration);

                // Plugin options
                services.Configure<FileLoggerOptions>(
                    ctx.Configuration.GetSection(FileLoggerOptions.SectionName));
                services.Configure<ApiPostOptions>(
                    ctx.Configuration.GetSection(ApiPostOptions.SectionName));
                services.Configure<ThresholdAlertOptions>(
                    ctx.Configuration.GetSection(ThresholdAlertOptions.SectionName));
                services.Configure<RealtimeDataOptions>(
                    ctx.Configuration.GetSection(RealtimeDataOptions.SectionName));

                services.AddHttpClient<ApiPostPlugin>(); // manages HttpClient lifetime
                services.AddSingleton<FileLoggerPlugin>();
                services.AddSingleton<ApiPostPlugin>();
                services.AddSingleton<ThresholdAlertPlugin>();
                services.AddSingleton<RealtimeDataPlugin>();

                services.AddSingleton<IMonitorPlugin>(sp =>
                    sp.GetRequiredService<RealtimeDataPlugin>());
                services.AddSingleton<IMonitorPlugin>(sp =>
                    sp.GetRequiredService<FileLoggerPlugin>());
                services.AddSingleton<IMonitorPlugin>(sp =>
                    sp.GetRequiredService<ApiPostPlugin>());
                services.AddSingleton<IMonitorPlugin>(sp =>
                    sp.GetRequiredService<ThresholdAlertPlugin>());

                // WPF ViewModels
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<PluginsViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddSingleton<MainViewModel>();

                // WPF Views
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Show main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Auto-start monitoring
        var vm = _host.Services.GetRequiredService<MainViewModel>();
        await vm.InitializeAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            var vm = _host.Services.GetRequiredService<MainViewModel>();
            await vm.ShutdownAsync();

            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
