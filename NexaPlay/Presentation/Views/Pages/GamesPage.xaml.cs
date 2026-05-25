using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Presentation.ViewModels;
using System.ComponentModel;
using System.Collections.Generic;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class GamesPage : Page
{
    public GamesViewModel ViewModel { get; }
    private readonly INavigationService _nav;
    private int _lastColumns = -1;
    private DispatcherTimer? _gridResizeDebounceTimer;
    private bool _suppressPageTransitionAnimation;

    public GamesPage()
    {
        ViewModel = ((App)App.Current).GetRequiredService<GamesViewModel>();
        _nav      = ((App)App.Current).GetRequiredService<INavigationService>();
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        SetupGridResizeDebounce();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.NavigationMode != NavigationMode.Back && ViewModel.Games.Count == 0)
        {
            await ViewModel.LoadAsync();
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
            _suppressPageTransitionAnimation = true;
            ViewModel.UpdateGridColumns(_lastColumns);
            _suppressPageTransitionAnimation = false;
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

        // Breakpoint agresif seperti GameHub xl/lg/md/sm:
        //   >= 1100 => 6 kolom (sebelumnya >= 1380)
        //   >= 880  => 5 kolom (sebelumnya >= 1080)
        //   >= 680  => 4 kolom (sebelumnya >= 800)
        //   _       => 3 kolom
        // Layar 1366px fullscreen: usable ≈ 1258 >= 1100 → 6 kolom ✓
        // Layar 1920px fullscreen: usable ≈ 1812 >= 1100 → 6 kolom ✓
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

        // Slot width fluid (mirip CSS grid %) — hanya clamp dari atas agar tidak overflow.
        // Tidak di-clamp dari bawah supaya card tidak force lebar dan merusak layout.
        var slotWidth = (availableWidth / columns) - 0.2;
        slotWidth = Math.Min(slotWidth, maxCardWidth + interItemGap);
        slotWidth = Math.Max(slotWidth, minCardWidth + interItemGap); // safety floor

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

    private void OnGameItemClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NexaPlay.Core.Models.FixEntry fix)
            _nav.Navigate<GameDetailPage>(fix.AppId);
    }

    private void OnGameCardClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int appId)
        {
            _nav.Navigate<GameDetailPage>(appId);
        }
    }

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

    private void DenuvoBadge_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border)
        {
            var storyboard = new Storyboard
            {
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            };
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.3,
                Duration = new Duration(TimeSpan.FromMilliseconds(700))
            };
            Storyboard.SetTarget(animation, border);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }
    }

    private void OnGenreChecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is string genre)
        {
            ViewModel.SetGenreFilter(genre, true);
        }
    }

    private void OnGenreUnchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is string genre)
        {
            ViewModel.SetGenreFilter(genre, false);
        }
    }

    // ── Static converters for x:Bind ────────────────────────────────────────

    public static bool IsGenreChecked(IReadOnlyList<string> selectedGenres, string genre)
    {
        return selectedGenres is not null && selectedGenres.Contains(genre, StringComparer.OrdinalIgnoreCase);
    }

    public static Microsoft.UI.Xaml.Visibility BoolToVis(bool v) =>
        v ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public static Microsoft.UI.Xaml.Visibility InverseBoolToVis(bool v) =>
        v ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
    public static Brush PageButtonBackground(bool isSelected) =>
        isSelected ? new SolidColorBrush(Microsoft.UI.Colors.White) : new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x12, 0x12, 0x12));
    public static Brush PageButtonForeground(bool isSelected) =>
        isSelected ? new SolidColorBrush(Microsoft.UI.Colors.Black) : new SolidColorBrush(Microsoft.UI.Colors.White);

    public static ImageSource? ToImageSource(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        try { return new BitmapImage(uri); } catch { return null; }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GamesViewModel.IsFilterOpen))
        {
            AnimateFilterOverlay(ViewModel.IsFilterOpen);
            return;
        }

        if (e.PropertyName == nameof(GamesViewModel.CurrentPageLabel))
        {
            if (_suppressPageTransitionAnimation)
                return;
            AnimatePageTransition();
        }

        if (e.PropertyName == nameof(GamesViewModel.SelectedGenres) && ViewModel.SelectedGenres.Count == 0)
        {
            var checkboxes = FindDescendants<CheckBox>(GenreItemsControl);
            foreach (var cb in checkboxes)
            {
                cb.IsChecked = false;
            }
        }
    }

    private void AnimateFilterOverlay(bool isOpen)
    {
        if (FilterOverlayTranslate is null || FilterOverlay is null) return;

        var storyboard = new Storyboard();

        var opacity = new DoubleAnimation
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(170)),
            To = isOpen ? 1 : 0,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(opacity, FilterOverlay);
        Storyboard.SetTargetProperty(opacity, "Opacity");
        storyboard.Children.Add(opacity);

        var slide = new DoubleAnimation
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(170)),
            To = isOpen ? 0 : -8,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slide, FilterOverlayTranslate);
        Storyboard.SetTargetProperty(slide, "Y");
        storyboard.Children.Add(slide);

        storyboard.Begin();
    }

    private void AnimatePageTransition()
    {
        if (GamesGrid is null)
        {
            return;
        }

        if (GamesGrid.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            GamesGrid.RenderTransform = transform;
        }

        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 0.6,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(240)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fade, GamesGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        var slide = new DoubleAnimation
        {
            From = 6,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(240)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slide, transform);
        Storyboard.SetTargetProperty(slide, "Y");
        storyboard.Children.Add(slide);

        storyboard.Begin();
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

    private static IEnumerable<T> FindDescendants<T>(DependencyObject? root) where T : DependencyObject
    {
        if (root is null)
            yield break;

        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T match && current != root)
                yield return match;

            var childCount = VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < childCount; i++)
            {
                queue.Enqueue(VisualTreeHelper.GetChild(current, i));
            }
        }
    }
}
