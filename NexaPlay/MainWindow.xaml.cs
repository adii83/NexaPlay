using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Presentation.ViewModels;
using NexaPlay.Presentation.Views.Dialogs;
using NexaPlay.Presentation.Views.Pages;
using System;

namespace NexaPlay;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly INavigationService _nav;
    private readonly ILicenseService _licenseService;

    public MainWindow(MainViewModel vm, INavigationService nav, ILicenseService licenseService)
    {
        _vm             = vm;
        _nav            = nav;
        _licenseService = licenseService;
        InitializeComponent();
        _nav.Initialize(ContentFrame);
        this.Activated += OnFirstActivated;
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        this.Activated -= OnFirstActivated;

        // Ensure XamlRoot is ready before showing dialogs
        if (ContentFrame.XamlRoot != null)
        {
            _ = ValidateLicenseAsync();
        }
        else
        {
            ContentFrame.Loaded += (s, args) => _ = ValidateLicenseAsync();
        }

        NavigateTo(NavHome);
    }

    private async System.Threading.Tasks.Task ValidateLicenseAsync()
    {
        var license = await _licenseService.LoadAsync();
        if (!license.IsValid)
        {
            ShowLicenseActivation();
        }
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb) NavigateTo(rb);
    }

    private void NavigateTo(RadioButton rb)
    {
        if (ContentFrame is null) return;

        // Update all label styles
        SetNavStyle(NavHome,     LblHome,     rb == NavHome);
        SetNavStyle(NavGames,    LblGames,    rb == NavGames);
        SetNavStyle(NavLibrary,  LblLibrary,  rb == NavLibrary);
        SetNavStyle(NavFixGames, LblFixGames, rb == NavFixGames);
        SetNavStyle(NavSettings, LblSettings, rb == NavSettings);

        Type? targetPage = null;
        string title = "Home";

        if (rb == NavHome)      { targetPage = typeof(HomePage);     title = "Home"; }
        else if (rb == NavGames)    { targetPage = typeof(GamesPage);    title = "Games"; }
        else if (rb == NavLibrary)  { targetPage = typeof(LibraryPage);  title = "Library"; }
        else if (rb == NavFixGames) { targetPage = typeof(FixGamesPage); title = "Fix Games"; }
        else if (rb == NavSettings) { targetPage = typeof(SettingsPage); title = "Settings"; }

        if (targetPage is not null && PageTitleText is not null)
        {
            PageTitleText.Text = title;
            ContentFrame.Navigate(targetPage, null, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
        }
    }

    private static void SetNavStyle(RadioButton? rb, TextBlock? label, bool active)
    {
        if (label is null) return;
        label.Style = active
            ? (Microsoft.UI.Xaml.Style)Application.Current.Resources["NavLabelActiveStyle"]
            : (Microsoft.UI.Xaml.Style)Application.Current.Resources["NavLabelStyle"];
    }

    private async void ShowLicenseActivation()
    {
        if (ContentFrame.XamlRoot == null) return;

        var dialog = new LicenseActivationDialog(_licenseService)
        {
            XamlRoot = ContentFrame.XamlRoot
        };

        try 
        {
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("crash_dialog.txt", "Dialog Exception: " + ex.ToString());
        }
    }
}
