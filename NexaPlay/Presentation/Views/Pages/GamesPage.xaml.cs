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
        await ViewModel.LoadAsync();
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
            >= 1380 => 6,
            >= 1080 => 5,
            >= 800 => 4,
            _ => 3
        };
        columns = Math.Clamp(columns, minColumns, maxColumns);

        var slotWidth = (usableWidth / columns) - 0.2;
        var minSlotWidth = minCardWidth + interItemGap;
        var maxSlotWidth = maxCardWidth + interItemGap;
        slotWidth = Math.Clamp(slotWidth, minSlotWidth, maxSlotWidth);

        var cardWidth = slotWidth - interItemGap;
        cardWidth = Math.Clamp(cardWidth, minCardWidth, maxCardWidth);
        var cardHeight = Math.Round(cardWidth * 1.5, 2);

        if (GamesGrid.ItemsPanelRoot is not ItemsWrapGrid wrapGrid)
            return false;

        wrapGrid.ItemWidth = slotWidth;
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

    private void OnGenreChecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is string genre)
        {
            ViewModel.ToggleGenreCommand.Execute(genre);
        }
    }

    private void OnGenreUnchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is string genre)
        {
            ViewModel.ToggleGenreCommand.Execute(genre);
        }
    }

    // ── Static converters for x:Bind ────────────────────────────────────────

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
}
