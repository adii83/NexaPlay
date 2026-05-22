using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Presentation.ViewModels;
using NexaPlay.Presentation.Views.Pages;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel => (HomeViewModel)DataContext;
    private DispatcherTimer? _carouselTimer;
    private DispatcherTimer? _popularResizeDebounceTimer;
    private INavigationService? _nav;
    private int _lastPopularColumns = -1;

    public HomePage() 
    {
        InitializeComponent();
        DataContext = ((App)App.Current).GetRequiredService<HomeViewModel>();
        _nav = ((App)App.Current).GetRequiredService<INavigationService>();
        SetupCarouselTimer();
        SetupPopularResizeDebounceTimer();
    }

    private void SetupCarouselTimer()
    {
        _carouselTimer = new DispatcherTimer();
        _carouselTimer.Interval = TimeSpan.FromSeconds(5); // Auto-scroll every 5 seconds
        _carouselTimer.Tick += CarouselTimer_Tick;
        this.Loaded += (s, e) => _carouselTimer.Start();
        this.Unloaded += (s, e) => _carouselTimer.Stop();
    }

    private void SetupPopularResizeDebounceTimer()
    {
        _popularResizeDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(160)
        };
        _popularResizeDebounceTimer.Tick += async (s, e) =>
        {
            _popularResizeDebounceTimer?.Stop();
            if (_lastPopularColumns > 0)
            {
                await ViewModel.UpdatePopularLayoutAsync(_lastPopularColumns);
            }
        };
    }

    private void CarouselTimer_Tick(object? sender, object e)
    {
        if (HeroCarousel != null && HeroCarousel.Items.Count > 0)
        {
            int nextIndex = HeroCarousel.SelectedIndex + 1;
            if (nextIndex >= HeroCarousel.Items.Count)
            {
                nextIndex = 0;
            }
            HeroCarousel.SelectedIndex = nextIndex;
        }
    }

    private void HeroCarousel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCustomPips();
    }

    private bool _isManualNavigating = false;

    private void UpdateCustomPips()
    {
        if (ViewModel?.RecentFixes == null || CustomPipsContainer == null) return;
        
        CustomPipsContainer.Children.Clear();
        int count = ViewModel.RecentFixes.Count;
        int selectedIndex = HeroCarousel.SelectedIndex;

        for (int i = 0; i < count; i++)
        {
            int targetIndex = i; // capture index for closure
            bool isActive = (i == selectedIndex);
            
            var pip = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Height = 6,
                RadiusX = 3,
                RadiusY = 3,
                Width = isActive ? 24 : 6,
                Fill = isActive 
                    ? (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["NexaTextPrimaryBrush"] 
                    : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(128, 255, 255, 255)),
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
            };

            // Hit area hanya diperbesar secara vertikal (Padding Y) agar jarak horizontal antar bulatan tetap rapat (Spacing=6)
            var hitArea = new Microsoft.UI.Xaml.Controls.Grid
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Padding = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 8) 
            };
            
            hitArea.Children.Add(pip);
            
            // Add click/tap handler
            hitArea.Tapped += (s, e) => 
            {
                if (_isManualNavigating) return;
                
                if (HeroCarousel != null && HeroCarousel.SelectedIndex != targetIndex)
                {
                    _isManualNavigating = true;
                    if (_carouselTimer != null) _carouselTimer.Stop();

                    // Langsung loncat ke target tanpa simulasi karena FlipView WinUI tidak mendukung animasi lompat ganda secara native
                    HeroCarousel.SelectedIndex = targetIndex;

                    if (_carouselTimer != null) _carouselTimer.Start();
                    _isManualNavigating = false;
                }
            };

            CustomPipsContainer.Children.Add(hitArea);
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
        if (ApplyPopularGridLayout() && _lastPopularColumns > 0)
        {
            await ViewModel.UpdatePopularLayoutAsync(_lastPopularColumns);
        }
        UpdateCustomPips();
    }

    private void PopularGamesGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var changed = ApplyPopularGridLayout(e.NewSize.Width);
        if (!changed)
            return;

        _popularResizeDebounceTimer?.Stop();
        _popularResizeDebounceTimer?.Start();
    }

    private bool ApplyPopularGridLayout(double? newWidth = null)
    {
        if (PopularGamesGrid is null)
            return false;

        var availableWidth = newWidth ?? PopularGamesGrid.ActualWidth;
        if (availableWidth <= 0)
            return false;

        const double minCardWidth = 200;
        const double maxCardWidth = 320;
        const double itemMargin = 8;
        const double interItemGap = itemMargin * 2;
        const double outerPadding = 0;
        const int minColumns = 3;
        const int maxColumns = 6;

        var usableWidth = Math.Max(0, availableWidth - (outerPadding * 2));
        var columns = usableWidth switch
        {
            >= 1380 => 6, // fullscreen (e.g. 1920x1080)
            >= 1080 => 5, // default windowed / 1366x768 fullscreen
            >= 800 => 4,
            _ => 3
        };
        columns = Math.Clamp(columns, minColumns, maxColumns);

        // Gunakan pembagian fraksional sedikit dikurangi (-0.2) agar ukuran slot presisi dan mulus saat drag resize
        // (menghilangkan efek jitter/staircase Math.Floor) namun tetap muat dan tidak tumpah ke baris berikutnya
        var slotWidth = (usableWidth / columns) - 0.2;
        var minSlotWidth = minCardWidth + interItemGap;
        var maxSlotWidth = maxCardWidth + interItemGap;
        slotWidth = Math.Clamp(slotWidth, minSlotWidth, maxSlotWidth);

        var cardWidth = slotWidth - interItemGap;
        cardWidth = Math.Clamp(cardWidth, minCardWidth, maxCardWidth);
        var cardHeight = Math.Round(cardWidth * 1.5, 2);

        if (PopularGamesGrid.ItemsPanelRoot is not ItemsWrapGrid wrapGrid)
            return false;

        wrapGrid.ItemWidth = slotWidth;
        wrapGrid.ItemHeight = cardHeight + interItemGap;
        wrapGrid.MaximumRowsOrColumns = columns;

        var isColumnChanged = _lastPopularColumns != columns;
        _lastPopularColumns = columns;
        return isColumnChanged;
    }

    // Converters for x:Bind
    public static Microsoft.UI.Xaml.Visibility InverseBoolToVis(bool val) => val ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
    public static Microsoft.UI.Xaml.Visibility BoolToVis(bool val) => val ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public static bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);
    public static ImageSource SafeImageSource(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) &&
            Uri.TryCreate(raw, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == "ms-appx"))
        {
            return new BitmapImage(parsed);
        }

        return new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));
    }

    private void DenuvoBadge_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border)
        {
            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard
            {
                RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever,
                AutoReverse = true
            };
            var animation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.3,
                Duration = new Duration(TimeSpan.FromMilliseconds(700))
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, border);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    private void OnPopularGameCardClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int appId && _nav is not null)
            _nav.Navigate<GameDetailPage>(appId);
    }

    private void PopularGameCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement card)
            return;

        var layer = FindDescendantByName<Border>(card, "HoverTitleLayer");
        if (layer is null)
            return;

        AnimateTitleLayer(layer, fadeIn: true);
    }

    private void PopularGameCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement card)
            return;

        var layer = FindDescendantByName<Border>(card, "HoverTitleLayer");
        if (layer is null)
            return;

        AnimateTitleLayer(layer, fadeIn: false);
    }

    private static void AnimateTitleLayer(Border layer, bool fadeIn)
    {
        var storyboard = new Storyboard();

        var opacityAnim = new DoubleAnimation
        {
            To = fadeIn ? 1 : 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(fadeIn ? 170 : 140)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(opacityAnim, layer);
        Storyboard.SetTargetProperty(opacityAnim, "Opacity");
        storyboard.Children.Add(opacityAnim);

        if (layer.RenderTransform is TranslateTransform transform)
        {
            var offsetAnim = new DoubleAnimation
            {
                To = fadeIn ? 0 : 14,
                Duration = new Duration(TimeSpan.FromMilliseconds(fadeIn ? 170 : 140)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(offsetAnim, transform);
            Storyboard.SetTargetProperty(offsetAnim, "Y");
            storyboard.Children.Add(offsetAnim);
        }

        storyboard.Begin();
    }

    private static T? FindDescendantByName<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var childCount = VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(current, i);
                if (child is T match && string.Equals(match.Name, name, StringComparison.Ordinal))
                {
                    return match;
                }

                queue.Enqueue(child);
            }
        }

        return null;
    }
}
