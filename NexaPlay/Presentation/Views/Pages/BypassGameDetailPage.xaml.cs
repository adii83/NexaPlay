using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Presentation.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;
using Windows.Storage.Pickers;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class BypassGameDetailPage : Page
{
    public BypassGameDetailViewModel ViewModel { get; }
    private readonly INavigationService _nav;
    private Storyboard? _shimmerStoryboard;
    private Storyboard? _heroShimmerStoryboard;
    private Storyboard? _topIconShimmerStoryboard;
    private bool _tutorialWebViewInitialized;
    private bool _tutorialVirtualHostMapped;

    public BypassGameDetailPage()
    {
        ViewModel = ((App)App.Current).GetRequiredService<BypassGameDetailViewModel>();
        _nav = ((App)App.Current).GetRequiredService<INavigationService>();
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.ConfirmAsync = ShowConfirmAsync;
        ViewModel.SelectFolderAsync = SelectManualFolderAsync;
        ViewModel.ShowDialogAsync = ShowInfoDialogAsync;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Unloaded += OnPageUnloaded;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ValueTuple<int, Core.Models.FixEntry?> payload)
        {
            await ViewModel.LoadAsync(payload.Item1, payload.Item2);
        }
        else if (e.Parameter is int appId)
        {
            await ViewModel.LoadAsync(appId);
        }
    }

    private void RootLayout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var width = e.NewSize.Width;
        HeroRowDefinition.Height = new GridLength(Math.Max(420, width * 1240 / 3840));

        // Update shimmer clip to match actual size
        if (ShimmerClipGeometry != null)
        {
            ShimmerClipGeometry.Rect = new Windows.Foundation.Rect(0, 0, width, e.NewSize.Height);
        }

        // Update shimmer animation range if running
        UpdateShimmerAnimation(width);
        UpdateHeroShimmerAnimation(width);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BypassGameDetailViewModel.HeroBackgroundUrl) or nameof(BypassGameDetailViewModel.IsDetailLoading))
        {
            ShowHeroSkeleton();
        }
    }

    private void ShimmerContainer_Loaded(object sender, RoutedEventArgs e)
    {
        StartShimmerAnimation();
    }

    private void ShimmerContainer_Unloaded(object sender, RoutedEventArgs e)
    {
        StopShimmerAnimation();
    }

    private void StartShimmerAnimation()
    {
        var width = RootLayout.ActualWidth > 0 ? RootLayout.ActualWidth : 1400;
        _shimmerStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var anim = new DoubleAnimation
        {
            From = -600,
            To = width + 100,
            Duration = TimeSpan.FromMilliseconds(1400),
        };
        Storyboard.SetTarget(anim, ShimmerTranslate);
        Storyboard.SetTargetProperty(anim, "X");
        _shimmerStoryboard.Children.Add(anim);
        _shimmerStoryboard.Begin();
    }

    private void UpdateShimmerAnimation(double width)
    {
        if (_shimmerStoryboard == null) return;
        _shimmerStoryboard.Stop();
        _shimmerStoryboard.Children.Clear();
        var anim = new DoubleAnimation
        {
            From = -600,
            To = width + 100,
            Duration = TimeSpan.FromMilliseconds(1400),
        };
        Storyboard.SetTarget(anim, ShimmerTranslate);
        Storyboard.SetTargetProperty(anim, "X");
        _shimmerStoryboard.Children.Add(anim);
        _shimmerStoryboard.Begin();
    }

    private void StopShimmerAnimation()
    {
        _shimmerStoryboard?.Stop();
        _shimmerStoryboard = null;
    }

    private void ShowHeroSkeleton()
    {
        if (HeroSkeletonOverlay is null || HeroImage is null) return;
        HeroSkeletonOverlay.Visibility = Visibility.Visible;
        if (HeroShimmerSweep is not null)
        {
            HeroShimmerSweep.Visibility = Visibility.Visible;
        }
        HeroImage.Visibility = Visibility.Visible;
        HeroImage.Opacity = 0;
        StartHeroShimmerAnimation();
    }

    private void HideHeroSkeleton()
    {
        if (HeroSkeletonOverlay is null || HeroImage is null) return;
        HeroImage.Visibility = Visibility.Visible;
        HeroImage.Opacity = 1;
        HeroSkeletonOverlay.Visibility = Visibility.Collapsed;
        StopHeroShimmerAnimation();
    }

    private void StartHeroShimmerAnimation()
    {
        if (HeroSkeletonOverlay is null || HeroSkeletonOverlay.Visibility != Visibility.Visible) return;

        var width = RootLayout.ActualWidth > 0 ? RootLayout.ActualWidth : 1400;
        _heroShimmerStoryboard?.Stop();
        _heroShimmerStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var anim = new DoubleAnimation
        {
            From = -560,
            To = width + 100,
            Duration = TimeSpan.FromMilliseconds(1250),
        };
        Storyboard.SetTarget(anim, HeroShimmerTranslate);
        Storyboard.SetTargetProperty(anim, "X");
        _heroShimmerStoryboard.Children.Add(anim);
        _heroShimmerStoryboard.Begin();
    }

    private void UpdateHeroShimmerAnimation(double width)
    {
        if (_heroShimmerStoryboard == null) return;
        _heroShimmerStoryboard.Stop();
        _heroShimmerStoryboard.Children.Clear();
        var anim = new DoubleAnimation
        {
            From = -560,
            To = width + 100,
            Duration = TimeSpan.FromMilliseconds(1250),
        };
        Storyboard.SetTarget(anim, HeroShimmerTranslate);
        Storyboard.SetTargetProperty(anim, "X");
        _heroShimmerStoryboard.Children.Add(anim);
        _heroShimmerStoryboard.Begin();
    }

    private void StopHeroShimmerAnimation()
    {
        _heroShimmerStoryboard?.Stop();
        _heroShimmerStoryboard = null;
    }

    private void TopIconImage_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Image image || TopIconSkeletonOverlay is null)
            return;

        image.Visibility = Visibility.Visible;
        image.Opacity = 0;
        TopIconSkeletonOverlay.Visibility = Visibility.Visible;
        StartTopIconShimmerAnimation();
    }

    private void TopIconImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not Image image || TopIconSkeletonOverlay is null)
            return;

        image.Visibility = Visibility.Visible;
        image.Opacity = 1;
        TopIconSkeletonOverlay.Visibility = Visibility.Collapsed;
        StopTopIconShimmerAnimation();
    }

    private void TopIconImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is not Image image || TopIconSkeletonOverlay is null)
            return;

        image.Visibility = Visibility.Collapsed;
        image.Opacity = 0;
        TopIconSkeletonOverlay.Visibility = Visibility.Visible;
        StopTopIconShimmerAnimation();
        if (TopIconShimmerSweep is not null)
        {
            TopIconShimmerSweep.Visibility = Visibility.Collapsed;
        }
    }

    private void StartTopIconShimmerAnimation()
    {
        if (TopIconShimmerSweep is null || TopIconShimmerTranslate is null)
            return;

        TopIconShimmerSweep.Visibility = Visibility.Visible;
        _topIconShimmerStoryboard?.Stop();
        _topIconShimmerStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var anim = new DoubleAnimation
        {
            From = -26,
            To = 40,
            Duration = TimeSpan.FromMilliseconds(900)
        };
        Storyboard.SetTarget(anim, TopIconShimmerTranslate);
        Storyboard.SetTargetProperty(anim, "X");
        _topIconShimmerStoryboard.Children.Add(anim);
        _topIconShimmerStoryboard.Begin();
    }

    private void StopTopIconShimmerAnimation()
    {
        _topIconShimmerStoryboard?.Stop();
        _topIconShimmerStoryboard = null;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        StopTutorialVideoPlayback();
        if (_nav.CanGoBack)
        {
            _nav.GoBack();
        }
    }

    private void HeroImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        HideHeroSkeleton();
    }

    private void HeroImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (HeroImage is null || HeroSkeletonOverlay is null)
            return;

        HeroImage.Visibility = Visibility.Collapsed;
        HeroImage.Opacity = 0;
        HeroSkeletonOverlay.Visibility = Visibility.Visible;
        StopHeroShimmerAnimation();
        if (HeroShimmerSweep is not null)
        {
            HeroShimmerSweep.Visibility = Visibility.Collapsed;
        }
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        StopTutorialVideoPlayback();
        StopHeroShimmerAnimation();
        StopTopIconShimmerAnimation();
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        Unloaded -= OnPageUnloaded;
    }

    private void CoverArtImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not Image img) return;
        img.Opacity = 1;
        if (CoverArtSkeleton is not null)
            CoverArtSkeleton.Visibility = Visibility.Collapsed;
    }

    private void CoverArtImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is not Image img) return;
        img.Visibility = Visibility.Collapsed;
        // Keep skeleton visible as fallback
        if (CoverArtSkeleton is not null)
            CoverArtSkeleton.Visibility = Visibility.Visible;
    }

    private void CoverArtCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (CoverArtHoverOverlay is null) return;
        var sb = new Storyboard();
        var anim = new DoubleAnimation { To = 1.0, Duration = TimeSpan.FromMilliseconds(160) };
        anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(anim, CoverArtHoverOverlay);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);
        sb.Begin();
    }

    private void CoverArtCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (CoverArtHoverOverlay is null) return;
        var sb = new Storyboard();
        var anim = new DoubleAnimation { To = 0.0, Duration = TimeSpan.FromMilliseconds(200) };
        anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(anim, CoverArtHoverOverlay);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);
        sb.Begin();
    }

    private void BypassBtn_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (BypassBtnGlow is null) return;
        var sb = new Storyboard();
        var anim = new DoubleAnimation { To = 1.0, Duration = TimeSpan.FromMilliseconds(160) };
        anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(anim, BypassBtnGlow);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);
        sb.Begin();
    }

    private void BypassBtn_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (BypassBtnGlow is null) return;
        var sb = new Storyboard();
        var anim = new DoubleAnimation { To = 0.0, Duration = TimeSpan.FromMilliseconds(200) };
        anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(anim, BypassBtnGlow);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);
        sb.Begin();
    }

    private async void StartBypassBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ShowSteamSection)
        {
            return;
        }

        if (ViewModel.ShowAktivasiOfflineBadge)
        {
            OfflineActivationCheckbox.IsChecked = false;
            OfflineActivationDialog.IsPrimaryButtonEnabled = false;
            OfflineActivationDialog.XamlRoot = this.XamlRoot;
            await OfflineActivationDialog.ShowAsync();
        }
        else
        {
            await ViewModel.StartBypassGameCommand.ExecuteAsync(null);
        }
    }

    private void OfflineActivationCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        OfflineActivationDialog.IsPrimaryButtonEnabled = OfflineActivationCheckbox.IsChecked == true;
    }

    private void OfflineActivationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _ = ViewModel.StartBypassGameCommand.ExecuteAsync(null);
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var origContent = btn.Content;
            var origBg = btn.Background;
            var origFg = btn.Foreground;

            btn.Content = "Copied!";
            btn.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            btn.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

            await Task.Delay(2000);

            btn.Content = origContent;
            btn.Background = origBg;
            btn.Foreground = origFg;
        }
    }

    private async void TutorialThumbnail_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (!ViewModel.HasTutorialVideo || TutorialOverlayWebView is null || TutorialVideoOverlayRoot is null)
            return;

        try
        {
            if (!_tutorialWebViewInitialized)
            {
                await TutorialOverlayWebView.EnsureCoreWebView2Async();
                ConfigureTutorialWebView(TutorialOverlayWebView.CoreWebView2);
                _tutorialWebViewInitialized = true;
            }

            TutorialVideoOverlayRoot.Visibility = Visibility.Visible;
            TutorialVideoOverlayRoot.IsHitTestVisible = true;
            var videoId = ExtractYouTubeVideoId(ViewModel.TutorialVideoEmbedUrl);
            if (string.IsNullOrWhiteSpace(videoId))
                return;

            var playerUrl = $"https://appassets.example/youtube-player.html?videoId={Uri.EscapeDataString(videoId)}&autoplay=1";
            TutorialOverlayWebView.Source = new Uri(playerUrl);
        }
        catch
        {
            // Keep UX safe: if embed fails, user still can use watch URL from metadata.
        }
    }

    private void TutorialVideoOverlayCloseBtn_Click(object sender, RoutedEventArgs e)
    {
        StopTutorialVideoPlayback();
    }

    private void ConfigureTutorialWebView(CoreWebView2 core)
    {
        if (!_tutorialVirtualHostMapped)
        {
            var webFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Web");
            core.SetVirtualHostNameToFolderMapping(
                "appassets.example",
                webFolder,
                CoreWebView2HostResourceAccessKind.DenyCors);
            _tutorialVirtualHostMapped = true;
        }

        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreHostObjectsAllowed = false;
        core.WebMessageReceived -= TutorialWebView_WebMessageReceived;
        core.WebMessageReceived += TutorialWebView_WebMessageReceived;
    }

    private async void TutorialWebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var msg = e.TryGetWebMessageAsString();
            if (string.IsNullOrWhiteSpace(msg) || !msg.StartsWith("YT_ERROR_", StringComparison.Ordinal))
                return;

            var watch = ViewModel.TutorialVideoWatchUrl;
            if (!string.IsNullOrWhiteSpace(watch) && Uri.TryCreate(watch, UriKind.Absolute, out var uri))
            {
                await Launcher.LaunchUriAsync(uri);
            }
        }
        catch
        {
            // Swallow fallback failures; user still can close overlay.
        }
    }

    private static string? ExtractYouTubeVideoId(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
            return null;

        if (uri.AbsolutePath.Contains("/embed/", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var idx = Array.FindIndex(parts, p => p.Equals("embed", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && idx + 1 < parts.Length)
                return parts[idx + 1];
        }

        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var q in query)
        {
            var kv = q.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "v")
                return Uri.UnescapeDataString(kv[1]);
        }

        return null;
    }

    private void StopTutorialVideoPlayback()
    {
        if (TutorialVideoOverlayRoot is not null)
        {
            TutorialVideoOverlayRoot.Visibility = Visibility.Collapsed;
            TutorialVideoOverlayRoot.IsHitTestVisible = false;
        }

        if (TutorialOverlayWebView?.CoreWebView2 is not null)
        {
            TutorialOverlayWebView.CoreWebView2.Navigate("about:blank");
        }
    }

    public static Visibility BoolToVis(bool v) =>
        v ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InverseBoolToVis(bool v) =>
        v ? Visibility.Collapsed : Visibility.Visible;

    public static bool InverseBool(bool v) => !v;

    private async Task<bool> ShowConfirmAsync(string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Peringatan Antivirus",
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.WrapWholeWords
            },
            PrimaryButtonText = "Tetap Lanjut",
            CloseButtonText = "Batal",
            DefaultButton = ContentDialogButton.Close
        };
        ApplyBypassDialogButtonTheme(dialog);

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private async Task<string?> SelectManualFolderAsync(string message)
    {
        var info = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Pilih Folder Game",
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.WrapWholeWords
            },
            PrimaryButtonText = "Lanjut Pilih Folder",
            CloseButtonText = "Batal",
            DefaultButton = ContentDialogButton.Primary
        };
        ApplyBypassDialogButtonTheme(info);

        var ack = await info.ShowAsync();
        if (ack != ContentDialogResult.Primary)
            throw new InvalidOperationException("Anda belum memilih folder game.");

        // Prioritas UI modern seperti GameHub screenshot: gunakan WinRT FolderPicker dulu.
        // Jika gagal di environment tertentu, fallback ke dialog legacy agar flow tetap jalan.
        try
        {
            var modernPath = await TryPickFolderWithWinRtAsync();
            if (!string.IsNullOrWhiteSpace(modernPath))
                return modernPath;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var fallbackPath = TryPickFolderWithWinForms();
            if (!string.IsNullOrWhiteSpace(fallbackPath))
                return fallbackPath;

            var detail = string.IsNullOrWhiteSpace(ex.Message)
                ? "Terjadi kendala sistem saat membuka dialog pemilih folder."
                : ex.Message;
            throw new InvalidOperationException($"Gagal membuka pemilih folder: {detail}");
        }

        // WinRT tidak melempar error tapi tidak ada path, coba fallback legacy.
        var legacyPath = TryPickFolderWithWinForms();
        if (!string.IsNullOrWhiteSpace(legacyPath))
            return legacyPath;

        throw new InvalidOperationException("Anda belum memilih folder game.");
    }

    private static string? TryPickFolderWithWinForms()
    {
        try
        {
            const string psScript =
                "Add-Type -AssemblyName System.Windows.Forms; " +
                "$dlg = New-Object System.Windows.Forms.OpenFileDialog; " +
                "$dlg.Title = 'Select Folder'; " +
                "$dlg.Filter = 'Folders|*.folder'; " +
                "$dlg.CheckFileExists = $false; " +
                "$dlg.ValidateNames = $false; " +
                "$dlg.FileName = 'Pilih folder instalasi game'; " +
                "if ($dlg.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { " +
                "  $path = Split-Path -Path $dlg.FileName -Parent; " +
                "  if ([string]::IsNullOrWhiteSpace($path)) { $path = $dlg.FileName }; " +
                "  Write-Output $path " +
                "}";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -STA -Command \"{psScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null)
                return null;

            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return string.IsNullOrWhiteSpace(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> TryPickFolderWithWinRtAsync()
    {
        var app = (App)App.Current;
        if (app.MainWindowInstance is null)
            throw new InvalidOperationException("Window aplikasi tidak tersedia untuk membuka pemilih folder.");

        var folderPicker = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        folderPicker.FileTypeFilter.Add("*");

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task ShowInfoDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.WrapWholeWords
            },
            PrimaryButtonText = "Tutup",
            DefaultButton = ContentDialogButton.Primary
        };
        ApplyBypassDialogButtonTheme(dialog);

        await dialog.ShowAsync();
    }

    private static void ApplyBypassDialogButtonTheme(ContentDialog dialog)
    {
        dialog.Resources["SystemControlBackgroundAccentBrush"] = new SolidColorBrush(Microsoft.UI.Colors.White);
        dialog.Resources["SystemControlForegroundChromeWhiteBrush"] = new SolidColorBrush(Microsoft.UI.Colors.Black);
        dialog.Resources["SystemControlHighlightAccentBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(38, 255, 255, 255));

        // Fallback keys across WinUI versions.
        dialog.Resources["AccentButtonBackground"] = new SolidColorBrush(Microsoft.UI.Colors.White);
        dialog.Resources["AccentButtonForeground"] = new SolidColorBrush(Microsoft.UI.Colors.Black);
        dialog.Resources["AccentButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(38, 255, 255, 255));
        dialog.Resources["AccentButtonBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(56, 255, 255, 255));
        dialog.Resources["AccentButtonForegroundPointerOver"] = new SolidColorBrush(Microsoft.UI.Colors.White);
        dialog.Resources["AccentButtonForegroundPressed"] = new SolidColorBrush(Microsoft.UI.Colors.White);
    }

    public static Uri SafeUri(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Uri.TryCreate(raw, UriKind.Absolute, out var parsed))
            return parsed;

        return new Uri("ms-appx:///Assets/StoreLogo.png");
    }
}
