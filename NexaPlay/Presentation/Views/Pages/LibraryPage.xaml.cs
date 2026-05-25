using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Presentation.ViewModels;
using System;
using System.ComponentModel;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel { get; }
    private readonly INavigationService _nav;
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
        if (e.NavigationMode != NavigationMode.Back && ViewModel.Games.Count == 0)
        {
            await ViewModel.LoadAsync();
        }
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
        if (sender is Button btn && btn.Tag is int appId)
        {
            await ViewModel.RemoveGameCommand.ExecuteAsync(appId.ToString());
        }
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
}
