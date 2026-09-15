namespace NexaPlay.Core.Models;

public sealed record CatalogQuery(
    string Search,
    bool? Premium,
    bool? Protection,
    IReadOnlyList<string> Genres,
    bool ApplyDefaultPriceFloor,
    int Offset,
    int Limit,
    int OrderSeed);

public sealed record CatalogPage(IReadOnlyList<GameEntry> Items, int TotalCount);

public sealed record CatalogRuntimeOverride(
    string? Name = null,
    string? NormalizedName = null,
    string? LibraryCapsule = null,
    string? GenreJson = null,
    int? PriceNormalized = null,
    string? PriceDisplay = null,
    bool? Protection = null,
    string? Tier = null);

public sealed record CatalogPatchGame(
    int AppId,
    string Name,
    string NormalizedName,
    string? LibraryCapsule,
    string GenreJson,
    int PriceNormalized,
    string PriceDisplay,
    bool Protection,
    string Tier,
    string PcRequirementsJson);

public sealed record CatalogPatch(
    int SchemaVersion,
    int FromVersion,
    int ToVersion,
    IReadOnlyList<CatalogPatchGame> Upsert,
    IReadOnlyList<int> Delete);

public enum CatalogUpdateStatus
{
    NoChange,
    Applied,
    LocalAhead,
    UnsupportedBaseline,
    Failed
}

public sealed record CatalogUpdateResult(
    CatalogUpdateStatus Status,
    int PreviousVersion,
    int CurrentVersion,
    int AppliedPatches,
    string Message)
{
    public bool Changed => CurrentVersion != PreviousVersion;
}
