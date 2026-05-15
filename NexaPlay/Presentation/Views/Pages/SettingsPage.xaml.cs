using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Presentation.ViewModels;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _vm;

    public SettingsPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _vm = ((App)App.Current).GetRequiredService<SettingsViewModel>();
        DataContext = _vm;
        await _vm.LoadAsync();
    }

    // ── License ──────────────────────────────────────────────────

    private async void OnActivateClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.ActivateLicenseAsync();
        ShowActivationStatus();
    }

    private async void OnDeactivateClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.DeactivateLicenseAsync();
        ShowActivationStatus();
    }

    private void ShowActivationStatus()
    {
        if (_vm is null) return;
        // ActivationMessage is data-bound in XAML via {Binding ActivationMessage}
    }

    // ── Steam ─────────────────────────────────────────────────────

    private async void OnScanLibraryClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.ScanSteamLibraryAsync();
    }

    private async void OnRestartSteamClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.RestartSteamAsync();
    }

    // ── Windows Defender ──────────────────────────────────────────

    private async void OnDetectAntivirusClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.DetectAntivirusAsync();
    }

    // ── Cache ─────────────────────────────────────────────────────

    private async void OnClearCacheClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.ClearMetadataCacheAsync();
    }

    private async void OnRefreshFixDataClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.RefreshFixDataAsync();
    }
}
