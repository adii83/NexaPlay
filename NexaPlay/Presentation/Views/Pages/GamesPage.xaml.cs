using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Presentation.ViewModels;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class GamesPage : Page
{
    private GamesViewModel? _vm;

    public GamesPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _vm = ((App)App.Current).GetRequiredService<GamesViewModel>();
        DataContext = _vm;
        await _vm.LoadAsync();
    }
}
