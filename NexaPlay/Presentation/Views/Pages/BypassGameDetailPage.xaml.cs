using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Presentation.ViewModels;
using System;
using System.ComponentModel;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class BypassGameDetailPage : Page
{
    public BypassGameDetailViewModel ViewModel { get; }
    private readonly INavigationService _nav;
    private Storyboard? _shimmerStoryboard;
    private Storyboard? _heroShimmerStoryboard;
    private Storyboard? _topIconShimmerStoryboard;

    public BypassGameDetailPage()
    {
        ViewModel = ((App)App.Current).GetRequiredService<BypassGameDetailViewModel>();
        _nav = ((App)App.Current).GetRequiredService<INavigationService>();
        InitializeComponent();
        DataContext = ViewModel;
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
        if (ViewModel.ShowAktivasiOfflineBadge)
        {
            OfflineActivationCheckbox.IsChecked = false;
            OfflineActivationDialog.IsPrimaryButtonEnabled = false;
            OfflineActivationDialog.XamlRoot = this.XamlRoot;
            await OfflineActivationDialog.ShowAsync();
        }
        else
        {
            ViewModel.StartBypassGameCommand.Execute(null);
        }
    }

    private void OfflineActivationCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        OfflineActivationDialog.IsPrimaryButtonEnabled = OfflineActivationCheckbox.IsChecked == true;
    }

    private void OfflineActivationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // For now, the Lanjut Bypass button just acts like a close button
    }

    public static Visibility BoolToVis(bool v) =>
        v ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InverseBoolToVis(bool v) =>
        v ? Visibility.Collapsed : Visibility.Visible;

    public static Uri SafeUri(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Uri.TryCreate(raw, UriKind.Absolute, out var parsed))
            return parsed;

        return new Uri("ms-appx:///Assets/StoreLogo.png");
    }
}
