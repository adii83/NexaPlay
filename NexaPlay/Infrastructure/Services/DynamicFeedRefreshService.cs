using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Models;
using System.Net;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public sealed class DynamicFeedRefreshService : IDynamicFeedRefreshService
{
    private readonly record struct FeedSpec(
        DynamicFeedKind Kind,
        DynamicFeedChanges Change,
        string Url,
        string FileName);

    private static readonly FeedSpec[] Feeds =
    [
        new(DynamicFeedKind.Popular, DynamicFeedChanges.Popular, AppConstants.PopularGamesUrl, "appid_populer.json"),
        new(DynamicFeedKind.NewFix, DynamicFeedChanges.NewFix, AppConstants.NewFixGamesUrl, AppConstants.NewFixGamesCacheFileName),
        new(DynamicFeedKind.FixGames, DynamicFeedChanges.FixGames, AppConstants.BypassGamesUrl, AppConstants.BypassGamesCacheFileName),
        new(DynamicFeedKind.SteamGames, DynamicFeedChanges.SteamGames, AppConstants.SteamGamesUrl, AppConstants.SteamGamesCacheFileName)
    ];

    private readonly IAppLogService _log;
    private readonly HttpClient _http;
    private readonly string _directory;
    private readonly string _etagDirectory;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _sessionChecked;

    public DynamicFeedRefreshService(IAppLogService log)
        : this(log, CreateHttpClient(), Path.Combine(AppConstants.AppDataPath, "runtime_catalog_sources"))
    {
    }

    internal DynamicFeedRefreshService(IAppLogService log, HttpClient http, string directory)
    {
        _log = log;
        _http = http;
        _directory = directory;
        _etagDirectory = Path.Combine(_directory, ".etags");
        Directory.CreateDirectory(_directory);
        Directory.CreateDirectory(_etagDirectory);
    }

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = AppConstants.HttpDefaultTimeout };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("NexaPlay/1.0");
        return http;
    }

    public string GetLocalPath(DynamicFeedKind feed) =>
        Path.Combine(_directory, Feeds.Single(item => item.Kind == feed).FileName);

    public async Task<DynamicFeedRefreshResult> RefreshAsync(bool force = false, CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            if (!force && _sessionChecked && AllLocalBodiesExist())
                return new DynamicFeedRefreshResult(DynamicFeedChanges.None, 0, 0, 0, Suppressed: true);

            var results = await Task.WhenAll(Feeds.Select(feed => RefreshOneAsync(feed, ct)));
            _sessionChecked = results.All(result => !result.Failed) && AllLocalBodiesExist();
            return new DynamicFeedRefreshResult(
                results.Aggregate(DynamicFeedChanges.None, (changes, result) => changes | result.Change),
                results.Length,
                results.Count(result => result.NotModified),
                results.Count(result => result.Failed));
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<(DynamicFeedChanges Change, bool NotModified, bool Failed)> RefreshOneAsync(
        FeedSpec feed,
        CancellationToken ct)
    {
        var localPath = Path.Combine(_directory, feed.FileName);
        var etagPath = Path.Combine(_etagDirectory, $"{feed.FileName}.etag");
        try
        {
            var localBody = TryReadText(localPath);
            var etag = TryReadText(etagPath);
            var response = await SendAsync(feed.Url, localBody is null ? null : etag, ct);
            try
            {
                if (response.StatusCode == HttpStatusCode.NotModified && localBody is null)
                {
                    response.Dispose();
                    response = await SendAsync(feed.Url, etag: null, ct);
                }

                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    if (localBody is null)
                        throw new InvalidDataException("Feed returned 304 without a local body.");
                    return (DynamicFeedChanges.None, true, false);
                }

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(ct);
                Validate(feed.Kind, json);

                var changed = !string.Equals(localBody, json, StringComparison.Ordinal);
                if (changed)
                    await WriteTextAtomicallyAsync(localPath, json, ct);

                var responseEtag = response.Headers.ETag?.ToString();
                if (!string.IsNullOrWhiteSpace(responseEtag) &&
                    !string.Equals(etag, responseEtag, StringComparison.Ordinal))
                {
                    await WriteTextAtomicallyAsync(etagPath, responseEtag, ct);
                }

                return (changed ? feed.Change : DynamicFeedChanges.None, false, false);
            }
            finally
            {
                response.Dispose();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Log("DynamicFeed", $"{feed.FileName} refresh failed: {ex.Message}");
            return (DynamicFeedChanges.None, false, true);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string url, string? etag, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(etag))
            request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private bool AllLocalBodiesExist() => Feeds.All(feed =>
        TryReadText(Path.Combine(_directory, feed.FileName)) is not null);

    private static void Validate(DynamicFeedKind kind, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (kind == DynamicFeedKind.Popular)
        {
            if (!ContainsPositiveAppId(root))
                throw new JsonException($"{kind} feed contains no usable AppID.");
            return;
        }

        var games = root.ValueKind == JsonValueKind.Array
            ? root
            : root.ValueKind == JsonValueKind.Object && root.TryGetProperty("games", out var wrapped)
                ? wrapped
                : default;
        var hasUsableEntry = games.ValueKind == JsonValueKind.Array && games.EnumerateArray().Any(item =>
            kind == DynamicFeedKind.NewFix && TryReadPositiveAppId(item) ||
            item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty("appid", out var appId) &&
            TryReadPositiveAppId(appId));
        if (!hasUsableEntry)
        {
            throw new JsonException("Game feed contains no usable entries.");
        }
    }

    private static bool ContainsPositiveAppId(JsonElement node)
    {
        if (TryReadPositiveAppId(node))
            return true;
        if (node.ValueKind == JsonValueKind.Array)
            return node.EnumerateArray().Any(ContainsPositiveAppId);
        if (node.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in node.EnumerateObject())
        {
            if (int.TryParse(property.Name, out var keyedAppId) && keyedAppId > 0)
                return true;
            if (property.NameEquals("appid") && TryReadPositiveAppId(property.Value))
                return true;
            if (ContainsPositiveAppId(property.Value))
                return true;
        }
        return false;
    }

    private static bool TryReadPositiveAppId(JsonElement node) =>
        node.ValueKind == JsonValueKind.Number
            ? node.TryGetInt32(out var number) && number > 0
            : node.ValueKind == JsonValueKind.String && int.TryParse(node.GetString(), out var text) && text > 0;

    private static string? TryReadText(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : null; }
        catch { return null; }
    }

    private static async Task WriteTextAtomicallyAsync(string path, string content, CancellationToken ct)
    {
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            await using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(content.AsMemory(), ct);
                await writer.FlushAsync(ct);
                await stream.FlushAsync(ct);
            }
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            try { File.Delete(temp); } catch { }
        }
    }
}
