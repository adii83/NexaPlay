using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using NexaPlay.Presentation.ViewModels;
using NexaPlay.Core.Models;
using Windows.System;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class GameDetailPage : Page
{
    private const double LibraryHeroWidth = 3840d;
    private const double LibraryHeroHeight = 1240d;
    private const double MinHeroHeight = 360d;

    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _mediaCarouselTimer;

    public GameDetailViewModel ViewModel { get; }

    public GameDetailPage()
    {
        ViewModel = ((App)App.Current).GetRequiredService<GameDetailViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int appId)
            await ViewModel.LoadAsync(appId);
            
        // Initialize smart auto-scroll timer to tick every 4 seconds
        _mediaCarouselTimer = DispatcherQueue.CreateTimer();
        _mediaCarouselTimer.Interval = TimeSpan.FromSeconds(4);
        _mediaCarouselTimer.Tick += MediaCarouselTimer_Tick;
        _mediaCarouselTimer.Start();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (_mediaCarouselTimer != null)
        {
            _mediaCarouselTimer.Stop();
            _mediaCarouselTimer.Tick -= MediaCarouselTimer_Tick;
            _mediaCarouselTimer = null;
        }
    }

    private void MediaCarouselTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        var screenshots = ViewModel.Screenshots;
        if (screenshots.Count <= 1) return;
        
        int currentIndex = 0;
        for (int i = 0; i < screenshots.Count; i++)
        {
            if (screenshots[i].IsSelected)
            {
                currentIndex = i;
                break;
            }
        }
        
        int nextIndex = (currentIndex + 1) % screenshots.Count;
        var nextScreenshot = screenshots[nextIndex];
        ViewModel.SelectScreenshot(nextScreenshot.FullUrl);
        
        // Smart scroll: Check if the card is already visible within the current viewport
        double viewportWidth = MediaScrollViewer.ViewportWidth;
        double currentOffset = MediaScrollViewer.HorizontalOffset;
        
        // Item container Grid is exactly 304px wide
        double itemStart = nextIndex * 304.0;
        double itemEnd = itemStart + 304.0; 
        
        // If the card is outside the viewport (or too close to the right edge), scroll smoothly
        if (itemStart < currentOffset || itemEnd > currentOffset + viewportWidth - 304.0)
        {
            MediaScrollViewer.ChangeView(nextIndex * 304.0, null, null);
        }
    }

    public static Visibility BoolToVis(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InverseBoolToVis(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    public static double BoolToOpacity(bool value) =>
        value ? 1.0 : 0.0;

    public static Visibility StringToVis(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;

    public static string FormatPrice(string? display, int normalized) =>
        !string.IsNullOrWhiteSpace(display) ? display : normalized > 0 ? $"Rp {normalized:N0}" : "Free";

    public static string FormatCategories(IReadOnlyList<string> categories) =>
        categories.Count == 0 ? string.Empty : string.Join(" · ", categories.Take(4));

    public static string FormatAboutText(string? aboutHtml, string? shortDescription)
    {
        var source = !string.IsNullOrWhiteSpace(aboutHtml) ? aboutHtml : shortDescription;
        if (string.IsNullOrWhiteSpace(source))
            return "No description available.";

        var text = source;
        text = Regex.Replace(text, @"<br\s*/?>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</p\s*>", "\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</li\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<li[^>]*>", "- ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    public static string FormatDetailedText(string? html) =>
        FormatAboutText(html, string.Empty);

    public static IReadOnlyList<string> FormatRequirementsMin(GameDetailEntry? detail) =>
        FormatRequirementItems(detail?.PcRequirementsMinimum);

    public static IReadOnlyList<string> FormatRequirementsMax(GameDetailEntry? detail) =>
        FormatRequirementItems(detail?.PcRequirementsRecommended);

    public static string FormatRequirementText(string? html)
    {
        var lines = FormatRequirementItems(html).Select(static item => $"- {item}");
        return string.Join('\n', lines);
    }

    public static IReadOnlyList<string> FormatRequirementItems(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return ["Not specified."];

        var items = new List<string>();
        if (Regex.IsMatch(html, @"<li(\s|>)", RegexOptions.IgnoreCase))
        {
            foreach (Match match in Regex.Matches(html, @"<li[^>]*>(.*?)</li\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                AddRequirementItem(items, match.Groups[1].Value, mergeWithPrevious: false);
        }
        else
        {
            var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", string.Empty);
            text = WebUtility.HtmlDecode(text);

            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                AddRequirementItem(items, line, mergeWithPrevious: true);
        }

        return items.Count == 0 ? ["Not specified."] : items;
    }

    private static void AddRequirementItem(List<string> items, string htmlFragment, bool mergeWithPrevious)
    {
        var text = Regex.Replace(htmlFragment, @"<br\s*/?>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = NormalizeRequirementItem(text);

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (mergeWithPrevious && items.Count > 0 && !LooksLikeRequirementStart(text))
            items[^1] = $"{items[^1]} {text}";
        else
            items.Add(text);
    }

    private static string NormalizeRequirementItem(string text)
    {
        text = Regex.Replace(text.Trim(), @"^(â€¢|•|-)\s*", string.Empty);
        text = Regex.Replace(text, @"^(Minimum|Recommended):\s*", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"[ \t\r\n]{2,}", " ");
        return text.Trim();
    }

    private static bool LooksLikeRequirementStart(string text)
    {
        return Regex.IsMatch(
            text,
            @"^(Requires|OS\b|Processor\b|CPU\b|Memory\b|RAM\b|Graphics\b|GPU\b|DirectX\b|Storage\b|Sound\b|Sound Card\b|Network\b|Additional\b|Notes\b)",
            RegexOptions.IgnoreCase);
    }

    public static string FormatInlineHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "Not specified.";

        var text = html;
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    public static Uri SafeUri(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Uri.TryCreate(raw, UriKind.Absolute, out var parsed))
            return parsed;

        return new Uri("ms-appx:///Assets/StoreLogo.png");
    }

    private void RootLayout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var targetHeight = Math.Max(MinHeroHeight, e.NewSize.Width * LibraryHeroHeight / LibraryHeroWidth);
        HeroRowDefinition.Height = new GridLength(targetHeight);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }

    public static Vector3 GetSelectedScale(bool isSelected) =>
        isSelected ? new Vector3(1.05f, 1.05f, 1f) : Vector3.One;

    private void MediaCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string url)
        {
            ViewModel.SelectScreenshot(url);
            
            // Restart timer to prevent immediate jump after manual click
            _mediaCarouselTimer?.Stop();
            _mediaCarouselTimer?.Start();
            
            MediaOverlayImage.Source = new BitmapImage(SafeUri(url));
            MediaOverlay.Visibility = Visibility.Visible;
        }
    }

    private void MediaCard_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            if (element.ScaleTransition == null)
            {
                element.ScaleTransition = new Vector3Transition { Duration = TimeSpan.FromMilliseconds(250) };
            }
            element.CenterPoint = new Vector3((float)e.NewSize.Width / 2, (float)e.NewSize.Height / 2, 0f);
        }
    }

    private void MediaCard_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            if (element.ScaleTransition == null)
            {
                element.ScaleTransition = new Vector3Transition { Duration = TimeSpan.FromMilliseconds(250) };
            }
            
            element.CenterPoint = new Vector3((float)element.ActualWidth / 2, (float)element.ActualHeight / 2, 0f);
            element.Scale = new Vector3(1.05f, 1.05f, 1f);
        }
    }

    private void MediaCard_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            if (element.ScaleTransition == null)
            {
                element.ScaleTransition = new Vector3Transition { Duration = TimeSpan.FromMilliseconds(250) };
            }
            
            bool isSelected = false;
            if (element.DataContext is ScreenshotEntry entry)
                isSelected = entry.IsSelected;
                
            element.Scale = GetSelectedScale(isSelected);
        }
    }

    private void MediaOverlayClose_Click(object sender, RoutedEventArgs e)
    {
        MediaOverlay.Visibility = Visibility.Collapsed;
        MediaOverlayImage.Source = null;
    }
}

public class SystemRequirementItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
