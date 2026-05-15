using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Presentation.ViewModels;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class LibraryPage : Page
{
    private LibraryViewModel? _vm;

    public LibraryPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _vm = ((App)App.Current).GetRequiredService<LibraryViewModel>();
        DataContext = _vm;
        await _vm.LoadAsync();
    }
}
