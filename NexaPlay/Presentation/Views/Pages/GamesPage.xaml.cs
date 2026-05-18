using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Presentation.ViewModels;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class GamesPage : Page
{
    public GamesViewModel ViewModel { get; }
    private readonly INavigationService _nav;

    public GamesPage()
    {
        ViewModel = ((App)App.Current).GetRequiredService<GamesViewModel>();
        _nav      = ((App)App.Current).GetRequiredService<INavigationService>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    private void OnGameItemClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NexaPlay.Core.Models.FixEntry fix)
            _nav.Navigate<GameDetailPage>(fix.AppId);
    }

    // ── Static converters for x:Bind ────────────────────────────────────────

    public static Microsoft.UI.Xaml.Visibility BoolToVis(bool v) =>
        v ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public static Microsoft.UI.Xaml.Visibility InverseBoolToVis(bool v) =>
        v ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
}
