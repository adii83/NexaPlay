namespace NexaPlay.Core.Models;

/// <summary>
/// Rich detail data fetched on-demand from Steam Store API (/api/appdetails).
/// Only loaded when the user opens a game detail page — never at startup.
/// Cached per-appid on disk for 7 days to avoid repeated API calls.
/// </summary>
public sealed class GameDetailEntry
{
    public int AppId { get; init; }

    // ── Description ─────────────────────────────────────────────
    /// <summary>One-liner description (also in GameEntry.ShortDescription).</summary>
    public string? ShortDescription { get; init; }

    /// <summary>Full "About the Game" HTML — strip tags before display.</summary>
    public string? AboutTheGame { get; init; }
    public string? DetailedDescription { get; init; }
    public string? SupportedLanguages { get; init; }
    public string? Website { get; init; }

    // ── Credits ─────────────────────────────────────────────────
    public IReadOnlyList<string> Developers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Publishers { get; init; } = Array.Empty<string>();

    // ── Release ──────────────────────────────────────────────────
    public string? ReleaseDate { get; init; }

    // ── Media ────────────────────────────────────────────────────
    public IReadOnlyList<ScreenshotEntry> Screenshots { get; init; } = Array.Empty<ScreenshotEntry>();
    public IReadOnlyList<MovieEntry> Movies { get; init; } = Array.Empty<MovieEntry>();

    /// <summary>
    /// Wide store background image URL.
    /// e.g. https://store.akamai.steamstatic.com/images/storepagebackground/app/{appId}
    /// </summary>
    public string? BackgroundImageUrl { get; init; }

    // ── System requirements ──────────────────────────────────────
    /// <summary>Minimum PC requirements (raw HTML from Steam — strip tags).</summary>
    public string? PcRequirementsMinimum { get; init; }

    /// <summary>Recommended PC requirements (raw HTML from Steam — strip tags).</summary>
    public string? PcRequirementsRecommended { get; init; }
    public string? MacRequirementsMinimum { get; init; }
    public string? MacRequirementsRecommended { get; init; }
    public string? LinuxRequirementsMinimum { get; init; }
    public string? LinuxRequirementsRecommended { get; init; }

    // ── Categories / tags ────────────────────────────────────────
    /// <summary>Steam category descriptions, e.g. "Single-player", "Co-op".</summary>
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();
    public string? SupportUrl { get; init; }
    public string? SupportEmail { get; init; }
    public string? LegalNotice { get; init; }
    public string? DrmNotice { get; init; }
    public string? StorePriceFinalFormatted { get; init; }
    public string? StorePriceCurrency { get; init; }

    // ── Cache metadata ───────────────────────────────────────────
    /// <summary>UTC timestamp when this entry was fetched and cached.</summary>
    public DateTime CachedAt { get; init; }

    /// <summary>True when the data was loaded from disk cache (not a fresh API call).</summary>
    public bool IsFromCache { get; init; }

    /// <summary>Full merged detail payload generated from Steam appdetails + original Steam assets.</summary>
    public string? RawMetadataJson { get; init; }

    public string? SgdbGridUrl => ReadSgdbUrl("sgdb_grid");
    public string? SgdbHeroUrl => ReadSgdbUrl("sgdb_hero");
    public string? SgdbIconUrl => ReadSgdbUrl("sgdb_icon");

    private string? ReadSgdbUrl(string key)
    {
        if (string.IsNullOrWhiteSpace(RawMetadataJson)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(RawMetadataJson);
            if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                assets.TryGetProperty(key, out var arr) &&
                arr.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == System.Text.Json.JsonValueKind.String)
                        return urlProp.GetString();
                }
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// The library_capsule URL from the merged API payload (portrait 600x900 poster).
    /// Extracted lazily from RawMetadataJson assets block.
    /// </summary>
    public string? LibraryCapsuleUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RawMetadataJson))
                return null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(RawMetadataJson);
                if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                    assets.TryGetProperty("library_capsule", out var arr) &&
                    arr.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        if (item.TryGetProperty("url", out var urlProp) &&
                            urlProp.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var url = urlProp.GetString();
                            if (!string.IsNullOrWhiteSpace(url))
                                return url;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
