namespace NexaPlay.Core.Models;

/// <summary>
/// Base game metadata loaded from steam data and overrides.
/// </summary>
public sealed class GameEntry
{
    public int AppId { get; init; }
    public string Name { get; set; } = string.Empty;

    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    public IReadOnlyList<string> Developers { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Publishers { get; set; } = Array.Empty<string>();

    public string? Genre { get; set; }
    public string? ShortDescription { get; set; }
    public string? ReleaseDate { get; set; }

    public int PriceNormalized { get; set; }
    public string? PriceDisplay { get; set; }
    public bool Protection { get; set; }

    public bool IsPremium => PriceNormalized >= 130_000;
    public bool HasDenuvo => Protection;

    public string DeveloperDisplay =>
        Developers.Count > 0 ? string.Join(", ", Developers) : Developer ?? string.Empty;

    public string PublisherDisplay =>
        Publishers.Count > 0 ? string.Join(", ", Publishers) : Publisher ?? string.Empty;

    private string? _headerImageUrl;
    public string HeaderImageUrl
    {
        get => _headerImageUrl ?? string.Empty;
        set => _headerImageUrl = value;
    }
    public string? RawHeaderImageUrl => _headerImageUrl;

    public string CapsuleImageUrl =>
        $"https://cdn.cloudflare.steamstatic.com/steam/apps/{AppId}/capsule_231x87.jpg";

    // Optional rich assets from prebuilt metadata chunks.
    public string? IconImageUrl { get; set; }
    public string? LibraryCapsule2xUrl { get; set; }
    public string? LibraryHero2xUrl { get; set; }
    public string? BackgroundRawImageUrl { get; set; }

    public string? PopularCoverImageUrl =>
        !string.IsNullOrWhiteSpace(LibraryCapsule2xUrl)
            ? LibraryCapsule2xUrl
            : !string.IsNullOrWhiteSpace(RawHeaderImageUrl)
                ? RawHeaderImageUrl
                : null;

    public bool HasPopularCover => !string.IsNullOrWhiteSpace(PopularCoverImageUrl);

    // Full raw metadata payload for future UI expansion without changing fetch layer.
    public string? RawMetadataJson { get; set; }
    public int RawFieldPathCount { get; set; }

    public string BackgroundImageUrl =>
        $"https://store.akamai.steamstatic.com/images/storepagebackground/app/{AppId}";
}
