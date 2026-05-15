using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Infrastructure.Logging;
using NexaPlay.Infrastructure.Persistence;
using NexaPlay.Infrastructure.Platform;
using NexaPlay.Infrastructure.Services;
using NexaPlay.Presentation.Navigation;
using NexaPlay.Presentation.ViewModels;
using System;
using System.IO;

namespace NexaPlay;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private Window? _window;

    public App()
    {
        InitializeComponent();
        this.UnhandledException += (s, e) => 
        {
            File.WriteAllText("crash.txt", "Unhandled Exception: " + e.Exception.ToString());
        };

        try 
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            File.WriteAllText("crash_init.txt", "Init Exception: " + ex.ToString());
        }
    }

    public T GetRequiredService<T>() where T : notnull
        => _serviceProvider!.GetRequiredService<T>();

    private static void ConfigureServices(IServiceCollection services)
    {
        // ── Infrastructure: Singletons ──────────────────────────────
        services.AddSingleton<IAppLogService, AppLogService>();
        services.AddSingleton<AppliedStateStore>();
        services.AddSingleton<ISteamService, SteamPlatformService>();
        services.AddSingleton<IWindowsDefenderService, WindowsDefenderService>();
        services.AddSingleton<IMetadataService, MetadataService>();
        services.AddSingleton<IFixGamesDataService, FixGamesDataService>();
        services.AddSingleton<IOnlineFixService, OnlineFixService>();
        services.AddSingleton<IAddGameService, AddGameService>();
        services.AddSingleton<ILicenseService, LicenseService>();

        // ── Navigation ──────────────────────────────────────────────
        services.AddSingleton<INavigationService, NavigationService>();

        // ── ViewModels ──────────────────────────────────────────────
        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<GamesViewModel>();
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<FixGamesViewModel>();
        services.AddTransient<SettingsViewModel>();

        // ── Window ──────────────────────────────────────────────────
        services.AddTransient<MainWindow>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try 
        {
            _window = GetRequiredService<MainWindow>();
            _window.Activate();
        }
        catch (Exception ex)
        {
            File.WriteAllText("crash_launch.txt", "Launch Exception: " + ex.ToString());
        }
    }
}
