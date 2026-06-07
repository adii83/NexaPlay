using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Presentation.ViewModels;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _vm;
    public SettingsViewModel? ViewModel => _vm;

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

    private void OnCopyLicenseKeyClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_vm?.CurrentLicense?.Key))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(_vm.CurrentLicense.Key);
            Clipboard.SetContent(dataPackage);
        }
    }

    private void OnCopyDeviceIdClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_vm?.DeviceId))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(_vm.DeviceId);
            Clipboard.SetContent(dataPackage);
        }
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
        var result = await _vm.ClearMetadataCacheAsync();
        
        var dialog = new ContentDialog
        {
            Title = result.Success ? "Berhasil" : "Gagal",
            Content = result.Message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 17, 17))
        };
        await dialog.ShowAsync();
    }

    private async void OnClearDataClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        
        var dialog = new ContentDialog
        {
            Title = "Clear Data",
            Content = "Apakah Anda yakin ingin menghapus semua data lokal (cache, status, file konfigurasi) dan merestart aplikasi? Tindakan ini tidak dapat dibatalkan.",
            PrimaryButtonText = "Ya, Hapus",
            CloseButtonText = "Batal",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 17, 17))
        };

        // Override resource for primary button to make it white with hover transparency
        dialog.Resources["AccentButtonBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        dialog.Resources["AccentButtonForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        dialog.Resources["AccentButtonBackgroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(204, 255, 255, 255));
        dialog.Resources["AccentButtonForegroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        dialog.Resources["AccentButtonBackgroundPressed"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(153, 255, 255, 255));
        dialog.Resources["AccentButtonForegroundPressed"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _vm.ClearAllDataAndRestartAsync();
        }
    }

    private async void OnLoadGamesClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        LoadingOverlay.Visibility = Visibility.Visible;
        var result = await _vm.LoadGamesAsync();
        LoadingOverlay.Visibility = Visibility.Collapsed;

        var dialog = new ContentDialog
        {
            Title = result.Success ? "Proses Selesai" : "Proses Gagal",
            Content = result.Message,
            CloseButtonText = "Tutup",
            XamlRoot = this.XamlRoot,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 17, 17))
        };
        await dialog.ShowAsync();
    }

    private async void OnRefreshFixDataClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.RefreshFixDataAsync();
    }
}
