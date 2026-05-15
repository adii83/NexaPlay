using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.ViewModels;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class FixGamesPage : Page
{
    private FixGamesViewModel? _vm;

    public FixGamesPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _vm = ((App)App.Current).GetRequiredService<FixGamesViewModel>();

        // Show loading
        // LoadingIndicator.Visibility = Visibility.Visible;
        // FixList.Visibility = Visibility.Collapsed;

        await _vm.LoadAsync();

        // FixList.ItemsSource = _vm.FilteredFixes;
        // LoadingIndicator.Visibility = Visibility.Collapsed;
        // FixList.Visibility = Visibility.Visible;

        // Scan AV in background
        _ = Task.Run(async () =>
        {
            await _vm.ScanSystemAsync();
            // DispatcherQueue.TryEnqueue(() => AvList.ItemsSource = _vm.AntivirusList);
        });
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_vm is null) return;
        // _vm.SearchQuery = SearchBox.Text;
        // FixList.ItemsSource = _vm.FilteredFixes;
    }

    private async void OnScanSystemClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        // ScanSystemBtn.IsEnabled = false;
        await _vm.ScanSystemAsync();
        // AvList.ItemsSource = _vm.AntivirusList;
        // ScanSystemBtn.IsEnabled = true;
    }

    private async void OnApplyFixClicked(object sender, RoutedEventArgs e)
    {
        if (_vm is null || sender is not Button btn) return;
        if (btn.Tag is not FixEntry fix) return;

        // Update UI header
        // SelectedGameTitle.Text     = fix.Title;
        // SelectedGamePublisher.Text = fix.Publisher;

        // Show progress UI
        // ShowProgressUI(true);
        // SetStep(1);
        // UpdateStatus(FixStatus.Downloading, "Checking availability...", 0);

        using var cts = new CancellationTokenSource();
        // CancelFixBtn.Tag = cts;

        var progress = new Progress<FixProgressState>(state =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // UpdateStatus(state.Status, GetStatusMessage(state), state.Percent < 0 ? (int)FixProgressBar.Value : state.Percent);

                switch (state.Phase)
                {
                    // case "download": SetStep(2); break;
                    // case "extract":
                    // case "done":    SetStep(3); break;
                }
            });
        });

        await _vm.ApplyFixAsync(fix);

        // Reflect final VM state
        DispatcherQueue.TryEnqueue(() =>
        {
            // UpdateStatus(_vm.CurrentFixStatus, _vm.FixStatusMessage, 100);
            // FixProgressRing.IsActive = false;
            // if (_vm.CurrentFixStatus == FixStatus.Applied) SetAllStepsDone();
        });

        // ShowProgressUI(false);
    }

    private void OnCancelFixClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CancellationTokenSource cts)
            cts.Cancel();
        _vm?.CancelFix();
        // ShowProgressUI(false);
        // UpdateStatus(FixStatus.Cancelled, "Cancelled by user", 0);
    }

    // ─── UI Helpers ────────────────────────────────────────────────

    private void ShowProgressUI(bool show)
    {
        /*
        ProgressBarPanel.Visibility = show ? Visibility.Visible  : Visibility.Collapsed;
        StepsPanel.Visibility       = show ? Visibility.Visible  : Visibility.Collapsed;
        CancelFixBtn.Visibility     = show ? Visibility.Visible  : Visibility.Collapsed;
        FixProgressRing.IsActive    = show;
        */
    }

    private void UpdateStatus(FixStatus status, string message, int pct)
    {
        /*
        FixStatusText.Text      = message;
        FixProgressBar.Value    = pct;
        FixProgressPct.Text     = $"{pct}%";
        FixStatusText.Foreground = StatusColor(status);
        */
    }

    private void SetStep(int step)
    {
        /*
        // Step 1=check, 2=download, 3=apply
        SetStepActive(StepCheck,    step >= 1, step > 1);
        SetStepActive(StepDownload, step >= 2, step > 2);
        SetStepActive(StepApply,    step >= 3, false);
        */
    }

    private void SetAllStepsDone()
    {
        /*
        SetStepActive(StepCheck,    true, true);
        SetStepActive(StepDownload, true, true);
        SetStepActive(StepApply,    true, true);
        */
    }

    private static void SetStepActive(Border border, bool active, bool done)
    {
        /*
        var panel = border.Child as StackPanel;
        if (panel is null) return;

        if (done)
        {
            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 34, 197, 94));
            foreach (var child in panel.Children)
            {
                if (child is FontIcon fi) fi.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
                if (child is TextBlock tb) tb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
            }
        }
        else if (active)
        {
            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 139, 124, 248));
            foreach (var child in panel.Children)
            {
                if (child is FontIcon fi) fi.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 124, 248));
                if (child is TextBlock tb) tb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 124, 248));
            }
        }
        else
        {
            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(48, 34, 34, 34));
            foreach (var child in panel.Children)
            {
                if (child is FontIcon fi) fi.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 80, 80, 80));
                if (child is TextBlock tb) tb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 80, 80, 80));
            }
        }
        */
    }

    private static SolidColorBrush StatusColor(FixStatus status) => status switch
    {
        FixStatus.Applied        => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34,  197, 94)),
        FixStatus.Failed         => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68,  68)),
        FixStatus.Cancelled      => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
        FixStatus.NotAvailable   => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
        _                        => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 138, 138, 138)),
    };

    private static string GetStatusMessage(FixProgressState state) => state.Phase switch
    {
        "download" => $"Downloading... {(state.Percent >= 0 ? state.Percent + "%" : "")}",
        "extract"  => "Extracting files...",
        "done"     => state.Status == FixStatus.Applied ? "Fix applied successfully!" : state.Error ?? "Failed",
        _          => state.Message ?? state.Status.ToString()
    };
}
