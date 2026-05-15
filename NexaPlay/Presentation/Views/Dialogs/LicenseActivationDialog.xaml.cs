using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;

namespace NexaPlay.Presentation.Views.Dialogs;

public sealed partial class LicenseActivationDialog : ContentDialog
{
    private readonly ILicenseService _licenseService;

    public LicenseActivationDialog(ILicenseService licenseService)
    {
        _licenseService = licenseService;
        InitializeComponent();
    }

    private async void OnActivateClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var key = LicenseKeyBox.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(key))
            {
                ShowStatus(false, "&#xE783;", "Please enter your license key.", "#EF4444", "#18EF4444");
                args.Cancel = true;
                return;
            }

            SetLoading(true);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var result = await _licenseService.ActivateAsync(key, cts.Token);
            SetLoading(false);

            if (result.IsValid)
            {
                ShowStatus(true, "&#xE73E;", $"License activated! Plan: {result.Plan}", "#22C55E", "#1822C55E");
            }
            else
            {
                var msg = result.Status switch
                {
                    LicenseStatus.Banned         => "This license key has been banned.",
                    LicenseStatus.DeviceMismatch => "License is bound to a different device.",
                    LicenseStatus.Offline        => "Cannot connect to server. Saved for offline use.",
                    LicenseStatus.NetworkError   => "Network error. Check your connection and try again.",
                    _                            => result.Message ?? "Invalid license key. Please check and try again."
                };
                ShowStatus(false, "&#xEA39;", msg, "#EF4444", "#18EF4444");
                args.Cancel = true;
            }
        }
        finally { deferral.Complete(); }
    }

    private void SetLoading(bool loading)
    {
        LoadingPanel.Visibility  = loading ? Visibility.Visible   : Visibility.Collapsed;
        StatusBorder.Visibility  = loading ? Visibility.Collapsed : StatusBorder.Visibility;
        IsPrimaryButtonEnabled   = !loading;
        IsSecondaryButtonEnabled = !loading;
    }

    private void ShowStatus(bool success, string glyph, string message, string fgColor, string bgColor)
    {
        StatusBorder.Visibility  = Visibility.Visible;
        StatusBorder.Background  = new SolidColorBrush(ParseColor(bgColor));
        StatusIcon.Glyph         = System.Text.RegularExpressions.Regex.Unescape(glyph.Replace("&#x", "\\u").Replace(";", ""));
        StatusIcon.Foreground    = new SolidColorBrush(ParseColor(fgColor));
        StatusText.Text          = message;
        StatusText.Foreground    = new SolidColorBrush(ParseColor(fgColor));
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        return Windows.UI.Color.FromArgb(
            Convert.ToByte(hex[0..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16),
            Convert.ToByte(hex[6..8], 16));
    }
}
