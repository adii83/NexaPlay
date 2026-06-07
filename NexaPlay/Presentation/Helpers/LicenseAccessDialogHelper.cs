using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace NexaPlay.Presentation.Helpers;

internal static class LicenseAccessDialogHelper
{
    public static Task ShowPremiumFeatureAsync(XamlRoot xamlRoot) =>
        ShowAsync(
            xamlRoot,
            "Fitur Premium",
            "Upgrade Ke Premium Dulu, Ya, Untuk Buka Fitur Ini 😁",
            "\uE7BA");

    public static Task ShowLicenseInvalidAsync(XamlRoot xamlRoot) =>
        ShowAsync(
            xamlRoot,
            "License Tidak Valid",
            "License tidak valid. Silakan aktivasi license terlebih dahulu.",
            "\uEA39");

    public static Task ShowVerificationFailedAsync(XamlRoot xamlRoot) =>
        ShowAsync(
            xamlRoot,
            "Verifikasi Gagal",
            "Verifikasi license gagal. Pastikan aplikasi berjalan dengan benar.",
            "\uEA39");

    public static async Task ShowAsync(XamlRoot xamlRoot, string title, string message, string glyph)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = string.Empty,
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
            Background = new SolidColorBrush(Color.FromArgb(255, 17, 17, 17)),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
            CornerRadius = new CornerRadius(20),
            Content = BuildContent(title, message, glyph)
        };

        dialog.Resources["SystemControlBackgroundAccentBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        dialog.Resources["SystemControlForegroundChromeWhiteBrush"] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        dialog.Resources["AccentButtonBackground"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        dialog.Resources["AccentButtonForeground"] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        dialog.Resources["AccentButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
        dialog.Resources["AccentButtonForegroundPointerOver"] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        dialog.Resources["AccentButtonBackgroundPressed"] = new SolidColorBrush(Color.FromArgb(170, 255, 255, 255));
        dialog.Resources["AccentButtonForegroundPressed"] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

        await dialog.ShowAsync();
    }

    private static UIElement BuildContent(string title, string message, string glyph)
    {
        var grid = new Grid
        {
            Width = 420,
            ColumnSpacing = 16
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconHost = new Border
        {
            Width = 48,
            Height = 48,
            CornerRadius = new CornerRadius(24),
            Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new FontIcon
            {
                Glyph = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 22,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var body = new StackPanel
        {
            Spacing = 8
        };
        body.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
            TextWrapping = TextWrapping.WrapWholeWords
        });
        body.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 185, 185, 185)),
            TextWrapping = TextWrapping.WrapWholeWords
        });

        Grid.SetColumn(iconHost, 0);
        Grid.SetColumn(body, 1);
        grid.Children.Add(iconHost);
        grid.Children.Add(body);
        return grid;
    }
}
