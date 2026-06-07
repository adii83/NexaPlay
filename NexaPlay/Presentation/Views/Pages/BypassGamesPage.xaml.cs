using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using NexaPlay.Contracts.Services;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Core.Enums;
using NexaPlay.Presentation.Helpers;
using NexaPlay.Presentation.ViewModels;
using System.ComponentModel;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class BypassGamesPage : Page
{
    public BypassGamesViewModel ViewModel { get; }
    private readonly INavigationService _nav;
    private readonly ILicenseService _licenseService;
    private int _lastColumns = -1;
    private DispatcherTimer? _gridResizeDebounceTimer;

    public BypassGamesPage()
    {
        ViewModel = ((App)App.Current).GetRequiredService<BypassGamesViewModel>();
        _nav      = ((App)App.Current).GetRequiredService<INavigationService>();
        _licenseService = ((App)App.Current).GetRequiredService<ILicenseService>();
        InitializeComponent();
        DataContext = ViewModel;
        SetupGridResizeDebounce();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.NavigationMode != NavigationMode.Back && ViewModel.DisplayGames.Count == 0)
        {
            await ViewModel.LoadAsync();
        }
        
        if (e.Parameter is string categoryFromSidebar && !string.IsNullOrWhiteSpace(categoryFromSidebar))
        {
            ViewModel.SetCategory(categoryFromSidebar);
        }

        if (ApplyGamesGridLayout() && _lastColumns > 0)
        {
            ViewModel.UpdateGridColumns(_lastColumns);
        }
    }

    private void GamesGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var changed = ApplyGamesGridLayout(e.NewSize.Width);
        if (!changed)
        {
            return;
        }

        _gridResizeDebounceTimer?.Stop();
        _gridResizeDebounceTimer?.Start();
    }

    private void SetupGridResizeDebounce()
    {
        _gridResizeDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };
        _gridResizeDebounceTimer.Tick += (s, e) =>
        {
            _gridResizeDebounceTimer?.Stop();
            if (GamesGrid is null || _lastColumns <= 0)
                return;

            var scrollViewer = FindDescendant<ScrollViewer>(GamesGrid);
            if (scrollViewer is null)
                return;

            var previousVerticalOffset = scrollViewer.VerticalOffset;
            ViewModel.UpdateGridColumns(_lastColumns);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                scrollViewer.ChangeView(null, previousVerticalOffset, null, true);
            });
        };
    }

    private bool ApplyGamesGridLayout(double? newWidth = null)
    {
        if (GamesGrid is null)
            return false;

        var availableWidth = newWidth ?? GamesGrid.ActualWidth;
        if (availableWidth <= 0)
            return false;

        const double minCardWidth = 150;
        const double maxCardWidth = 280;
        const double itemMargin = 8;
        const double interItemGap = itemMargin * 2; // 16px
        const int minColumns = 3;
        const int maxColumns = 6;

        var columns = availableWidth switch
        {
            >= 1100 => 6,
            >= 880  => 5,
            >= 680  => 4,
            _       => 3
        };
        columns = Math.Clamp(columns, minColumns, maxColumns);

        var slotWidth = (availableWidth / columns) - 0.2;
        slotWidth = Math.Min(slotWidth, maxCardWidth + interItemGap);
        slotWidth = Math.Max(slotWidth, minCardWidth + interItemGap);

        var cardWidth = Math.Clamp(slotWidth - interItemGap, minCardWidth, maxCardWidth);
        var cardHeight = Math.Round(cardWidth * 1.5, 2);

        if (GamesGrid.ItemsPanelRoot is not ItemsWrapGrid wrapGrid)
            return false;

        wrapGrid.ItemWidth  = slotWidth;
        wrapGrid.ItemHeight = cardHeight + interItemGap;
        wrapGrid.MaximumRowsOrColumns = columns;

        var changed = _lastColumns != columns;
        _lastColumns = columns;
        return changed;
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    private async void OnGameCardClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int appId)
        {
            var selectedEntry = ViewModel.DisplayGames.FirstOrDefault(x => x.AppId == appId);
            if (selectedEntry is not null &&
                string.Equals(ViewModel.ActiveCategory, "steam-sharing", StringComparison.OrdinalIgnoreCase) &&
                selectedEntry.IsPremium)
            {
                try
                {
                    var license = await _licenseService.LoadAsync();
                    if (!license.IsValid)
                    {
                        await LicenseAccessDialogHelper.ShowLicenseInvalidAsync(XamlRoot);
                        return;
                    }

                    if (!license.IsPremium)
                    {
                        await LicenseAccessDialogHelper.ShowPremiumFeatureAsync(XamlRoot);
                        return;
                    }
                }
                catch
                {
                    await LicenseAccessDialogHelper.ShowVerificationFailedAsync(XamlRoot);
                    return;
                }
            }

            _nav.Navigate<BypassGameDetailPage>((appId, selectedEntry));
        }
    }

    // ── Hover Effects ────────────────────────────────────────────────────────

    private void GameCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is FrameworkElement root)
        {
            if (root.FindName("HoverTitleLayer") is UIElement titleLayer)
            {
                var fade = new DoubleAnimation
                {
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var storyboard = new Storyboard();
                Storyboard.SetTarget(fade, titleLayer);
                Storyboard.SetTargetProperty(fade, "Opacity");

                if (titleLayer.RenderTransform is TranslateTransform trans)
                {
                    var slide = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(250),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(slide, trans);
                    Storyboard.SetTargetProperty(slide, "Y");
                    storyboard.Children.Add(slide);
                }

                storyboard.Children.Add(fade);
                storyboard.Begin();
            }
        }
    }

    private void GameCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is FrameworkElement root)
        {
            if (root.FindName("HoverTitleLayer") is UIElement titleLayer)
            {
                var fade = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                var storyboard = new Storyboard();
                Storyboard.SetTarget(fade, titleLayer);
                Storyboard.SetTargetProperty(fade, "Opacity");

                if (titleLayer.RenderTransform is TranslateTransform trans)
                {
                    var slide = new DoubleAnimation
                    {
                        To = 14,
                        Duration = TimeSpan.FromMilliseconds(250),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(slide, trans);
                    Storyboard.SetTargetProperty(slide, "Y");
                    storyboard.Children.Add(slide);
                }

                storyboard.Children.Add(fade);
                storyboard.Begin();
            }
        }
    }

    // ── Static converters for x:Bind ────────────────────────────────────────

    public static Microsoft.UI.Xaml.Visibility BoolToVis(bool v) =>
        v ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public static Microsoft.UI.Xaml.Visibility InverseBoolToVis(bool v) =>
        v ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;

    public static Microsoft.UI.Xaml.Visibility IsSteamMode(string? category) =>
        string.Equals(category, "steam-sharing", StringComparison.OrdinalIgnoreCase)
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

    public static Microsoft.UI.Xaml.Visibility IsThirdPartyMode(string? category) =>
        string.Equals(category, "steam-sharing", StringComparison.OrdinalIgnoreCase)
            ? Microsoft.UI.Xaml.Visibility.Collapsed
            : Microsoft.UI.Xaml.Visibility.Visible;

    public static ImageSource? ToImageSource(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != "ms-appx") return null;
        try { return new BitmapImage(uri); } catch { return null; }
    }

    // Category Buttons Styling
    public static Brush CategoryButtonBackground(string activeCategory, string currentCategory) =>
        activeCategory == currentCategory ? new SolidColorBrush(Microsoft.UI.Colors.White) : new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x12, 0x12, 0x12));

    public static Brush CategoryButtonForeground(string activeCategory, string currentCategory) =>
        activeCategory == currentCategory ? new SolidColorBrush(Microsoft.UI.Colors.Black) : new SolidColorBrush(Microsoft.UI.Colors.White);

    private void OnCategoryClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string categoryId)
        {
            ViewModel.SetCategory(categoryId);
        }
    }

    private static T? FindDescendant<T>(DependencyObject? root) where T : DependencyObject
    {
        if (root is null)
            return null;

        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T match)
                return match;

            var childCount = VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < childCount; i++)
            {
                queue.Enqueue(VisualTreeHelper.GetChild(current, i));
            }
        }
        return null;
    }
}
