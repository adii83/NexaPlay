namespace NexaPlay.Core.Models;

/// <summary>
/// Sparse catalog-level override from nexaplay_override.json.
/// Any non-null field will replace the corresponding value in GameEntry.
/// </summary>
public sealed class NexaPlayCatalogOverride
{
    public string? Title { get; init; }
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public IReadOnlyList<string>? Developers { get; init; }
    public IReadOnlyList<string>? Publishers { get; init; }
    public string? Genre { get; init; }
    public string? ShortDescription { get; init; }
    public string? ReleaseDate { get; init; }
    public int? PriceNormalized { get; init; }
    public string? PriceDisplay { get; init; }
    public bool? Protection { get; init; }
    public string? Header { get; init; }
    public string? Icon { get; init; }
    public string? LibraryCapsule2x { get; init; }
    public string? LibraryHero2x { get; init; }
    public string? BackgroundRaw { get; init; }
}

/// <summary>
/// Sparse detail-level override from nexaplay_override.json.
/// Any non-null field will replace the corresponding value in GameDetailEntry.
/// </summary>
public sealed class NexaPlayDetailOverride
{
    public string? ShortDescription { get; init; }
    public string? AboutTheGame { get; init; }
    public string? DetailedDescription { get; init; }
    public string? SupportedLanguages { get; init; }
    public string? Website { get; init; }
    public IReadOnlyList<string>? Developers { get; init; }
    public IReadOnlyList<string>? Publishers { get; init; }
    public string? ReleaseDate { get; init; }
    public IReadOnlyList<NexaPlayScreenshotOverride>? Screenshots { get; init; }
    public IReadOnlyList<NexaPlayMovieOverride>? Movies { get; init; }
    public string? BackgroundImage { get; init; }
    public string? PcRequirementsMinimum { get; init; }
    public string? PcRequirementsRecommended { get; init; }
    public string? MacRequirementsMinimum { get; init; }
    public string? MacRequirementsRecommended { get; init; }
    public string? LinuxRequirementsMinimum { get; init; }
    public string? LinuxRequirementsRecommended { get; init; }
    public IReadOnlyList<string>? Categories { get; init; }
    public string? SupportUrl { get; init; }
    public string? SupportEmail { get; init; }
    public string? LegalNotice { get; init; }
    public string? DrmNotice { get; init; }
    public string? StorePriceFinalFormatted { get; init; }
    public string? StorePriceCurrency { get; init; }
}

public sealed class NexaPlayScreenshotOverride
{
    public int Id { get; init; }
    public string Thumbnail { get; init; } = string.Empty;
    public string Full { get; init; } = string.Empty;
}

public sealed class NexaPlayMovieOverride
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Thumbnail { get; init; } = string.Empty;
    public string? HlsUrl { get; init; }
    public string? DashH264Url { get; init; }
    public bool IsHighlight { get; init; }
}
