namespace NexaPlay.Core.Models;

public sealed class NewGamesCatalogEntry
{
    public int AppId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public string[] Developers { get; init; } = [];
    public string[] Publishers { get; init; } = [];
    public string? Genre { get; init; }
    public string? ShortDescription { get; init; }
    public string? ReleaseDate { get; init; }
    public int PriceNormalized { get; init; }
    public string? PriceDisplay { get; init; }
    public bool Protection { get; init; }
    public string? HeaderImageUrl { get; init; }
    public string? IconImageUrl { get; init; }
    public string? LibraryCapsuleUrl { get; init; }
    public string? LibraryHero2xUrl { get; init; }
    public string? BackgroundRawImageUrl { get; init; }
}

public readonly record struct NewGamesRefreshResult(int Added, int Unavailable);
