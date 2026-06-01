using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel { get; }
    private readonly INavigationService _nav;
    private int? _pendingRemoveGameAppId;
    private string _pendingRemoveGameTitle = string.Empty;
    public LibraryPage()
    {
        ViewModel = ((App)App.Current).GetRequiredService<LibraryViewModel>();
        _nav = ((App)App.Current).GetRequiredService<INavigationService>();
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
        ScrollLibraryToTop();
    }

    private void OnGameItemClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LibraryGameCard card)
        {
            _nav.Navigate<GameDetailPage>(card.AppId);
        }
    }

    private async void OnRemoveGameClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int appId && btn.DataContext is LibraryGameCard card)
        {
            _pendingRemoveGameAppId = appId;
            _pendingRemoveGameTitle = card.Title;
            LibraryRemoveConfirmMessageText.Text = $"Hapus \"{_pendingRemoveGameTitle}\" dari Steam?";
            LibraryRemoveConfirmOverlay.Visibility = Visibility.Visible;
        }
    }

    private void LibraryRemoveConfirmCancel_Click(object sender, RoutedEventArgs e)
    {
        _pendingRemoveGameAppId = null;
        _pendingRemoveGameTitle = string.Empty;
        LibraryRemoveConfirmOverlay.Visibility = Visibility.Collapsed;
    }

    private async void LibraryRemoveConfirmPrimary_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingRemoveGameAppId is null)
        {
            LibraryRemoveConfirmOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        var appId = _pendingRemoveGameAppId.Value;
        _pendingRemoveGameAppId = null;
        _pendingRemoveGameTitle = string.Empty;
        LibraryRemoveConfirmOverlay.Visibility = Visibility.Collapsed;

        var targetCard = ViewModel.Games.FirstOrDefault(g => g.AppId == appId);
        RemoveGameResult removeResult = await ViewModel.RemoveGameWithResultAsync(appId.ToString(), reloadOnSuccess: false);
        if (removeResult.Success)
        {
            if (targetCard is not null)
            {
                await AnimateRemoveCardAsync(targetCard);
            }
            await ViewModel.LoadAsync();
            ScrollLibraryToTop();
            return;
        }

        LibraryRemoveResultTitleText.Text = removeResult.BlockedByInstalledGame
            ? "Game Masih Terinstall"
            : "Remove Game Gagal";
        LibraryRemoveResultMessageText.Text = removeResult.Error ?? "Gagal menghapus game dari Steam.";
        LibraryRemoveResultOverlay.Visibility = Visibility.Visible;
    }

    private void LibraryRemoveResultClose_Click(object sender, RoutedEventArgs e)
    {
        LibraryRemoveResultOverlay.Visibility = Visibility.Collapsed;
    }

    private void ListItem_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A));
            // White lighting effect
            border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
        }
    }

    private void ListItem_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x0F, 0x0F, 0x0F));
            border.BorderBrush = (Brush)Application.Current.Resources["NexaCardBorderBrush"];
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

    private void SearchLaser_Loaded(object sender, RoutedEventArgs e)
    {
        var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

        // Expand Border Left
        var leftAnim = new DoubleAnimation { From = 0.5, To = 0.0, Duration = new Duration(TimeSpan.FromSeconds(2.0)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(leftAnim, BorderGlowLeft);
        Storyboard.SetTargetProperty(leftAnim, "Offset");

        // Expand Border Right
        var rightAnim = new DoubleAnimation { From = 0.5, To = 1.0, Duration = new Duration(TimeSpan.FromSeconds(2.0)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(rightAnim, BorderGlowRight);
        Storyboard.SetTargetProperty(rightAnim, "Offset");

        // Fade Color Left
        var colorLeftAnim = new ColorAnimation { From = Microsoft.UI.Colors.White, To = Windows.UI.Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF), Duration = new Duration(TimeSpan.FromSeconds(2.0)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        Storyboard.SetTarget(colorLeftAnim, BorderGlowLeft);
        Storyboard.SetTargetProperty(colorLeftAnim, "Color");

        // Fade Color Right
        var colorRightAnim = new ColorAnimation { From = Microsoft.UI.Colors.White, To = Windows.UI.Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF), Duration = new Duration(TimeSpan.FromSeconds(2.0)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        Storyboard.SetTarget(colorRightAnim, BorderGlowRight);
        Storyboard.SetTargetProperty(colorRightAnim, "Color");

        // Expand Bg Left
        var bgLeftAnim = new DoubleAnimation { From = 0.5, To = 0.0, Duration = new Duration(TimeSpan.FromSeconds(2.0)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(bgLeftAnim, BgGlowLeft);
        Storyboard.SetTargetProperty(bgLeftAnim, "Offset");

        // Expand Bg Right
        var bgRightAnim = new DoubleAnimation { From = 0.5, To = 1.0, Duration = new Duration(TimeSpan.FromSeconds(2.0)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(bgRightAnim, BgGlowRight);
        Storyboard.SetTargetProperty(bgRightAnim, "Offset");

        // Fade Bg Color Left
        var bgColorLeftAnim = new ColorAnimation { From = Windows.UI.Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A), To = Windows.UI.Color.FromArgb(0xFF, 0x0D, 0x0D, 0x0D), Duration = new Duration(TimeSpan.FromSeconds(2.0)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        Storyboard.SetTarget(bgColorLeftAnim, BgGlowLeft);
        Storyboard.SetTargetProperty(bgColorLeftAnim, "Color");

        // Fade Bg Color Right
        var bgColorRightAnim = new ColorAnimation { From = Windows.UI.Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A), To = Windows.UI.Color.FromArgb(0xFF, 0x0D, 0x0D, 0x0D), Duration = new Duration(TimeSpan.FromSeconds(2.0)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        Storyboard.SetTarget(bgColorRightAnim, BgGlowRight);
        Storyboard.SetTargetProperty(bgColorRightAnim, "Color");

        storyboard.Children.Add(leftAnim);
        storyboard.Children.Add(rightAnim);
        storyboard.Children.Add(colorLeftAnim);
        storyboard.Children.Add(colorRightAnim);
        storyboard.Children.Add(bgLeftAnim);
        storyboard.Children.Add(bgRightAnim);
        storyboard.Children.Add(bgColorLeftAnim);
        storyboard.Children.Add(bgColorRightAnim);

        storyboard.Begin();
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
        if (e.PropertyName == nameof(LibraryViewModel.CurrentPageLabel))
        {
            AnimatePageTransition();
        }
    }

    private void AnimatePageTransition()
    {
        if (LibraryList is null)
            return;

        if (LibraryList.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            LibraryList.RenderTransform = transform;
        }

        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 0.6,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(240)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fade, LibraryList);
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

    private async Task AnimateRemoveCardAsync(LibraryGameCard card)
    {
        if (LibraryList.ContainerFromItem(card) is not ListViewItem item)
            return;

        if (item.ContentTemplateRoot is not FrameworkElement cardRoot)
            return;

        if (cardRoot.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            cardRoot.RenderTransform = transform;
        }

        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(220)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fade, cardRoot);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        var slide = new DoubleAnimation
        {
            From = 0,
            To = 54,
            Duration = new Duration(TimeSpan.FromMilliseconds(220)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(slide, transform);
        Storyboard.SetTargetProperty(slide, "X");
        storyboard.Children.Add(slide);

        var tcs = new TaskCompletionSource<bool>();
        void OnCompleted(object? s, object e)
        {
            storyboard.Completed -= OnCompleted;
            tcs.TrySetResult(true);
        }

        storyboard.Completed += OnCompleted;
        storyboard.Begin();
        await tcs.Task;

        // Reset visual state to avoid recycled container inheriting old animation state.
        cardRoot.Opacity = 1;
        transform.X = 0;
        item.Opacity = 1;
        if (item.RenderTransform is TranslateTransform itemTransform)
        {
            itemTransform.X = 0;
            itemTransform.Y = 0;
        }
    }

    private void ScrollLibraryToTop()
    {
        var sv = FindDescendant<ScrollViewer>(LibraryList);
        sv?.ChangeView(null, 0, null, true);
    }

    private static T? FindDescendant<T>(DependencyObject? root) where T : DependencyObject
    {
        if (root is null) return null;
        if (root is T typed) return typed;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var result = FindDescendant<T>(child);
            if (result is not null) return result;
        }
        return null;
    }
}
