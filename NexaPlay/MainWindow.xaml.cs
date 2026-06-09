using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.ViewModels;
using NexaPlay.Presentation.Views.Pages;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NexaPlay;

public sealed partial class MainWindow : Window
{
    private const uint WM_SETICON = 0x0080;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    private readonly MainViewModel _vm;
    private readonly INavigationService _nav;
    private readonly ILicenseService _licenseService;
    private readonly IMetadataService _metadataService;
    private readonly IAppUpdateService _appUpdateService;
    private readonly IAppLogService _appLog;
    private bool _hasShownStartupUpdatePrompt;
    private IntPtr _windowIconHandle;

    public MainWindow(
        MainViewModel vm,
        INavigationService nav,
        ILicenseService licenseService,
        IMetadataService metadataService,
        IAppUpdateService appUpdateService,
        IAppLogService appLog)
    {
        _vm             = vm;
        _nav            = nav;
        _licenseService = licenseService;
        _metadataService = metadataService;
        _appUpdateService = appUpdateService;
        _appLog = appLog;
        InitializeComponent();
        
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        
        // Set default window size larger
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
        int width = (int)(displayArea.WorkArea.Width * 0.95);
        int height = (int)(displayArea.WorkArea.Height * 0.95);
        int x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - width) / 2;
        int y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - height) / 2;
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
        ApplyWindowIcon(appWindow, hWnd);

        _nav.Initialize(ContentFrame);
        ContentFrame.Navigated += ContentFrame_Navigated;
        this.Activated += OnFirstActivated;
        SetBypassSubmenuActive("all");
        UpdateShellChrome();
    }

    private void ApplyWindowIcon(Microsoft.UI.Windowing.AppWindow appWindow, IntPtr hWnd)
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "app.ico");
            if (File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
                ApplyHwndIcon(hWnd, iconPath);
            }
        }
        catch (Exception ex)
        {
            _appLog.Log("Window", $"Gagal menerapkan icon window: {ex.Message}");
        }
    }

    private void ApplyHwndIcon(IntPtr hWnd, string iconPath)
    {
        try
        {
            _windowIconHandle = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
            if (_windowIconHandle == IntPtr.Zero)
            {
                return;
            }

            SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_SMALL, _windowIconHandle);
            SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_BIG, _windowIconHandle);
        }
        catch (Exception ex)
        {
            _appLog.Log("Window", $"Gagal menerapkan HWND icon: {ex.Message}");
        }
    }

    [DllImport("user32.dll", EntryPoint = "LoadImageW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(
        IntPtr hInst,
        string lpszName,
        uint uType,
        int cxDesired,
        int cyDesired,
        uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private async void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        this.Activated -= OnFirstActivated;
        LogLicenseFlow("OnFirstActivated entered");

        // Ensure XamlRoot is ready before showing dialogs
        if (ContentFrame.XamlRoot != null)
        {
            LogLicenseFlow("ContentFrame.XamlRoot ready on first activation");
            await RunStartupLicenseFlowAsync("first activation");
        }
        else
        {
            LogLicenseFlow("ContentFrame.XamlRoot not ready, waiting for Loaded");
            ContentFrame.Loaded += async (s, args) => 
            {
                LogLicenseFlow("ContentFrame.Loaded fired for first activation");
                await RunStartupLicenseFlowAsync("delayed first activation");
            };
        }
    }

    private async System.Threading.Tasks.Task RunStartupLicenseFlowAsync(string source)
    {
        try
        {
            LogLicenseFlow($"RunStartupLicenseFlowAsync started from {source}");
            await ValidateLicenseAsync();
        }
        catch (Exception ex)
        {
            LogLicenseFlow($"Startup license flow exception from {source}: {ex}");
            await ShowLicenseActivationFallbackAsync();
        }

        DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                LogLicenseFlow($"Navigating to Home after startup license flow ({source})");
                NavigateTo(NavHome);
                await WarmupMetadataStartupAsync();
                await WaitForHomeReadyAsync();
                await PromptForStartupUpdateAsync();
            }
            catch (Exception ex)
            {
                LogLicenseFlow($"Startup navigation/warmup exception from {source}: {ex}");
            }
        });
    }

    private async System.Threading.Tasks.Task WarmupMetadataStartupAsync()
    {
        bool isWarm = _metadataService.IsCacheAvailable;
        var dispatcher = this.DispatcherQueue;

        dispatcher.TryEnqueue(() =>
        {
            StartupOverlay.Visibility = Visibility.Visible;
            UpdateShellChrome();
            StartupProgressBar.IsIndeterminate = false;
            StartupProgressBar.Value = 0;

            if (isWarm)
            {
                StartupTitleText.Visibility = Visibility.Collapsed;
                StartupStatusText.Visibility = Visibility.Collapsed;
                StartupPercentText.Visibility = Visibility.Collapsed;
            }
            else
            {
                StartupTitleText.Text = "Menyiapkan NexaPlay";
                StartupStatusText.Text = "Menghubungkan ke server...";
                StartupPercentText.Text = "0%";

                StartupTitleText.Visibility = Visibility.Visible;
                StartupStatusText.Visibility = Visibility.Visible;
                StartupPercentText.Visibility = Visibility.Visible;
            }
        });

        // Minimum time we want to show the loading screen (1.2 seconds)
        var minDuration = TimeSpan.FromMilliseconds(1200);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (isWarm)
        {
            // Run the actual metadata warmup in background
            var warmupTask = _metadataService.WarmupEssentialSourcesAsync(null);

            // Animate progress bar smoothly from 0 to 100 over 1200ms
            const int steps = 40;
            var stepDelay = (int)(minDuration.TotalMilliseconds / steps);
            for (int i = 1; i <= steps; i++)
            {
                var val = (double)i / steps * 100d;
                dispatcher.TryEnqueue(() => StartupProgressBar.Value = val);
                await System.Threading.Tasks.Task.Delay(stepDelay);
            }

            await warmupTask;
        }
        else
        {
            // Cold boot: actual progress from downloads
            var progress = new Progress<MetadataWarmupProgress>(p =>
            {
                var basePercent = p.TotalFiles <= 0 ? 0 : ((double)p.CompletedFiles / p.TotalFiles) * 100d;
                var fileWeight = p.TotalFiles <= 0 ? 0 : (100d / p.TotalFiles);
                var fileProgressPart = (p.FilePercent ?? 0) / 100d * fileWeight;
                var overall = Math.Clamp(basePercent + fileProgressPart, 0, 100);

                dispatcher.TryEnqueue(() =>
                {
                    string friendlyMessage = p.FileName switch
                    {
                        "steam_data.json" or "steam_data.json.gz" => "Menyiapkan database game...",
                        "override_data.json" => "Mengunduh konfigurasi...",
                        "fix_games.json" or "new_fix_games.json" => "Menyiapkan bypass games...",
                        "steam_games.json" => "Memverifikasi database Denuvo...",
                        _ => "Sedang menyiapkan NexaPlay..."
                    };
                    StartupStatusText.Text = $"{friendlyMessage} ({p.CompletedFiles}/{p.TotalFiles})";
                    StartupPercentText.Text = $"{overall:F0}%";
                    StartupProgressBar.Value = overall;
                });
            });

            try
            {
                await _metadataService.WarmupEssentialSourcesAsync(progress);
                dispatcher.TryEnqueue(() =>
                {
                    StartupStatusText.Text = "NexaPlay siap!";
                    StartupPercentText.Text = "100%";
                    StartupProgressBar.Value = 100;
                });
            }
            catch (Exception ex)
            {
                dispatcher.TryEnqueue(() =>
                {
                    StartupStatusText.Text = $"Gagal menyiapkan data: {ex.Message}";
                });
            }
        }

        stopwatch.Stop();
        var remaining = minDuration - stopwatch.Elapsed;
        if (remaining > TimeSpan.Zero)
        {
            await System.Threading.Tasks.Task.Delay(remaining);
        }

        // Wait slightly for visual completion before collapsing
        await System.Threading.Tasks.Task.Delay(150);
        var hideOverlayTcs = new TaskCompletionSource();
        dispatcher.TryEnqueue(() =>
        {
            StartupOverlay.Visibility = Visibility.Collapsed;
            UpdateShellChrome();
            hideOverlayTcs.TrySetResult();
        });
        await hideOverlayTcs.Task;
    }

    private async Task WaitForHomeReadyAsync()
    {
        const int maxAttempts = 20;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var isHomeVisible = ContentFrame.CurrentSourcePageType == typeof(HomePage)
                                && StartupOverlay.Visibility != Visibility.Visible
                                && LicenseOverlay.Visibility != Visibility.Visible
                                && ValidatingLicenseOverlay.Visibility != Visibility.Visible
                                && ContentFrame.XamlRoot is not null;

            if (isHomeVisible)
            {
                // Give WinUI a brief chance to paint Home first before showing the dialog.
                await Task.Delay(300);
                return;
            }

            await Task.Delay(100);
        }
    }

    private async Task PromptForStartupUpdateAsync()
    {
        if (_hasShownStartupUpdatePrompt)
        {
            return;
        }

        _hasShownStartupUpdatePrompt = true;

        try
        {
            var result = await _appUpdateService.CheckForUpdatesAsync(force: false);
            if (!result.IsUpdateAvailable || ContentFrame.XamlRoot is null)
            {
                return;
            }

            var content = new StackPanel
            {
                Spacing = 16
            };

            content.Children.Add(new TextBlock
            {
                Text = $"Versi baru {result.LatestVersion} siap dipasang.",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            content.Children.Add(new TextBlock
            {
                Text = $"Versi saat ini {result.CurrentVersion}. Jika Anda melanjutkan, NexaPlay akan mengunduh setup, menutup aplikasi, lalu membuka installer agar Anda bisa melanjutkan pemasangan manual.",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 160, 160, 160)),
                TextWrapping = TextWrapping.Wrap
            });

            if (result.ReleaseNotes.Count > 0)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "Release Notes",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });

                foreach (var note in result.ReleaseNotes)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = $"• {note}",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 160, 160, 160)),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }

            var dialog = CreateStyledDialog(
                "Update Tersedia",
                content,
                primaryButtonText: "Update Sekarang",
                closeButtonText: "Nanti");
            dialog.DefaultButton = ContentDialogButton.Primary;

            var dialogResult = await dialog.ShowAsync();
            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            await DownloadAndInstallUpdateAsync(result);
        }
        catch (Exception ex)
        {
            _appLog.Log("Update", $"Startup update prompt gagal: {ex.Message}");
        }
    }

    private async Task DownloadAndInstallUpdateAsync(AppUpdateCheckResult result)
    {
        StartupOverlay.Visibility = Visibility.Visible;
        UpdateShellChrome();
        StartupTitleText.Text = "Menyiapkan Update";
        StartupStatusText.Text = $"Mengunduh NexaPlay {result.LatestVersion}...";
        StartupPercentText.Text = "0%";
        StartupTitleText.Visibility = Visibility.Visible;
        StartupStatusText.Visibility = Visibility.Visible;
        StartupPercentText.Visibility = Visibility.Visible;
        StartupProgressBar.IsIndeterminate = false;
        StartupProgressBar.Value = 0;

        try
        {
            var progress = new Progress<double>(value =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    StartupProgressBar.Value = Math.Clamp(value, 0, 100);
                    StartupPercentText.Text = $"{StartupProgressBar.Value:F0}%";
                    StartupStatusText.Text = $"Mengunduh NexaPlay {result.LatestVersion}...";
                });
            });

            var installerPath = await _appUpdateService.DownloadInstallerAsync(result, progress);

            DispatcherQueue.TryEnqueue(() =>
            {
                StartupProgressBar.Value = 100;
                StartupPercentText.Text = "100%";
                StartupStatusText.Text = "Membuka installer update...";
            });

            await _appUpdateService.LaunchInstallerAndExitAsync(installerPath);
        }
        catch (Exception ex)
        {
            StartupOverlay.Visibility = Visibility.Collapsed;
            UpdateShellChrome();
            await ShowStyledDialogAsync(
                "Update Gagal",
                $"NexaPlay gagal memulai update.\n\n{ex.Message}",
                closeButtonText: "Tutup");
        }
    }

    private async System.Threading.Tasks.Task ValidateLicenseAsync()
    {
        try
        {
            LogLicenseFlow("ValidateLicenseAsync entered");
            var license = await _licenseService.LoadAsync();
            LogLicenseFlow($"ValidateLicenseAsync: offline status={license.Status}, isValid={license.IsValid}, hasKey={!string.IsNullOrWhiteSpace(license.Key)}");
            if (!license.IsValid)
            {
                LogLicenseFlow("Offline license invalid, showing activation overlay immediately");
                await ShowLicenseActivation();
                return;
            }

            // License valid offline, show validating overlay
            LogLicenseFlow("Offline license valid, showing validating overlay");
            ValidatingLicenseOverlay.Visibility = Visibility.Visible;
            UpdateShellChrome();
            ValidationProgressRing.Visibility = Visibility.Visible;
            ValidationStatusText.Visibility = Visibility.Visible;
            ValidationErrorPanel.Visibility = Visibility.Collapsed;

            var result = await _licenseService.ValidateExistingAsync();
            LogLicenseFlow($"ValidateExistingAsync completed: status={result.Status}, isValid={result.IsValid}, message={result.Message ?? "(null)"}");

            var tcs = new System.Threading.Tasks.TaskCompletionSource();
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    LogLicenseFlow("Entered UI dispatcher continuation for license validation result");
                    if (result.IsValid)
                    {
                        // Valid online, proceed to home
                        LogLicenseFlow("License valid online, collapsing validating overlay");
                        ValidatingLicenseOverlay.Visibility = Visibility.Collapsed;
                        UpdateShellChrome();
                        tcs.SetResult();
                        return;
                    }

                    // Handle errors
                    ValidationProgressRing.Visibility = Visibility.Collapsed;
                    ValidationStatusText.Visibility = Visibility.Collapsed;
                    ValidationErrorPanel.Visibility = Visibility.Visible;

                    if (result.Status == NexaPlay.Core.Enums.LicenseStatus.Banned ||
                        result.Status == NexaPlay.Core.Enums.LicenseStatus.Reset ||
                        result.Status == NexaPlay.Core.Enums.LicenseStatus.NotFound)
                    {
                        // License was banned/reset/deleted from server, ask for new key
                        LogLicenseFlow($"License status {result.Status}, collapsing validating overlay and showing activation overlay");
                        ValidatingLicenseOverlay.Visibility = Visibility.Collapsed;
                        UpdateShellChrome();
                        await ShowLicenseActivation();
                        tcs.SetResult();
                        return;
                    }

                    // Show error message
                    ValidationErrorMessage.Text = result.Status switch
                    {
                        NexaPlay.Core.Enums.LicenseStatus.DeviceMismatch => "License terikat dengan perangkat lain.",
                        NexaPlay.Core.Enums.LicenseStatus.NetworkError   => "Kesalahan jaringan. Periksa koneksi internet Anda.",
                        NexaPlay.Core.Enums.LicenseStatus.Offline        => "Koneksi ke server timeout (Offline mode).",
                        _ => result.Message ?? "Gagal memvalidasi lisensi."
                    };
                    LogLicenseFlow($"Validation non-fatal error shown to user: status={result.Status}");
                    
                    // Wait for user to click Retry or Enter New Key
                    _licenseTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                    await _licenseTcs.Task;
                    
                    LogLicenseFlow("Validation error panel completed by user action");
                    ValidatingLicenseOverlay.Visibility = Visibility.Collapsed;
                    UpdateShellChrome();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    LogLicenseFlow($"Exception inside validation UI dispatcher continuation: {ex}");
                    System.Diagnostics.Debug.WriteLine($"[License UI Error] {ex}");
                    ValidationErrorMessage.Text = $"Internal UI Error: {ex.Message}";
                    ValidationErrorPanel.Visibility = Visibility.Visible;
                    ValidatingLicenseOverlay.Visibility = Visibility.Visible;
                    UpdateShellChrome();
                    
                    // Keep the app alive, wait for user input
                    _licenseTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                    await _licenseTcs.Task;
                    
                    tcs.TrySetResult();
                }
            });

            await tcs.Task;
        }
        catch (Exception ex)
        {
            LogLicenseFlow($"ValidateLicenseAsync outer exception: {ex}");
            await ShowLicenseActivationFallbackAsync();
        }
    }

    private async System.Threading.Tasks.Task ShowLicenseActivationFallbackAsync()
    {
        try
        {
            LogLicenseFlow("ShowLicenseActivationFallbackAsync entered");
            ValidatingLicenseOverlay.Visibility = Visibility.Collapsed;
            UpdateShellChrome();
            ValidationProgressRing.Visibility = Visibility.Visible;
            ValidationStatusText.Visibility = Visibility.Visible;
            ValidationErrorPanel.Visibility = Visibility.Collapsed;
            await ShowLicenseActivation();
        }
        catch (Exception ex)
        {
            LogLicenseFlow($"ShowLicenseActivationFallbackAsync exception: {ex}");
        }
    }

    private async void OnValidationRetryClicked(object sender, RoutedEventArgs e)
    {
        LogLicenseFlow("Validation retry clicked");
        ValidatingLicenseOverlay.Visibility = Visibility.Collapsed;
        UpdateShellChrome();
        _licenseTcs?.TrySetResult(true);
        await ValidateLicenseAsync();
    }

    private void OnValidationEnterKeyClicked(object sender, RoutedEventArgs e)
    {
        LogLicenseFlow("Validation enter-new-key clicked");
        ValidatingLicenseOverlay.Visibility = Visibility.Collapsed;
        UpdateShellChrome();
        _licenseTcs?.TrySetResult(true);
        _ = ShowLicenseActivation();
    }

    private void Sidebar_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        RootSplitView.IsPaneOpen = true;
        MenuHeader.Visibility = Visibility.Visible;
        AccountHeader.Visibility = Visibility.Visible;
        VersionGrid.Visibility = Visibility.Visible;
        UpdateBypassSubmenuVisibility();

        AnimateNavWidth(200);
    }

    private void Sidebar_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        RootSplitView.IsPaneOpen = false;
        MenuHeader.Visibility = Visibility.Collapsed;
        AccountHeader.Visibility = Visibility.Collapsed;
        VersionGrid.Visibility = Visibility.Collapsed;
        BypassSubmenuPanel.Visibility = Visibility.Collapsed;

        AnimateNavWidth(68);
    }

    private void AnimateNavWidth(double toWidth)
    {
        var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        var animation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            To = toWidth,
            Duration = new TimeSpan(0, 0, 0, 0, 200),
            EnableDependentAnimation = true,
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.ExponentialEase 
            { 
                EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, 
                Exponent = 4 
            }
        };

        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, NavItemsPanel);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "Width");

        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb) NavigateTo(rb);
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        UpdateShellChrome(e.SourcePageType);
    }

    private void UpdateShellChrome(Type? currentPageType = null)
    {
        currentPageType ??= ContentFrame?.CurrentSourcePageType;

        bool overlayActive = StartupOverlay.Visibility == Visibility.Visible
                             || LicenseOverlay.Visibility == Visibility.Visible
                             || ValidatingLicenseOverlay.Visibility == Visibility.Visible;
        bool immersiveDetail = currentPageType == typeof(GameDetailPage)
                               || currentPageType == typeof(BypassGameDetailPage);

        if (overlayActive)
        {
            WindowTopRow.Height = new GridLength(40);
            ContentTopBarRow.Height = new GridLength(0);
            SidebarShell.Visibility = Visibility.Collapsed;
            PageTopBar.Visibility = Visibility.Collapsed;
            RootSplitView.DisplayMode = SplitViewDisplayMode.Overlay;
            RootSplitView.CompactPaneLength = 0;
            RootSplitView.OpenPaneLength = 0;
            RootSplitView.IsPaneOpen = false;
            return;
        }

        if (immersiveDetail)
        {
            WindowTopRow.Height = new GridLength(0);
            ContentTopBarRow.Height = new GridLength(0);
            SidebarShell.Visibility = Visibility.Collapsed;
            PageTopBar.Visibility = Visibility.Collapsed;
            RootSplitView.DisplayMode = SplitViewDisplayMode.Overlay;
            RootSplitView.CompactPaneLength = 0;
            RootSplitView.OpenPaneLength = 0;
            RootSplitView.IsPaneOpen = false;
            return;
        }

        WindowTopRow.Height = new GridLength(40);
        ContentTopBarRow.Height = new GridLength(0);
        SidebarShell.Visibility = Visibility.Visible;
        PageTopBar.Visibility = Visibility.Collapsed;
        RootSplitView.DisplayMode = SplitViewDisplayMode.CompactInline;
        RootSplitView.CompactPaneLength = 68;
        RootSplitView.OpenPaneLength = 200;
    }

    private void NavigateTo(RadioButton rb)
    {
        if (ContentFrame is null) return;

        // Update all label styles
        SetNavStyle(NavHome,     LblHome,     rb == NavHome);
        SetNavStyle(NavGames,    LblGames,    rb == NavGames);
        SetNavStyle(NavLibrary,  LblLibrary,  rb == NavLibrary);
        SetNavStyle(NavBypass, LblBypassGames, rb == NavBypass);
        SetNavStyle(NavSettings, LblSettings, rb == NavSettings);
        UpdateBypassSubmenuVisibility();

        Type? targetPage = null;

        if (rb == NavHome)      { targetPage = typeof(HomePage); }
        else if (rb == NavGames)    { targetPage = typeof(GamesPage); }
        else if (rb == NavLibrary)  { targetPage = typeof(LibraryPage); }
        else if (rb == NavBypass) { targetPage = typeof(BypassGamesPage); }
        else if (rb == NavSettings) { targetPage = typeof(SettingsPage); }

        if (targetPage is not null)
        {
            if (rb == NavBypass)
            {
                SetBypassSubmenuActive("all");
            }

            ContentFrame.Navigate(targetPage, null, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
        }
    }

    private void OnBypassSubmenuClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string category)
            return;

        if (NavBypass is not null)
        {
            NavBypass.IsChecked = true;
            SetNavStyle(NavHome, LblHome, false);
            SetNavStyle(NavGames, LblGames, false);
            SetNavStyle(NavLibrary, LblLibrary, false);
            SetNavStyle(NavBypass, LblBypassGames, true);
            SetNavStyle(NavSettings, LblSettings, false);
        }

        ContentFrame.Navigate(typeof(BypassGamesPage), category, new SlideNavigationTransitionInfo
        {
            Effect = SlideNavigationTransitionEffect.FromRight
        });
        SetBypassSubmenuActive(category);
    }

    private static void SetNavStyle(RadioButton? rb, TextBlock? label, bool active)
    {
        if (label is null) return;
        label.Style = active
            ? (Microsoft.UI.Xaml.Style)Application.Current.Resources["NavLabelActiveStyle"]
            : (Microsoft.UI.Xaml.Style)Application.Current.Resources["NavLabelStyle"];
    }

    private void UpdateBypassSubmenuVisibility()
    {
        if (BypassSubmenuPanel is null)
            return;

        BypassSubmenuPanel.Visibility = RootSplitView.IsPaneOpen
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SetBypassSubmenuActive(string category)
    {
        if (BypassSubmenuThirdParty is null || BypassSubmenuSteam is null)
            return;

        var isSteam = string.Equals(category, "steam-sharing", StringComparison.OrdinalIgnoreCase);

        // Active background: #1AFFFFFF, Inactive: Transparent
        BypassSubmenuThirdParty.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            isSteam ? Windows.UI.Color.FromArgb(0x00, 0x00, 0x00, 0x00) : Windows.UI.Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
        BypassSubmenuSteam.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            isSteam ? Windows.UI.Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF) : Windows.UI.Color.FromArgb(0x00, 0x00, 0x00, 0x00));

        // Active foreground: Primary (White), Inactive: Secondary (Gray)
        var activeBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["NexaTextPrimaryBrush"];
        var inactiveBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["NexaTextSecondaryBrush"];

        BypassSubmenuThirdParty.Foreground = isSteam ? inactiveBrush : activeBrush;
        BypassSubmenuSteam.Foreground = isSteam ? activeBrush : inactiveBrush;

        // Also update icon colors
        if (BypassSubmenuThirdParty.Content is StackPanel sp3rd && sp3rd.Children.Count > 0 && sp3rd.Children[0] is FontIcon icon3rd)
        {
            icon3rd.Foreground = BypassSubmenuThirdParty.Foreground;
        }
        if (BypassSubmenuSteam.Content is StackPanel spSteam && spSteam.Children.Count > 0 && spSteam.Children[0] is FontIcon iconSteam)
        {
            iconSteam.Foreground = BypassSubmenuSteam.Foreground;
        }
    }

    private System.Threading.Tasks.TaskCompletionSource<bool>? _licenseTcs;

    private async System.Threading.Tasks.Task ShowLicenseActivation()
    {
        LogLicenseFlow("ShowLicenseActivation: opening activation overlay");
        LicenseOverlay.Visibility = Visibility.Visible;
        UpdateShellChrome();
        // Wait for license completion
        _licenseTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        await _licenseTcs.Task;
        LogLicenseFlow("ShowLicenseActivation: activation overlay completed");
        LicenseOverlay.Visibility = Visibility.Collapsed;
        UpdateShellChrome();
    }

    private async Task ShowStyledDialogAsync(string title, string message, string closeButtonText)
    {
        var dialog = CreateStyledDialog(title, new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 160, 160, 160))
        }, closeButtonText: closeButtonText);

        await dialog.ShowAsync();
    }

    private ContentDialog CreateStyledDialog(
        string title,
        object content,
        string? primaryButtonText = null,
        string? closeButtonText = null)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText ?? string.Empty,
            CloseButtonText = closeButtonText ?? string.Empty,
            XamlRoot = ContentFrame.XamlRoot,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 17, 17))
        };

        dialog.Resources["ContentDialogBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 17, 17));
        dialog.Resources["ContentDialogBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 34, 34));
        dialog.Resources["ContentDialogForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        dialog.Resources["AccentButtonBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        dialog.Resources["AccentButtonForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        dialog.Resources["AccentButtonBackgroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(220, 255, 255, 255));
        dialog.Resources["AccentButtonForegroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        dialog.Resources["AccentButtonBackgroundPressed"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(180, 255, 255, 255));
        dialog.Resources["AccentButtonForegroundPressed"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        dialog.Resources["ButtonForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        dialog.Resources["ButtonBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

        return dialog;
    }

    private async void OnActivateClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var key = LicenseKeyBox.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(key))
            {
                LogLicenseFlow("Activate clicked with empty key");
                ShowLicenseStatus(false, "&#xE783;", "Masukkan license key Anda.", "#EF4444", "#18EF4444");
                return;
            }

            LogLicenseFlow("Activate clicked with non-empty key");
            SetLicenseLoading(true);
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(12));
            var result = await _licenseService.ActivateAsync(key, cts.Token);
            LogLicenseFlow($"ActivateAsync completed: status={result.Status}, isValid={result.IsValid}, message={result.Message ?? "(null)"}");
            SetLicenseLoading(false);

            if (result.IsValid)
            {
                ShowLicenseStatus(true, "&#xE73E;", $"Aktivasi berhasil! Paket: {result.Plan}", "#22C55E", "#1822C55E");
                await System.Threading.Tasks.Task.Delay(1000);
                _licenseTcs?.TrySetResult(true);
            }
            else
            {
                var msg = result.Status switch
                {
                    NexaPlay.Core.Enums.LicenseStatus.Banned         => "License key ini telah diblokir.",
                    NexaPlay.Core.Enums.LicenseStatus.DeviceMismatch => "License terikat dengan perangkat lain.",
                    NexaPlay.Core.Enums.LicenseStatus.Offline        => "Tidak dapat terhubung ke server. Disimpan untuk offline.",
                    NexaPlay.Core.Enums.LicenseStatus.NetworkError   => "Kesalahan jaringan. Periksa koneksi dan coba lagi.",
                    _                                                => result.Message ?? "License key tidak valid. Silakan periksa kembali."
                };
                ShowLicenseStatus(false, "&#xEA39;", msg, "#EF4444", "#18EF4444");
            }
        }
        catch (Exception ex)
        {
            LogLicenseFlow($"OnActivateClicked exception: {ex}");
            SetLicenseLoading(false);
            ShowLicenseStatus(false, "&#xEA39;", $"Terjadi kesalahan: {ex.Message}", "#EF4444", "#18EF4444");
        }
    }

    private void LogLicenseFlow(string message)
    {
        try
        {
            _appLog.Log("LicenseFlow", message);
        }
        catch
        {
        }
    }

    private void SetLicenseLoading(bool loading)
    {
        LicenseBtnText.Visibility = loading ? Visibility.Collapsed : Visibility.Visible;
        LicenseLoadingRing.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        LicenseLoadingRing.IsActive = loading;
        
        LicenseStatusBorder.Visibility  = loading ? Visibility.Collapsed : LicenseStatusBorder.Visibility;
        ActivateButton.IsEnabled = !loading;
        LicenseKeyBox.IsEnabled  = !loading;
    }

    private void ShowLicenseStatus(bool success, string glyph, string message, string fgColor, string bgColor)
    {
        LicenseStatusBorder.Visibility  = Visibility.Visible;
        LicenseStatusBorder.Background  = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseColor(bgColor));
        LicenseStatusIcon.Glyph         = System.Text.RegularExpressions.Regex.Unescape(glyph.Replace("&#x", "\\u").Replace(";", ""));
        LicenseStatusIcon.Foreground    = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseColor(fgColor));
        LicenseStatusText.Text          = message;
        LicenseStatusText.Foreground    = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseColor(fgColor));
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        return Windows.UI.Color.FromArgb(
            Convert.ToByte(hex[0..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16),
            Convert.ToByte(hex[6..8], 16));
    }
}
