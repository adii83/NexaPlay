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
using System.Text;
using System.Threading.Tasks;

namespace NexaPlay;

public partial class App : Application
{
    private static readonly object CrashLogLock = new();
    private static string CrashLogPath => @"D:\My Project\NexaPlay\crash.txt";

    private IServiceProvider? _serviceProvider;
    private Window? _window;

    public App()
    {
        InitializeComponent();
        RegisterCrashLogging();

        try 
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            AppendCrashLog("InitException", ex.ToString());
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
        services.AddSingleton<INexaPlayOverrideService, NexaPlayOverrideService>();
        services.AddSingleton<IMetadataService, MetadataService>();
        services.AddSingleton<IBypassGamesDataService, BypassGamesDataService>();
        services.AddSingleton<IOnlineFixService, OnlineFixService>();
        services.AddSingleton<IAddGameService, AddGameService>();
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<ISteamStoreService, SteamStoreService>();

        // ── Navigation ──────────────────────────────────────────────
        services.AddSingleton<INavigationService, NavigationService>();

        // ── ViewModels ──────────────────────────────────────────────
        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<GamesViewModel>();
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<BypassGamesViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<GameDetailViewModel>();
        services.AddTransient<BypassGameDetailViewModel>();

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
            AppendCrashLog("LaunchException", ex.ToString());
        }
    }

    private void RegisterCrashLogging()
    {
        this.UnhandledException += (s, e) =>
        {
            AppendCrashLog("WinUI.UnhandledException", e.Exception?.ToString() ?? "(null exception)");
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            AppendCrashLog("AppDomain.UnhandledException",
                $"IsTerminating={e.IsTerminating}\n{e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            AppendCrashLog("TaskScheduler.UnobservedTaskException", e.Exception.ToString());
            e.SetObserved();
        };
    }

    private static void AppendCrashLog(string source, string details)
    {
        try
        {
            lock (CrashLogLock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine($"Timestamp : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"Source    : {source}");
                sb.AppendLine($"Process   : {Environment.ProcessId}");
                sb.AppendLine($"Thread    : {Environment.CurrentManagedThreadId}");
                sb.AppendLine("Details:");
                sb.AppendLine(details);
                sb.AppendLine();

                File.WriteAllText(CrashLogPath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Last-resort logging must never throw.
        }
    }
}
