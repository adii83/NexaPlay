using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.ViewModels;
using NexaPlay.Presentation.Views.Dialogs;
using NexaPlay.Presentation.Views.Pages;
using System;

namespace NexaPlay;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly INavigationService _nav;
    private readonly ILicenseService _licenseService;
    private readonly IMetadataService _metadataService;

    public MainWindow(MainViewModel vm, INavigationService nav, ILicenseService licenseService, IMetadataService metadataService)
    {
        _vm             = vm;
        _nav            = nav;
        _licenseService = licenseService;
        _metadataService = metadataService;
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

        _nav.Initialize(ContentFrame);
        ContentFrame.Navigated += ContentFrame_Navigated;
        this.Activated += OnFirstActivated;
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        this.Activated -= OnFirstActivated;

        // Ensure XamlRoot is ready before showing dialogs
        if (ContentFrame.XamlRoot != null)
        {
            _ = ValidateLicenseAsync();
        }
        else
        {
            ContentFrame.Loaded += (s, args) => _ = ValidateLicenseAsync();
        }

        NavigateTo(NavHome);
        _ = WarmupMetadataStartupAsync();
    }

    private async System.Threading.Tasks.Task WarmupMetadataStartupAsync()
    {
        StartupOverlay.Visibility = Visibility.Visible;
        StartupStatusText.Text = "Menyiapkan sumber metadata inti...";
        StartupProgressBar.IsIndeterminate = false;
        StartupProgressBar.Value = 0;
        StartupPercentText.Text = "0%";

        var progress = new Progress<MetadataWarmupProgress>(p =>
        {
            var basePercent = p.TotalFiles <= 0 ? 0 : ((double)p.CompletedFiles / p.TotalFiles) * 100d;
            var fileWeight = p.TotalFiles <= 0 ? 0 : (100d / p.TotalFiles);
            var fileProgressPart = (p.FilePercent ?? 0) / 100d * fileWeight;
            var overall = Math.Clamp(basePercent + fileProgressPart, 0, 100);

            StartupStatusText.Text = p.Message;
            StartupProgressBar.Value = overall;
            StartupPercentText.Text = $"{overall:F0}%";
        });

        try
        {
            await _metadataService.WarmupEssentialSourcesAsync(progress);
            StartupStatusText.Text = "Metadata inti siap.";
            StartupProgressBar.Value = 100;
            StartupPercentText.Text = "100%";
        }
        catch (Exception ex)
        {
            StartupStatusText.Text = $"Warmup metadata gagal: {ex.Message}";
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(350);
            StartupOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private async System.Threading.Tasks.Task ValidateLicenseAsync()
    {
        var license = await _licenseService.LoadAsync();
        if (!license.IsValid)
        {
            ShowLicenseActivation();
        }
    }

    private void Sidebar_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        RootSplitView.IsPaneOpen = true;
        MenuHeader.Visibility = Visibility.Visible;
        AccountHeader.Visibility = Visibility.Visible;
        VersionGrid.Visibility = Visibility.Visible;

        AnimateNavWidth(200);
    }

    private void Sidebar_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        RootSplitView.IsPaneOpen = false;
        MenuHeader.Visibility = Visibility.Collapsed;
        AccountHeader.Visibility = Visibility.Collapsed;
        VersionGrid.Visibility = Visibility.Collapsed;

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
        bool immersiveDetail = e.SourcePageType == typeof(GameDetailPage);
        SetShellDetailMode(immersiveDetail);
    }

    private void SetShellDetailMode(bool enabled)
    {
        if (enabled)
        {
            WindowTopRow.Height = new GridLength(0);
            ContentTopBarRow.Height = new GridLength(0);
            SidebarShell.Visibility = Visibility.Collapsed;
            PageTopBar.Visibility = Visibility.Collapsed;
            RootSplitView.CompactPaneLength = 0;
            RootSplitView.OpenPaneLength = 0;
            RootSplitView.IsPaneOpen = false;
            return;
        }

        WindowTopRow.Height = new GridLength(40);
        ContentTopBarRow.Height = new GridLength(60);
        SidebarShell.Visibility = Visibility.Visible;
        PageTopBar.Visibility = Visibility.Visible;
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

        Type? targetPage = null;
        string title = "Home";

        if (rb == NavHome)      { targetPage = typeof(HomePage);     title = "Home"; }
        else if (rb == NavGames)    { targetPage = typeof(GamesPage);    title = "Games"; }
        else if (rb == NavLibrary)  { targetPage = typeof(LibraryPage);  title = "Library"; }
        else if (rb == NavBypass) { targetPage = typeof(BypassGamesPage); title = "Fix Games"; }
        else if (rb == NavSettings) { targetPage = typeof(SettingsPage); title = "Settings"; }

        if (targetPage is not null && PageTitleText is not null)
        {
            PageTitleText.Text = title;
            ContentFrame.Navigate(targetPage, null, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
        }
    }

    private static void SetNavStyle(RadioButton? rb, TextBlock? label, bool active)
    {
        if (label is null) return;
        label.Style = active
            ? (Microsoft.UI.Xaml.Style)Application.Current.Resources["NavLabelActiveStyle"]
            : (Microsoft.UI.Xaml.Style)Application.Current.Resources["NavLabelStyle"];
    }

    private async void ShowLicenseActivation()
    {
        if (ContentFrame.XamlRoot == null) return;

        var dialog = new LicenseActivationDialog(_licenseService)
        {
            XamlRoot = ContentFrame.XamlRoot
        };

        try 
        {
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("crash_dialog.txt", "Dialog Exception: " + ex.ToString());
        }
    }
}
