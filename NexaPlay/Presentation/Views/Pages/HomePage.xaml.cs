using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Presentation.ViewModels;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel => (HomeViewModel)DataContext;

    public HomePage() 
    {
        InitializeComponent();
        DataContext = ((App)App.Current).GetRequiredService<HomeViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    // Converters for x:Bind
    public static Microsoft.UI.Xaml.Visibility InverseBoolToVis(bool val) => val ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
    public static Microsoft.UI.Xaml.Visibility BoolToVis(bool val) => val ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
}
