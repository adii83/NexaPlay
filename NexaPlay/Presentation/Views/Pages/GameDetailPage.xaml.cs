using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using NexaPlay.Presentation.ViewModels;
using NexaPlay.Core.Models;
using Windows.System;
using Windows.Media.Core;
using Microsoft.Web.WebView2.Core;

namespace NexaPlay.Presentation.Views.Pages;

public sealed partial class GameDetailPage : Page
{
    private const double LibraryHeroWidth = 3840d;
    private const double LibraryHeroHeight = 1240d;
    private const double MinHeroHeight = 360d;

    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _mediaCarouselTimer;
    private CancellationTokenSource? _loadCts;
    private int _navigationSession;
    private long _renderStamp;
    private long _expectedRenderStamp;
    private long _aboutLoadWatchdogStamp;
    private bool _isPageActive;
    private Microsoft.UI.Xaml.Media.Animation.Storyboard? _denuvoPulseStoryboard;
    private static readonly JsonSerializerOptions _jsonCaseInsensitive = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Static cache: AppId → rendered height (pixels).
    /// Static agar bertahan meski page instance baru dibuat — cukup simpan angka,
    /// hampir nol overhead RAM. Digunakan untuk set height WebView2 INSTAN saat
    /// user revisit game yang sudah pernah dibuka sebelumnya (eliminasi loading ring).
    /// </summary>
    private static readonly Dictionary<int, double> _heightCache = new();

    /// <summary>
    /// AppId dari game yang saat ini sudah di-render di WebView2 instance ini.
    /// Jika sama dengan AppId yang diminta → skip NavigateToString sepenuhnya.
    /// </summary>
    private int _renderedForAppId = -1;
    private bool _hasValidRenderForCurrentApp;
    private readonly Dictionary<FrameworkElement, Storyboard> _metadataShimmerStoryboards = new();

    public GameDetailViewModel ViewModel { get; }

    public GameDetailPage()
    {
        ViewModel = ((App)App.Current).GetRequiredService<GameDetailViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isPageActive = true;
        var session = Interlocked.Increment(ref _navigationSession);
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        try
        {
            await AboutGameWebView.EnsureCoreWebView2Async();
        }
        catch { }

        if (!IsSessionActive(session))
            return;

        if (e.Parameter is int appId)
        {
            try
            {
                await ViewModel.LoadAsync(appId, _loadCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        if (!IsSessionActive(session))
            return;

        RenderAboutGameWebView();
            
        // Initialize smart auto-scroll timer to tick every 4 seconds
        _mediaCarouselTimer = DispatcherQueue.CreateTimer();
        _mediaCarouselTimer.Interval = TimeSpan.FromSeconds(4);
        _mediaCarouselTimer.Tick += MediaCarouselTimer_Tick;
        _mediaCarouselTimer.Start();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _isPageActive = false;
        Interlocked.Increment(ref _navigationSession);
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        // WebView2 TIDAK di-Close() agar instance + height tetap hidup.
        // Height sudah tersimpan di _heightCache (static) untuk revisit instan.

        if (_mediaCarouselTimer != null)
        {
            _mediaCarouselTimer.Stop();
            _mediaCarouselTimer.Tick -= MediaCarouselTimer_Tick;
            _mediaCarouselTimer = null;
        }

        StopAllMetadataShimmerAnimations();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameDetailViewModel.DisplayRichDescription))
        {
            RenderAboutGameWebView();
        }
        else if (e.PropertyName == nameof(GameDetailViewModel.HasDenuvo))
        {
            if (ViewModel.HasDenuvo)
            {
                StartDenuvoPulse();
            }
            else
            {
                StopDenuvoPulse();
            }
        }
    }

    /// <summary>
    /// Strip heading pertama yang bunyinya "About the Game" / "About This Game"
    /// dari HTML Steam — karena kita sudah tidak pakai native header di atas WebView2.
    /// </summary>
    private static string StripAboutGameHeading(string html)
    {
        // Match: <h1>, <h2>, atau <h3> dengan konten teks "about the game" / "about this game"
        // Termasuk varian dengan atribut (e.g. <h2 class="bb_tag">)
        var regex = new System.Text.RegularExpressions.Regex(
            @"<h[1-3][^>]*>\s*About\s+(?:the|this)\s+Game\s*</h[1-3]>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var match = regex.Match(html);
        return match.Success ? html.Remove(match.Index, match.Length) : html;
    }

    /// <summary>
    /// Render About the Game dengan smart height cache:
    ///   Tier 1 — Same AppId di instance ini → skip sepenuhnya (0ms)
    ///   Tier 2 — AppId pernah dikunjungi → height di-set instan dari _heightCache,
    ///            render di background tanpa loading ring (smooth, no flash)
    ///   Tier 3 — Cold open → show loading ring, render normal
    /// </summary>
    private void RenderAboutGameWebView()
    {
        if (!_isPageActive)
            return;

        var rawHtml = ViewModel.DisplayRichDescription;
        var appId   = ViewModel.Game?.AppId ?? 0;

        if (string.IsNullOrWhiteSpace(rawHtml))
        {
            AboutGameWebView.Visibility = Visibility.Collapsed;
            ViewModel.IsAboutContentLoading = false;
            return;
        }

        AboutGameWebView.Visibility = Visibility.Visible;

        // ── Tier 1: Game yang sama sudah di-render di instance ini ────────────────
        // Skip NavigateToString sepenuhnya — 0ms, tidak ada perubahan UI apapun.
        if (_hasValidRenderForCurrentApp && _renderedForAppId == appId && AboutGameWebView.Height > 0)
        {
            ViewModel.IsAboutContentLoading = false;
            return;
        }

        // ── Tier 2: Height di-cache dari kunjungan sebelumnya ────────────────────
        // Set height INSTAN (O(1)) → tidak ada layout jump, tidak ada loading ring.
        // WebView2 render di background — user tidak merasakan delay.
        if (_heightCache.TryGetValue(appId, out double cachedHeight))
        {
            AboutGameWebView.Height = cachedHeight;
            ViewModel.IsAboutContentLoading = false;
        }
        else
        {
            // ── Tier 3: Cold open — tampilkan loading ring ────────────────────────
            ViewModel.IsAboutContentLoading = true;
        }

        // Strip heading "About the Game" dari HTML (duplikat dengan section divider)
        var cleanHtml = StripAboutGameHeading(rawHtml);
        if (string.IsNullOrWhiteSpace(cleanHtml))
        {
            cleanHtml = rawHtml;
        }

        var html = $@"
        <html>
        <head>
            <style>
                html, body {{
                    background-color: #080808 !important;
                    color: #C6C9CE;
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    font-size: 14px;
                    line-height: 1.85;
                    margin: 0;
                    padding: 0;
                    overflow: hidden;
                }}
                * {{
                    background-color: transparent !important;
                    background: transparent !important;
                    box-sizing: border-box;
                }}
                #nexacontent {{ padding-bottom: 4px; }}
                img, video, iframe {{
                    max-width: 100%;
                    height: auto;
                    border-radius: 6px;
                    display: block;
                    margin: 14px 0;
                }}
                a {{ color: #66C0F4; text-decoration: none; }}
                a:hover {{ text-decoration: underline; }}
                /* ── NexaPlay-style headings: UPPERCASE + garis putih kiri ── */
                h1, h2, h3, h4, .bb_h1, .bb_h2, .bb_h3 {{
                    text-transform: uppercase;
                    letter-spacing: 2px;
                    font-weight: 700;
                    color: #FFFFFF;
                    padding-left: 14px;
                    border-left: 4px solid #FFFFFF;
                    margin: 28px 0 16px 0;
                    line-height: 1.4;
                }}
                h1, .bb_h1 {{ font-size: 16px; }}
                h2, .bb_h2 {{ font-size: 15px; }}
                h3, .bb_h3 {{ font-size: 14px; font-weight: 600; }}
                h4          {{ font-size: 13px; font-weight: 600; }}
                ul, ol {{ padding-left: 22px; margin: 8px 0 14px 0; }}
                li {{ margin-bottom: 6px; line-height: 1.7; }}
                strong, b {{ color: #E8E8E8; font-weight: 600; }}
                p {{ margin: 0 0 14px 0; }}
                p:first-child {{ margin-top: 0; }}
            </style>
        </head>
        <body>
            <div id=""nexacontent"">
                {cleanHtml}
            </div>
        </body>
        </html>";

        try
        {
            _hasValidRenderForCurrentApp = false;
            _expectedRenderStamp = Interlocked.Increment(ref _renderStamp);
            AboutGameWebView.NavigateToString(html);
            StartAboutLoadWatchdog(_expectedRenderStamp);
        }
        catch
        {
            ViewModel.IsAboutContentLoading = false;
        }
    }

    private async void AboutGameWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!_isPageActive)
            return;

        if (!args.IsSuccess)
        {
            ViewModel.IsAboutContentLoading = false;
            return;
        }

        // Fail-safe: jangan biarkan loading ring nyangkut jika JS height callback gagal.
        if (sender.Height <= 0)
            sender.Height = 320;
        ViewModel.IsAboutContentLoading = false;

        try
        {
            // Ukur #nexacontent.scrollHeight — lebih presisi daripada documentElement.
            // Tambah buffer 32px untuk bottom margin/padding yang mungkin tidak ter-report.
            // Tiga titik pengukuran: DOM ready, onload (gambar selesai), dan 1500ms fallback.
            var appId = ViewModel.Game?.AppId ?? 0;
            var stamp = _expectedRenderStamp;
            await sender.ExecuteScriptAsync(@"
                var __nexaAppId = " + appId + @";
                var __nexaStamp = " + stamp + @";
                var _nexaReported = false;
                function reportHeight() {
                    var el = document.getElementById('nexacontent');
                    var h = el ? (el.scrollHeight + 32) : (document.documentElement.scrollHeight + 32);
                    if (h > 0) {
                        window.chrome.webview.postMessage(JSON.stringify({ appId: __nexaAppId, stamp: __nexaStamp, height: h }));
                        _nexaReported = true;
                    }
                }
                reportHeight();
                window.addEventListener('load', function() { setTimeout(reportHeight, 100); });
                setTimeout(reportHeight, 1500);
            ");
        }
        catch
        {
            ViewModel.IsAboutContentLoading = false;
        }
    }

    private void AboutGameWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            if (!_isPageActive)
                return;

            var raw = args.TryGetWebMessageAsString();
            WebViewHeightPayload? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<WebViewHeightPayload>(raw, _jsonCaseInsensitive);
            }
            catch
            {
                payload = null;
            }

            var currentAppId = ViewModel.Game?.AppId ?? 0;
            double measuredHeight = 0;
            if (payload is not null && payload.Height > 0)
            {
                if (payload.AppId > 0 && payload.AppId != currentAppId)
                    return;
                
                if (payload.Stamp > 0 && payload.Stamp != _expectedRenderStamp)
                    return;

                measuredHeight = payload.Height;
            }
            else if (!double.TryParse(raw, out measuredHeight) || measuredHeight <= 0)
            {
                return;
            }

            // Defer ke UI thread — mencegah LayoutCycleException.
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (!_isPageActive)
                        return;

                    sender.Height = measuredHeight;
                    ViewModel.IsAboutContentLoading = false;

                    // Simpan ke static cache — O(1) write, hampir nol memory.
                    // Digunakan di Tier2 saat user revisit game ini di masa depan.
                    if (currentAppId > 0)
                    {
                        _heightCache[currentAppId] = measuredHeight;
                        _renderedForAppId = currentAppId;
                        _hasValidRenderForCurrentApp = true;
                    }
                }
                catch { }
            });
        }
        catch { }
    }

    private void StartAboutLoadWatchdog(long stamp)
    {
        _aboutLoadWatchdogStamp = stamp;
        var timer = DispatcherQueue.CreateTimer();
        timer.IsRepeating = false;
        timer.Interval = TimeSpan.FromMilliseconds(2500);
        timer.Tick += (_, _) =>
        {
            try
            {
                if (!_isPageActive)
                    return;

                if (stamp != _aboutLoadWatchdogStamp || stamp != _expectedRenderStamp)
                    return;

                if (ViewModel.IsAboutContentLoading)
                {
                    if (AboutGameWebView.Height <= 0)
                        AboutGameWebView.Height = 320;
                    ViewModel.IsAboutContentLoading = false;
                }
            }
            catch { }
        };
        timer.Start();
    }

    private void MediaCarouselTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        if (!_isPageActive || ViewModel.IsDetailLoading)
            return;

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
        if (viewportWidth > 0 && (itemStart < currentOffset || itemEnd > currentOffset + viewportWidth - 304.0))
        {
            MediaScrollViewer.ChangeView(nextIndex * 304.0, null, null);
        }
    }

    private bool IsSessionActive(int session) =>
        _isPageActive && session == _navigationSession && (_loadCts?.IsCancellationRequested != true);

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

    public static int GetPostAboutScreenshotCount(
        IReadOnlyList<ScreenshotEntry> screenshots,
        string? overview1,
        string? overview2,
        string? overview3) =>
        BuildPostAboutScreenshotUrls(screenshots, overview1, overview2, overview3).Count;

    public static string GetPostAboutScreenshotUrl(
        IReadOnlyList<ScreenshotEntry> screenshots,
        string? overview1,
        string? overview2,
        string? overview3,
        int index)
    {
        var items = BuildPostAboutScreenshotUrls(screenshots, overview1, overview2, overview3);
        return index >= 0 && index < items.Count ? items[index] : string.Empty;
    }

    public static string GetPostAboutHeroScreenshotUrl(
        IReadOnlyList<ScreenshotEntry> screenshots,
        string? overview1,
        string? overview2,
        string? overview3) =>
        GetPostAboutScreenshotUrl(screenshots, overview1, overview2, overview3, 0);

    public static IReadOnlyList<string> GetPostAboutTailScreenshotUrls(
        IReadOnlyList<ScreenshotEntry> screenshots,
        string? overview1,
        string? overview2,
        string? overview3)
    {
        var items = BuildPostAboutScreenshotUrls(screenshots, overview1, overview2, overview3);
        if (items.Count <= 1)
            return Array.Empty<string>();
        return items.Skip(1).ToArray();
    }

    public static Visibility PostAboutLayoutVisibility(
        IReadOnlyList<ScreenshotEntry> screenshots,
        string? overview1,
        string? overview2,
        string? overview3,
        int minCountInclusive,
        int maxCountInclusive)
    {
        var count = GetPostAboutScreenshotCount(screenshots, overview1, overview2, overview3);
        return count >= minCountInclusive && count <= maxCountInclusive
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static IReadOnlyList<string> BuildPostAboutScreenshotUrls(
        IReadOnlyList<ScreenshotEntry> screenshots,
        string? overview1,
        string? overview2,
        string? overview3)
    {
        if (screenshots.Count == 0)
            return Array.Empty<string>();

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfNotEmpty(excluded, overview1);
        AddIfNotEmpty(excluded, overview2);
        AddIfNotEmpty(excluded, overview3);

        var result = new List<string>(capacity: 5);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var shot in screenshots)
        {
            var url = shot.FullUrl;
            if (string.IsNullOrWhiteSpace(url))
                continue;
            if (excluded.Contains(url))
                continue;
            if (!seen.Add(url))
                continue;

            result.Add(url);
            if (result.Count == 5)
                break;
        }

        return result;
    }

    private static void AddIfNotEmpty(HashSet<string> set, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            set.Add(value);
    }

    public static string FormatAboutText(string? aboutHtml, string? shortDescription)
    {
        var source = !string.IsNullOrWhiteSpace(aboutHtml) ? aboutHtml : shortDescription;
        if (string.IsNullOrWhiteSpace(source))
            return "No description available.";

        var text = source;
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
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
        text = Regex.Replace(text, @"\r\n|\r", "\n");
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</p\s*>", "\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<p[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</div\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<div[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</li\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<li[^>]*>", "- ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</?(ul|ol)[^>]*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"[ \t]+\n", "\n");
        text = Regex.Replace(text, @"[ \t]{2,}", " ");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = Regex.Replace(text, @"\u2022\s*", "- ");
        text = Regex.Replace(text, @"^(?:-\s*){2,}", "- ", RegexOptions.Multiline);
        return text.Trim() is { Length: > 0 } cleaned ? cleaned : "Not specified.";
    }

    public static Uri SafeUri(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Uri.TryCreate(raw, UriKind.Absolute, out var parsed))
            return parsed;

        return new Uri("ms-appx:///Assets/StoreLogo.png");
    }

    public static MediaSource? StringToMediaSource(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return MediaSource.CreateFromUri(uri);
        return null;
    }

    private void RichVideo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MediaPlayerElement mpe)
        {
            if (mpe.MediaPlayer == null)
            {
                mpe.SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
            }
            
            if (mpe.MediaPlayer != null)
            {
                mpe.MediaPlayer.IsLoopingEnabled = true;
                mpe.MediaPlayer.IsMuted = true;
            }
        }
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

    private void DenuvoBadge_Loaded(object sender, RoutedEventArgs e)
    {
        StartDenuvoPulse();
    }

    private void DenuvoBadge_Unloaded(object sender, RoutedEventArgs e)
    {
        StopDenuvoPulse();
    }

    private void MetadataImage_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Image image)
            return;

        image.Visibility = Visibility.Visible;
        image.Opacity = 0;
        var overlay = FindSiblingSkeletonOverlay(image);
        if (overlay is not null)
        {
            overlay.Visibility = Visibility.Visible;
        }
    }

    private void MetadataImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        RevealMetadataImage(sender);
    }

    private void MetadataImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is not Image image)
            return;

        image.Visibility = Visibility.Collapsed;
        image.Opacity = 0;
        var overlay = FindSiblingSkeletonOverlay(image);
        if (overlay is not null)
        {
            overlay.Visibility = Visibility.Visible;
            FreezeMetadataSkeletonOverlay(overlay);
        }
    }

    private void RevealMetadataImage(object sender)
    {
        if (sender is not Image image)
            return;

        image.Visibility = Visibility.Visible;
        image.Opacity = 1;
        var overlay = FindSiblingSkeletonOverlay(image);
        if (overlay is not null)
        {
            overlay.Visibility = Visibility.Collapsed;
        }
    }

    private FrameworkElement? FindSiblingSkeletonOverlay(Image image)
    {
        if (image.Parent is not Panel panel)
            return null;

        foreach (var child in panel.Children)
        {
            if (ReferenceEquals(child, image))
                continue;

            if (child is FrameworkElement fe && fe.Tag is string tag && tag == "SkeletonOverlay")
                return fe;
        }

        return null;
    }

    private void MetadataSkeletonSweep_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement sweep || sweep.RenderTransform is not TranslateTransform transform)
            return;

        StartMetadataShimmerAnimation(sweep, transform);
    }

    private void MetadataSkeletonSweep_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement sweep)
            return;

        if (_metadataShimmerStoryboards.TryGetValue(sweep, out var storyboard))
        {
            storyboard.Stop();
            _metadataShimmerStoryboards.Remove(sweep);
        }
    }

    private void StartMetadataShimmerAnimation(FrameworkElement sweep, TranslateTransform transform)
    {
        if (_metadataShimmerStoryboards.TryGetValue(sweep, out var existing))
        {
            existing.Stop();
            _metadataShimmerStoryboards.Remove(sweep);
        }

        var parentWidth = (sweep.Parent as FrameworkElement)?.ActualWidth;
        var start = -Math.Max(180, sweep.Width > 0 ? sweep.Width : 240);
        var end = ((parentWidth is > 0 ? parentWidth.Value : RootLayout.ActualWidth > 0 ? RootLayout.ActualWidth : 1200) + 120);

        var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var anim = new DoubleAnimation
        {
            From = start,
            To = end,
            Duration = TimeSpan.FromMilliseconds(1200)
        };
        Storyboard.SetTarget(anim, transform);
        Storyboard.SetTargetProperty(anim, "X");
        storyboard.Children.Add(anim);
        storyboard.Begin();
        _metadataShimmerStoryboards[sweep] = storyboard;
    }

    private void StopAllMetadataShimmerAnimations()
    {
        foreach (var storyboard in _metadataShimmerStoryboards.Values)
        {
            storyboard.Stop();
        }
        _metadataShimmerStoryboards.Clear();
    }

    private void FreezeMetadataSkeletonOverlay(FrameworkElement overlay)
    {
        if (overlay is not Panel panel)
            return;

        foreach (var child in panel.Children)
        {
            if (child is not FrameworkElement fe)
                continue;

            if (_metadataShimmerStoryboards.TryGetValue(fe, out var storyboard))
            {
                storyboard.Stop();
                _metadataShimmerStoryboards.Remove(fe);
            }

            // child shimmer sweep disembunyikan agar tidak terlihat "loading tanpa akhir"
            if (fe is Border)
            {
                fe.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void StartDenuvoPulse()
    {
        if (DenuvoBadge is null)
            return;

        if (_denuvoPulseStoryboard == null)
        {
            _denuvoPulseStoryboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            var badgePulse = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.3,
                Duration = new Duration(TimeSpan.FromMilliseconds(700)),
                AutoReverse = true,
                RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever
            };

            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(badgePulse, DenuvoBadge);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(badgePulse, "Opacity");
            _denuvoPulseStoryboard.Children.Add(badgePulse);

            if (DenuvoBadgeGlowOverlay is not null)
            {
                var glowPulse = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 0.0,
                    To = 0.42,
                    Duration = new Duration(TimeSpan.FromMilliseconds(700)),
                    AutoReverse = true,
                    RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever
                };

                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(glowPulse, DenuvoBadgeGlowOverlay);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(glowPulse, "Opacity");
                _denuvoPulseStoryboard.Children.Add(glowPulse);
            }
        }

        _denuvoPulseStoryboard.Begin();
    }

    private void StopDenuvoPulse()
    {
        _denuvoPulseStoryboard?.Stop();
        if (DenuvoBadge is not null)
        {
            DenuvoBadge.Opacity = 1;
        }
        if (DenuvoBadgeGlowOverlay is not null)
        {
            DenuvoBadgeGlowOverlay.Opacity = 0;
        }
    }

    private sealed class WebViewHeightPayload
    {
        [JsonPropertyName("appId")]
        public int AppId { get; set; }
        [JsonPropertyName("stamp")]
        public long Stamp { get; set; }
        [JsonPropertyName("height")]
        public double Height { get; set; }
    }
}

public class SystemRequirementItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
