using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using System;
using NexaPlay.Presentation.ViewModels;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel => (HomeViewModel)DataContext;
    private DispatcherTimer _carouselTimer;

    public HomePage() 
    {
        InitializeComponent();
        DataContext = ((App)App.Current).GetRequiredService<HomeViewModel>();
        SetupCarouselTimer();
    }

    private void SetupCarouselTimer()
    {
        _carouselTimer = new DispatcherTimer();
        _carouselTimer.Interval = TimeSpan.FromSeconds(5); // Auto-scroll every 5 seconds
        _carouselTimer.Tick += CarouselTimer_Tick;
        this.Loaded += (s, e) => _carouselTimer.Start();
        this.Unloaded += (s, e) => _carouselTimer.Stop();
    }

    private void CarouselTimer_Tick(object sender, object e)
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
        UpdateCustomPips();
    }

    // Converters for x:Bind
    public static Microsoft.UI.Xaml.Visibility InverseBoolToVis(bool val) => val ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
    public static Microsoft.UI.Xaml.Visibility BoolToVis(bool val) => val ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

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
}
