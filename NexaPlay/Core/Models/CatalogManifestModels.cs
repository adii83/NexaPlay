namespace NexaPlay.Core.Models;

public sealed record CatalogManifestDocument(
    int Schema,
    string Revision,
    IReadOnlyDictionary<string, CatalogManifestEntry> Files);

public sealed record CatalogManifestEntry(
    string Name,
    string Url,
    long Size,
    string Sha256);

public readonly record struct CatalogFileState(long Size, string Sha256);
