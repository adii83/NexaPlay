using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Services;

/// <summary>One GitHub mapping, one in-flight refresh, bounded on-demand freshness.</summary>
public sealed class OnlineFixCatalogService : IOnlineFixCatalogService
{
    internal static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);
    internal static readonly TimeSpan FailureInterval = TimeSpan.FromMinutes(1);
    private const int MaxCatalogBytes = 4 * 1024 * 1024;
    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly string _cachePath;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyDictionary<int, OnlineFixEntry>? _entries;
    private string? _etag;
    private DateTimeOffset _nextCheck;
    private bool _cacheLoaded;
    private string? _lastError;

    public OnlineFixCatalogService(IAppLogService log)
        : this(log, new HttpClient { Timeout = TimeSpan.FromSeconds(15) },
            Path.Combine(AppConstants.AppDataPath, "online_fix_catalog.json"), TimeProvider.System) { }

    internal OnlineFixCatalogService(IAppLogService log, HttpClient http, string cachePath, TimeProvider clock)
    {
        _log = log;
        _http = http;
        _cachePath = cachePath;
        _clock = clock;
    }

    public async Task<OnlineFixEntry?> GetAsync(int appId, CancellationToken ct = default)
    {
        if (appId <= 0) throw new ArgumentOutOfRangeException(nameof(appId));
        await RefreshAsync(ct).ConfigureAwait(false);
        var snapshot = Volatile.Read(ref _entries);
        if (snapshot is null || _lastError is not null)
            throw new InvalidOperationException(_lastError ?? "Katalog Online-Fix belum dapat diambil. Periksa koneksi lalu coba lagi.");
        return snapshot.GetValueOrDefault(appId);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!_cacheLoaded)
            {
                _cacheLoaded = true;
                await LoadCacheAsync(ct).ConfigureAwait(false);
            }
            if (_clock.GetUtcNow() < _nextCheck) return;

            // Read branch content through GitHub's API rather than the raw-content CDN.
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{AppConstants.OnlineFixCatalogUrl}&nexaplay_check={Guid.NewGuid():N}");
            request.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            request.Headers.Accept.ParseAdd("application/vnd.github.raw+json");
            request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, MaxAge = TimeSpan.Zero };
            if (_entries is not null && !string.IsNullOrWhiteSpace(_etag))
                request.Headers.TryAddWithoutValidation("If-None-Match", _etag);

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotModified && _entries is not null)
                {
                    _nextCheck = _clock.GetUtcNow() + RefreshInterval;
                    _lastError = null;
                    _log.Log("OnlineFixCatalog", "online_fix.json unchanged (ETag 304)");
                    return;
                }
                if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
                {
                    _nextCheck = ReadBackoff(response, _clock.GetUtcNow());
                    _lastError = "GitHub membatasi akses katalog Online-Fix. Silakan coba lagi setelah waktu tunggu berakhir.";
                    _log.Log("OnlineFixCatalog", $"GitHub refused refresh; next attempt {_nextCheck:O}");
                    return;
                }
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength > MaxCatalogBytes)
                    throw new InvalidDataException("Katalog Online-Fix melebihi batas ukuran.");
                await using var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                using var body = new MemoryStream();
                var buffer = new byte[81920];
                int count;
                while ((count = await input.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0)
                {
                    if (body.Length + count > MaxCatalogBytes) throw new InvalidDataException("Katalog terlalu besar.");
                    body.Write(buffer, 0, count);
                }
                var json = System.Text.Encoding.UTF8.GetString(body.ToArray());
                var entries = Parse(json);
                var etag = response.Headers.ETag?.ToString();
                // Validation precedes publication. Cache failure must not discard valid network data.
                try
                {
                    await WriteCacheAsync(new CacheEnvelope(json, etag, AppConstants.OnlineFixCatalogUrl), timeout.Token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.Log("OnlineFixCatalog", $"Cache could not be persisted: {ex.Message}");
                }
                Volatile.Write(ref _entries, entries);
                _etag = etag;
                _nextCheck = _clock.GetUtcNow() + RefreshInterval;
                _lastError = null;
                _log.Log("OnlineFixCatalog", $"Validated {entries.Count} mappings from online_fix.json");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _nextCheck = _clock.GetUtcNow() + FailureInterval;
                _lastError = "Gagal memperbarui katalog Online-Fix. Periksa koneksi atau data sumber.";
                _log.Log("OnlineFixCatalog", $"Refresh failed; keeping last valid mapping: {ex.Message}");
            }
        }
        finally { _gate.Release(); }
    }

    internal static IReadOnlyDictionary<int, OnlineFixEntry> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException("online_fix.json harus berupa array.");
        var entries = new Dictionary<int, OnlineFixEntry>();
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("appid", out var id) || !id.TryGetInt32(out var appId) || appId <= 0 ||
                !row.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
                throw new JsonException("Mapping Online-Fix tidak valid.");
            var packages = new List<OnlineFixPackage>();
            var filenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files.EnumerateArray())
            {
                if (packages.Count >= 64) throw new JsonException("Terlalu banyak paket untuk satu AppID.");
                var filename = file.GetProperty("filename").GetString() ?? "";
                var driveId = file.GetProperty("gdrive_id").GetString() ?? "";
                var url = file.TryGetProperty("gdrive_url", out var link) ? link.GetString() : null;
                if (filename.Length is 0 or > 180 || filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    filename.Contains('/') || filename.Contains('\\') || filename.EndsWith('.') || filename.EndsWith(' ') ||
                    !Regex.IsMatch(driveId, @"\A[A-Za-z0-9_-]{10,200}\z") || !filenames.Add(filename))
                    throw new JsonException("Nama paket atau ID Google Drive tidak valid.");
                // Only the explicit mapping is used; no package hosts or fallback sources are inferred.
                url = string.IsNullOrWhiteSpace(url)
                    ? $"https://drive.google.com/uc?export=download&id={driveId}"
                    : url;
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsDriveDownloadUri(uri) ||
                    !GetQuery(uri).TryGetValue("id", out var urlId) || urlId != driveId)
                    throw new JsonException("URL Google Drive tidak cocok dengan gdrive_id.");
                packages.Add(new OnlineFixPackage(filename, driveId, uri.AbsoluteUri));
            }
            if (!entries.TryAdd(appId, new OnlineFixEntry(appId, packages.AsReadOnly())))
                throw new JsonException($"AppID Online-Fix duplikat: {appId}.");
        }
        return entries;
    }

    internal static bool IsDriveDownloadUri(Uri uri) => uri.Scheme == Uri.UriSchemeHttps &&
        uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) &&
        ((uri.Host.Equals("drive.google.com", StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath is "/uc" or "/download") ||
         (uri.Host.Equals("drive.usercontent.google.com", StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath == "/download"));

    internal static Dictionary<string, string> GetQuery(Uri uri) => uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(p => p.Split('=', 2)).GroupBy(p => Uri.UnescapeDataString(p[0]), StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => Uri.UnescapeDataString(g.Last().ElementAtOrDefault(1) ?? ""), StringComparer.Ordinal);

    private async Task LoadCacheAsync(CancellationToken ct)
    {
        try
        {
            if (!File.Exists(_cachePath) || new FileInfo(_cachePath).Length > MaxCatalogBytes * 8) return;
            var cached = JsonSerializer.Deserialize<CacheEnvelope>(await File.ReadAllTextAsync(_cachePath, ct).ConfigureAwait(false));
            if (cached is null) return;
            Volatile.Write(ref _entries, Parse(cached.Json));
            _etag = cached.Source == AppConstants.OnlineFixCatalogUrl ? cached.ETag : null;
        }
        catch (OperationCanceledException) { _cacheLoaded = false; throw; }
        catch (Exception ex) { _log.Log("OnlineFixCatalog", $"Ignoring invalid local catalog: {ex.Message}"); }
    }

    private async Task WriteCacheAsync(CacheEnvelope cache, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);
        var temporary = _cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(cache), ct).ConfigureAwait(false);
            File.Move(temporary, _cachePath, overwrite: true);
        }
        finally { try { File.Delete(temporary); } catch { } }
    }

    private static DateTimeOffset ReadBackoff(HttpResponseMessage response, DateTimeOffset now)
    {
        var next = now + FailureInterval;
        var retry = response.Headers.RetryAfter;
        if (retry?.Delta is { } delta && now + delta > next) next = now + delta;
        if (retry?.Date is { } date && date > next) next = date;
        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var reset) &&
            long.TryParse(reset.FirstOrDefault(), out var unix) && unix > 0 && unix < 253402300799)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(unix);
            if (time > next) next = time;
        }
        // Do not accept unreasonable remote delays beyond one day.
        return next > now.AddDays(1) ? now.AddDays(1) : next;
    }

    private sealed record CacheEnvelope(string Json, string? ETag, string? Source = null);
}
