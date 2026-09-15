using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Helpers;
using NexaPlay.Core.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexaPlay.Infrastructure.Services;

public sealed class CatalogUpdateService : ICatalogUpdateService
{
    private readonly ICatalogBootstrapService _bootstrap;
    private readonly ICatalogRepository _repository;
    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly Uri _manifestUri;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CatalogUpdateService(
        ICatalogBootstrapService bootstrap,
        ICatalogRepository repository,
        IAppLogService log)
        : this(bootstrap, repository, log, CreateHttpClient(), new Uri(AppConstants.CatalogManifestUrl))
    {
    }

    public CatalogUpdateService(
        ICatalogBootstrapService bootstrap,
        ICatalogRepository repository,
        IAppLogService log,
        HttpClient http,
        Uri manifestUri)
    {
        _bootstrap = bootstrap;
        _repository = repository;
        _log = log;
        _http = http;
        _manifestUri = manifestUri;
    }

    public async Task<CatalogUpdateResult> CheckAndApplyPatchesAsync(bool force = false, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        var initialVersion = 0;
        var currentVersion = 0;
        var applied = 0;
        try
        {
            await _bootstrap.EnsureReadyAsync(ct);
            var localVersion = await _repository.GetVersionAsync(ct);
            initialVersion = localVersion;
            currentVersion = localVersion;

            using var response = await _http.GetAsync(_manifestUri, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var manifest = JsonSerializer.Deserialize<CatalogManifest>(json)
                ?? throw new JsonException("Catalog manifest is empty.");
            ValidateManifest(manifest);

            if (localVersion == manifest.LatestVersion)
                return new(CatalogUpdateStatus.NoChange, localVersion, localVersion, 0, "Catalog already current.");

            if (localVersion > manifest.LatestVersion)
            {
                _log.Log("Catalog", $"Remote manifest behind local catalog. local={localVersion}, remote={manifest.LatestVersion}");
                return new(CatalogUpdateStatus.LocalAhead, localVersion, localVersion, 0, "Local catalog is newer than remote manifest.");
            }

            if (localVersion < manifest.MinimumSupportedVersion)
            {
                _log.Log("Catalog", $"Patch baseline unsupported. local={localVersion}, minimum={manifest.MinimumSupportedVersion}");
                return new(CatalogUpdateStatus.UnsupportedBaseline, localVersion, localVersion, 0,
                    "Catalog baseline is too old; install a newer NexaPlay release.");
            }

            var entries = ResolvePatchChain(manifest, localVersion);
            foreach (var entry in entries)
            {
                var bytes = await DownloadPatchAsync(entry, ct);
                var patch = ParsePatch(bytes);
                if (patch.FromVersion != currentVersion || patch.ToVersion != entry.Version)
                    throw new InvalidDataException($"Patch chain mismatch at version {entry.Version}.");

                await _repository.ApplyPatchAsync(patch, ct);
                currentVersion = patch.ToVersion;
                applied++;
            }

            _log.Log("Catalog", $"Applied {applied} catalog patch(es). version={currentVersion}");
            return new(CatalogUpdateStatus.Applied, localVersion, currentVersion, applied, "Catalog patches applied.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            try { currentVersion = await _repository.GetVersionAsync(CancellationToken.None); } catch { }
            _log.Log("Catalog", $"Catalog update failed; local database retained: {ex.Message}");
            return new(CatalogUpdateStatus.Failed, initialVersion, currentVersion, applied, ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = AppConstants.HttpDefaultTimeout };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NexaPlay", "1.0"));
        return client;
    }

    private static void ValidateManifest(CatalogManifest manifest)
    {
        if (manifest.SchemaVersion != 1 || manifest.LatestVersion <= 0 || manifest.MinimumSupportedVersion <= 0 ||
            manifest.MinimumSupportedVersion > manifest.LatestVersion)
        {
            throw new InvalidDataException("Catalog manifest version fields are invalid.");
        }

        var versions = new HashSet<int>();
        foreach (var patch in manifest.Patches)
        {
            if (patch.Version <= 0 || patch.Size < 0 || !IsSha256(patch.Sha256) ||
                string.IsNullOrWhiteSpace(patch.File) || !versions.Add(patch.Version))
            {
                throw new InvalidDataException("Catalog manifest contains an invalid or duplicate patch entry.");
            }
        }
    }

    private static IReadOnlyList<CatalogPatchEntry> ResolvePatchChain(CatalogManifest manifest, int localVersion)
    {
        var byVersion = manifest.Patches.ToDictionary(patch => patch.Version);
        var chain = new List<CatalogPatchEntry>();
        for (var version = localVersion + 1; version <= manifest.LatestVersion; version++)
        {
            if (!byVersion.TryGetValue(version, out var patch))
                throw new InvalidDataException($"Catalog patch {version} is missing.");
            chain.Add(patch);
        }
        return chain;
    }

    private async Task<byte[]> DownloadPatchAsync(CatalogPatchEntry entry, CancellationToken ct)
    {
        if (!Uri.TryCreate(entry.File, UriKind.Relative, out var relative) || entry.File.Contains("..", StringComparison.Ordinal))
            throw new InvalidDataException($"Patch path is invalid: {entry.File}");

        var patchUri = new Uri(_manifestUri, relative);
        if (!string.Equals(patchUri.Scheme, _manifestUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(patchUri.Host, _manifestUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Patch URL left the catalog origin.");
        }

        using var response = await _http.GetAsync(patchUri, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.LongLength != entry.Size)
            throw new InvalidDataException($"Patch {entry.Version} size mismatch.");

        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(sha256, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Patch {entry.Version} SHA-256 mismatch.");
        return bytes;
    }

    private static CatalogPatch ParsePatch(byte[] bytes)
    {
        var document = JsonSerializer.Deserialize<CatalogPatchDocument>(bytes)
            ?? throw new JsonException("Catalog patch is empty.");
        if (document.SchemaVersion != 1 || document.FromVersion <= 0 || document.ToVersion != document.FromVersion + 1)
            throw new InvalidDataException("Catalog patch version fields are invalid.");

        var upsert = new List<CatalogPatchGame>(document.Upsert.Count);
        var appIds = new HashSet<int>();
        foreach (var row in document.Upsert)
        {
            if (row.AppId <= 0 || string.IsNullOrWhiteSpace(row.Name) || string.IsNullOrWhiteSpace(row.NormalizedName) ||
                row.Genre.ValueKind != JsonValueKind.Array || row.PcRequirements.ValueKind != JsonValueKind.Object ||
                row.Tier is not ("standard" or "premium") || !appIds.Add(row.AppId))
            {
                throw new InvalidDataException("Catalog patch contains an invalid or duplicate upsert row.");
            }

            upsert.Add(new CatalogPatchGame(
                row.AppId,
                row.Name,
                row.NormalizedName,
                row.LibraryCapsule,
                row.Genre.GetRawText(),
                row.PriceNormalized,
                row.PriceDisplay ?? "Free",
                row.Protection,
                row.Tier,
                row.PcRequirements.GetRawText()));
        }

        if (document.Delete.Any(appId => appId <= 0) || document.Delete.Count != document.Delete.Distinct().Count() ||
            document.Delete.Any(appIds.Contains))
        {
            throw new InvalidDataException("Catalog patch delete rows are invalid or conflict with upserts.");
        }

        return new CatalogPatch(document.SchemaVersion, document.FromVersion, document.ToVersion, upsert, document.Delete);
    }

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private sealed class CatalogManifest
    {
        [JsonPropertyName("schema_version")] public int SchemaVersion { get; init; }
        [JsonPropertyName("latest_version")] public int LatestVersion { get; init; }
        [JsonPropertyName("minimum_supported_version")] public int MinimumSupportedVersion { get; init; }
        [JsonPropertyName("patches")] public List<CatalogPatchEntry> Patches { get; init; } = [];
    }

    private sealed class CatalogPatchEntry
    {
        [JsonPropertyName("version")] public int Version { get; init; }
        [JsonPropertyName("file")] public string File { get; init; } = string.Empty;
        [JsonPropertyName("size")] public long Size { get; init; }
        [JsonPropertyName("sha256")] public string Sha256 { get; init; } = string.Empty;
    }

    private sealed class CatalogPatchDocument
    {
        [JsonPropertyName("schema_version")] public int SchemaVersion { get; init; }
        [JsonPropertyName("from_version")] public int FromVersion { get; init; }
        [JsonPropertyName("to_version")] public int ToVersion { get; init; }
        [JsonPropertyName("upsert")] public List<CatalogPatchRow> Upsert { get; init; } = [];
        [JsonPropertyName("delete")] public List<int> Delete { get; init; } = [];
    }

    private sealed class CatalogPatchRow
    {
        [JsonPropertyName("appid")] public int AppId { get; init; }
        [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
        [JsonPropertyName("normalized_name")] public string NormalizedName { get; init; } = string.Empty;
        [JsonPropertyName("library_capsule")] public string? LibraryCapsule { get; init; }
        [JsonPropertyName("genre")] public JsonElement Genre { get; init; }
        [JsonPropertyName("price_normalized")] public int PriceNormalized { get; init; }
        [JsonPropertyName("price_display")] public string? PriceDisplay { get; init; }
        [JsonPropertyName("protection")] public bool Protection { get; init; }
        [JsonPropertyName("tier")] public string Tier { get; init; } = string.Empty;
        [JsonPropertyName("pc_requirements")] public JsonElement PcRequirements { get; init; }
    }
}
