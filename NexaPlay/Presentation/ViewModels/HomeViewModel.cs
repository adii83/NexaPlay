using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IBypassGamesDataService _fixData;
    private readonly IAddGameService _addGame;
    private readonly IMetadataService _metadata;
    private readonly IGameCoverIndexService _gameCoverIndex;
    private readonly ICoverImageCacheService _coverImageCache;
    private readonly ISteamStoreService _storeService;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly IAppLogService _log;

    [ObservableProperty] public partial int TotalFixes { get; set; }
    [ObservableProperty] public partial int LibraryCount { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial IReadOnlyList<FixEntry> RecentFixes { get; set; }
    [ObservableProperty] public partial ObservableCollection<GameEntry> PopularGames { get; set; }
    [ObservableProperty] public partial FixEntry? HeroGame { get; set; }

    private IReadOnlyList<int> _allPopularAppIds = Array.Empty<int>();
    private readonly List<GameEntry> _loadedPopularCache = new();
    private int _currentPopularPage = 0;
    private int _popularColumns = 5;
    private int PopularGamesPageSize => _popularColumns * 8;
    private const int PopularFetchConcurrency = 6;
    private readonly SemaphoreSlim _apiEnrichLock = new(1, 1);
    private Dictionary<int, GameEntry>? _gamesParityCatalog;
    private readonly Dictionary<int, GameEntry> _popularCardPrefetchCache = new();
    
    private static readonly SemaphoreSlim _prefetchGate = new SemaphoreSlim(4, 4);
    private CancellationTokenSource? _prefetchCts;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private Task? _loadTask;

    public HomeViewModel(
        IBypassGamesDataService fixData,
        IAddGameService addGame,
        IMetadataService metadata,
        IGameCoverIndexService gameCoverIndex,
        ICoverImageCacheService coverImageCache,
        ISteamStoreService storeService,
        INexaPlayOverrideService nexaPlayOverride,
        IAppLogService log)
    {
        _fixData = fixData;
        _addGame = addGame;
        _metadata = metadata;
        _gameCoverIndex = gameCoverIndex;
        _coverImageCache = coverImageCache;
        _storeService = storeService;
        _nexaPlayOverride = nexaPlayOverride;
        _log = log;

        // Default values for partial properties
        IsLoading    = true;
        RecentFixes  = Array.Empty<FixEntry>();
        PopularGames = new ObservableCollection<GameEntry>();
    }

    public async Task LoadAsync()
    {
        Task pending;
        await _loadGate.WaitAsync();
        try
        {
            _loadTask ??= LoadCoreAsync();
            pending = _loadTask;
        }
        finally
        {
            _loadGate.Release();
        }

        try
        {
            await pending;
        }
        finally
        {
            await _loadGate.WaitAsync();
            try
            {
                if (ReferenceEquals(_loadTask, pending))
                {
                    _loadTask = null;
                }
            }
            finally
            {
                _loadGate.Release();
            }
        }
    }

    private async Task LoadCoreAsync()
    {
        IsLoading = true;
        try
        {
            var fixes = await _fixData.GetAllFixesAsync();
            TotalFixes   = fixes.Count;
            LibraryCount = _addGame.ListLibraryGames().Count;

            var newFixAppIds = await _metadata.GetNewFixAppIdsAsync();
            if (newFixAppIds.Count > 0)
            {
                var bypassLookup = await BuildBypassLookupAsync();
                var recent = new List<FixEntry>();
                foreach (var appId in newFixAppIds.Take(12))
                {
                    var meta = await _metadata.GetMetadataAsync(appId);
                    bypassLookup.TryGetValue(appId, out var bypassEntry);
                    recent.Add(new FixEntry
                    {
                        AppId = appId,
                        Title = bypassEntry?.Title ?? meta?.Name ?? $"App {appId}",
                        Publisher = bypassEntry?.Publisher ?? meta?.PublisherDisplay ?? string.Empty,
                        Category = bypassEntry?.Category ?? GameCategory.Other,
                        PosterUrl = bypassEntry?.PosterUrl ?? meta?.LibraryHero2xUrl ?? meta?.LibraryCapsuleUrl ?? meta?.RawHeaderImageUrl,
                        IsPremium = bypassEntry?.IsPremium ?? meta?.IsPremium ?? false,
                        Username = bypassEntry?.Username,
                        Password = bypassEntry?.Password,
                        AktivasiOffline = bypassEntry?.AktivasiOffline ?? false,
                        DapatkanKode = bypassEntry?.DapatkanKode ?? false,
                        ExeHint = bypassEntry?.ExeHint,
                        UseShortcut = bypassEntry?.UseShortcut ?? false,
                        Files = bypassEntry?.Files ?? Array.Empty<FixFile>()
                    });
                }
                recent = await EnrichRecentFixesHeroCoverAsync(recent);
                HeroGame = recent.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.PosterUrl)) ?? recent.FirstOrDefault();
                RecentFixes = recent;
            }
            else
            {
                RecentFixes = Array.Empty<FixEntry>();
                HeroGame = null;
                _log.Log("Home", "new_fix_games appid list is empty.");
            }

            await LoadPopularGamesInBackgroundAsync();
        }
        finally 
        { 
            IsLoading = false; 
        }
    }

    private async Task<Dictionary<int, FixEntry>> BuildBypassLookupAsync(CancellationToken ct = default)
    {
        var lookup = new Dictionary<int, FixEntry>();

        foreach (var entry in await _fixData.GetNewFixesAsync(ct))
        {
            lookup[entry.AppId] = entry;
        }

        foreach (var entry in await _fixData.GetAllFixesAsync(ct))
        {
            lookup[entry.AppId] = entry;
        }

        foreach (var entry in await _fixData.GetSteamGamesAsync(ct))
        {
            lookup[entry.AppId] = entry;
        }

        return lookup;
    }

    private async Task LoadPopularGamesInBackgroundAsync()
    {
        // Prevent resetting the list and scroll state if already loaded (e.g., returning from GameDetail)
        if (_allPopularAppIds.Count > 0 && PopularGames.Count > 0)
        {
            return;
        }

        try
        {
            _allPopularAppIds = await _metadata.GetPopularAppIdsAsync();
            _currentPopularPage = 0;
            _log.Log("Home", $"Popular app ids loaded: {_allPopularAppIds.Count}");

            var popularList = await LoadNextPopularPageAsync();
            popularList = await PreparePopularCardBatchAsync(popularList);

            _loadedPopularCache.Clear();
            _loadedPopularCache.AddRange(popularList);
            PopularGames = new ObservableCollection<GameEntry>(popularList.Take(PopularGamesPageSize));
            _log.Log("Home", $"Popular games rendered: {PopularGames.Count}");

            if (popularList.Count > 0 && popularList.All(g => !g.HasPopularCover))
            {
                _log.Log("Home", "No library_capsule_2x/header cover in current cache. Triggering background metadata refresh for Home.");
                _ = RefreshPopularCoversInBackgroundAsync();
            }

            _ = PreFetchNextPopularGamesBackgroundAsync();
        }
        catch (Exception ex)
        {
            _log.Log("Home", $"Popular games load failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadMorePopularGamesAsync()
    {
        if (_allPopularAppIds.Count == 0 || _currentPopularPage >= _allPopularAppIds.Count) return;

        var targetRows = 4;
        var targetCount = _popularColumns * targetRows;
        var newGames = await LoadNextPopularPageAsync(targetCount);

        foreach(var g in newGames)
        {
            _loadedPopularCache.Add(g);
            PopularGames.Add(g);
        }

        await EnsurePopularFilledRowsAsync();
        _ = PreFetchNextPopularGamesBackgroundAsync();
    }

    private async Task PreFetchNextPopularGamesBackgroundAsync()
    {
        _prefetchCts?.Cancel();
        _prefetchCts?.Dispose();
        _prefetchCts = new CancellationTokenSource();
        var ct = _prefetchCts.Token;

        try
        {
            // Prefetch only the next load-more batch so the next reveal already has covers ready.
            var targetCount = _popularColumns * 4;
            var nextAppIds = _allPopularAppIds.Skip(_currentPopularPage).Take(targetCount).ToList();
            if (nextAppIds.Count == 0) return;

            _log.Log("Home", $"Pre-fetching next {nextAppIds.Count} popular games in background");
            
            var tasks = nextAppIds.Select(async appId =>
            {
                await _prefetchGate.WaitAsync(ct);
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var game = await GetPrefetchedPopularCardAsync(appId, ct);
                    if (game is not null)
                    {
                        lock (_popularCardPrefetchCache)
                        {
                            _popularCardPrefetchCache[appId] = game;
                        }
                    }
                }
                catch { }
                finally { _prefetchGate.Release(); }
            });
            await Task.WhenAll(tasks);
        }
        catch { }
    }

    public async Task UpdatePopularLayoutAsync(int columns)
    {
        columns = Math.Clamp(columns, 3, 6);
        _popularColumns = columns;
        if (_loadedPopularCache.Count == 0)
            return;

        // Maintain user's scroll depth by keeping the maximum of currently displayed items or 8 full rows
        var baseItemCount = Math.Max(PopularGames.Count, _popularColumns * 8);
        var neededRows = (int)Math.Ceiling((double)baseItemCount / _popularColumns);
        var needed = neededRows * _popularColumns;
        while (_loadedPopularCache.Count < needed && _currentPopularPage < _allPopularAppIds.Count)
        {
            var more = await LoadNextPopularPageAsync(_popularColumns * 2);
            if (more.Count == 0)
                break;
            _loadedPopularCache.AddRange(more);
            _ = EnrichPopularCoversFromApiAsync(more);
        }

        var targetCount = Math.Min(needed, _loadedPopularCache.Count);
        if (PopularGames.Count > targetCount)
        {
            for (var i = PopularGames.Count - 1; i >= targetCount; i--)
            {
                PopularGames.RemoveAt(i);
            }
        }
        else if (PopularGames.Count < targetCount)
        {
            for (var i = PopularGames.Count; i < targetCount; i++)
            {
                PopularGames.Add(_loadedPopularCache[i]);
            }
        }

        await EnsurePopularFilledRowsAsync();
    }

    private async Task EnsurePopularFilledRowsAsync()
    {
        if (_popularColumns <= 0)
            return;

        var remainder = PopularGames.Count % _popularColumns;
        if (remainder == 0)
            return;

        var required = _popularColumns - remainder;
        while (required > 0 && _currentPopularPage < _allPopularAppIds.Count)
        {
            var batch = await LoadNextPopularPageAsync(required);
            if (batch.Count == 0)
                break;

            foreach (var g in batch)
            {
                _loadedPopularCache.Add(g);
                PopularGames.Add(g);
            }

            required -= batch.Count;
            _ = EnrichPopularCoversFromApiAsync(batch);
        }
    }

    private async Task<List<GameEntry>> LoadNextPopularPageAsync(int targetCount = -1)
    {
        var requestSize = targetCount > 0 ? targetCount : PopularGamesPageSize;
        var finalResults = new List<GameEntry>(requestSize);
        var catalog = await GetGamesParityCatalogAsync();

        // Keep fetching until we fulfill the requested quota of VALID (non-null) items, or we run out of catalog IDs
        while (finalResults.Count < requestSize && _currentPopularPage < _allPopularAppIds.Count)
        {
            var needed = requestSize - finalResults.Count;
            var pageAppIds = new List<int>(needed);
            
            while (pageAppIds.Count < needed && _currentPopularPage < _allPopularAppIds.Count)
            {
                pageAppIds.Add(_allPopularAppIds[_currentPopularPage++]);
            }

            if (pageAppIds.Count == 0)
                break;

            var results = new GameEntry?[pageAppIds.Count];
            using var gate = new SemaphoreSlim(PopularFetchConcurrency, PopularFetchConcurrency);

            var tasks = pageAppIds.Select(async (appId, idx) =>
            {
                await gate.WaitAsync();
                try
                {
                    GameEntry? prefetched = null;
                    lock (_popularCardPrefetchCache)
                    {
                        if (_popularCardPrefetchCache.TryGetValue(appId, out var cached))
                        {
                            prefetched = CloneGameEntry(cached);
                            _popularCardPrefetchCache.Remove(appId);
                        }
                    }

                    if (prefetched is not null)
                    {
                        results[idx] = prefetched;
                        return;
                    }

                    if (catalog.TryGetValue(appId, out var snapshotGame))
                    {
                        results[idx] = CloneGameEntry(snapshotGame);
                        return;
                    }

                    results[idx] = await GetClonedMetadataAsync(appId);
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);

            foreach (var g in results)
            {
                if (g is not null)
                {
                    finalResults.Add(g);
                }
            }
        }

        return finalResults;
    }

    private async Task RefreshPopularCoversInBackgroundAsync()
    {
        try
        {
            await _metadata.RefreshAsync(forceDownload: false);
            _gamesParityCatalog = null;
            var catalog = await GetGamesParityCatalogAsync();
            var refreshed = new List<GameEntry>(PopularGamesPageSize);
            foreach (var appId in _allPopularAppIds.Take(PopularGamesPageSize))
            {
                catalog.TryGetValue(appId, out var metadata);
                metadata ??= await GetClonedMetadataAsync(appId);
                if (metadata is not null)
                    refreshed.Add(metadata);
            }

            if (refreshed.Count == 0)
                return;

            PopularGames.Clear();
            foreach (var game in refreshed)
            {
                PopularGames.Add(game);
            }
            _log.Log("Home", $"Popular games refreshed after metadata sync: {PopularGames.Count}");
        }
        catch (Exception ex)
        {
            _log.Log("Home", $"Background popular cover refresh failed: {ex.Message}");
        }
    }

    private async Task<Dictionary<int, GameEntry>> GetGamesParityCatalogAsync(CancellationToken ct = default)
    {
        if (_gamesParityCatalog is not null && _gamesParityCatalog.Count > 0)
        {
            return _gamesParityCatalog;
        }

        var snapshot = await _metadata.GetCatalogSnapshotAsync(ct);
        _gamesParityCatalog = snapshot.ToDictionary(x => x.AppId, CloneGameEntry);
        return _gamesParityCatalog;
    }

    private async Task<GameEntry?> GetClonedMetadataAsync(int appId, CancellationToken ct = default)
    {
        var metadata = await _metadata.GetMetadataAsync(appId, ct);
        return metadata is null ? null : CloneGameEntry(metadata);
    }

    private static GameEntry CloneGameEntry(GameEntry source)
    {
        return new GameEntry
        {
            AppId = source.AppId,
            Name = source.Name,
            Developer = source.Developer,
            Publisher = source.Publisher,
            Developers = source.Developers,
            Publishers = source.Publishers,
            Genre = source.Genre,
            ShortDescription = source.ShortDescription,
            ReleaseDate = source.ReleaseDate,
            PriceNormalized = source.PriceNormalized,
            PriceDisplay = source.PriceDisplay,
            Protection = source.Protection,
            HeaderImageUrl = source.HeaderImageUrl,
            IconImageUrl = source.IconImageUrl,
            LibraryCapsuleUrl = source.LibraryCapsuleUrl,
            LibraryHero2xUrl = source.LibraryHero2xUrl,
            BackgroundRawImageUrl = source.BackgroundRawImageUrl,
            RawMetadataJson = source.RawMetadataJson,
            RawFieldPathCount = source.RawFieldPathCount
        };
    }

    private async Task<List<GameEntry>> PreparePopularCardBatchAsync(IReadOnlyList<GameEntry> source, CancellationToken ct = default)
    {
        if (source.Count == 0)
            return new List<GameEntry>();

        var results = new GameEntry?[source.Count];
        using var gate = new SemaphoreSlim(4, 4);
        var tasks = source.Select(async (game, idx) =>
        {
            await gate.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                results[idx] = await BuildPopularCardWithBestCoverAsync(game, ct);
            }
            catch (OperationCanceledException)
            {
                results[idx] = CloneGameEntry(game);
            }
            catch
            {
                results[idx] = CloneGameEntry(game);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.Where(x => x is not null).Select(x => x!).ToList();
    }

    private async Task<GameEntry?> GetPrefetchedPopularCardAsync(int appId, CancellationToken ct = default)
    {
        var catalog = await GetGamesParityCatalogAsync(ct);
        GameEntry? game = null;
        if (catalog.TryGetValue(appId, out var snapshotGame))
        {
            game = CloneGameEntry(snapshotGame);
        }
        else
        {
            game = await GetClonedMetadataAsync(appId, ct);
        }

        if (game is null)
            return null;

        return await BuildPopularCardWithBestCoverAsync(game, ct);
    }

    private async Task<GameEntry> BuildPopularCardWithBestCoverAsync(GameEntry game, CancellationToken ct = default)
    {
        var clone = CloneGameEntry(game);
        var overrideCover = (await _nexaPlayOverride.GetCatalogOverrideAsync(clone.AppId, ct))?.LibraryCapsule;
        var indexedCover = await _gameCoverIndex.GetLibraryCapsuleAsync(clone.AppId, ct);

        var preferredCover = FirstNonEmpty(
            overrideCover,
            indexedCover,
            clone.LibraryCapsuleUrl,
            clone.RawHeaderImageUrl,
            null);

        if (!string.IsNullOrWhiteSpace(preferredCover))
        {
            clone.LibraryCapsuleUrl = await _coverImageCache.GetCachedOrFetchAsync(clone.AppId, preferredCover, ct)
                ?? preferredCover;
        }

        return clone;
    }

    private async Task EnrichPopularCoversFromApiAsync(IReadOnlyList<GameEntry>? targetGames = null)
    {
        var scope = (targetGames is { Count: > 0 } ? targetGames : PopularGames.ToList());
        if (scope.Count == 0)
            return;

        if (!await _apiEnrichLock.WaitAsync(0))
            return;

        try
        {
            var candidates = scope
                .Select((game, index) => new { game, index })
                .ToList();

            if (candidates.Count == 0)
                return;

            using var gate = new SemaphoreSlim(4, 4);
            var updateTasks = candidates.Select(async c =>
            {
                await gate.WaitAsync();
                try
                {
                    var overrideCover = (await _nexaPlayOverride.GetCatalogOverrideAsync(c.game.AppId))?.LibraryCapsule;
                    var indexedCover = await _gameCoverIndex.GetLibraryCapsuleAsync(c.game.AppId);

                    var preferredCover = !string.IsNullOrWhiteSpace(overrideCover) ? overrideCover 
                        : indexedCover;
                    if (string.IsNullOrWhiteSpace(preferredCover))
                        return;

                    preferredCover = await _coverImageCache.GetCachedOrFetchAsync(c.game.AppId, preferredCover)
                        ?? preferredCover;

                    var current = PopularGames.FirstOrDefault(g => g.AppId == c.game.AppId);
                    if (current is null)
                        return;

                    if (string.Equals(current.LibraryCapsuleUrl, preferredCover, StringComparison.OrdinalIgnoreCase))
                        return;

                    var idx = PopularGames.IndexOf(current);
                    if (idx < 0)
                        return;

                    PopularGames[idx] = new GameEntry
                    {
                        AppId = current.AppId,
                        Name = current.Name,
                        Developer = current.Developer,
                        Publisher = current.Publisher,
                        Developers = current.Developers,
                        Publishers = current.Publishers,
                        Genre = current.Genre,
                        ShortDescription = current.ShortDescription,
                        ReleaseDate = current.ReleaseDate,
                        PriceNormalized = current.PriceNormalized,
                        PriceDisplay = current.PriceDisplay,
                        Protection = current.Protection,
                        HeaderImageUrl = current.HeaderImageUrl,
                        IconImageUrl = current.IconImageUrl,
                        LibraryCapsuleUrl = preferredCover,
                        LibraryHero2xUrl = current.LibraryHero2xUrl,
                        BackgroundRawImageUrl = current.BackgroundRawImageUrl,
                        RawMetadataJson = current.RawMetadataJson,
                        RawFieldPathCount = current.RawFieldPathCount
                    };
                }
                catch (Exception ex)
                {
                    _log.Log("Home", $"API cover enrichment failed appId={c.game.AppId}: {ex.Message}");
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(updateTasks);
            _log.Log("Home", $"API cover enrichment done. candidates={candidates.Count}, current={PopularGames.Count}");
        }
        catch (Exception ex)
        {
            _log.Log("Home", $"API cover enrichment failed: {ex.Message}");
        }
        finally
        {
            _apiEnrichLock.Release();
        }
    }

    private static string? ReadFirstAssetUrl(string? rawMetadataJson, string assetKey)
    {
        if (string.IsNullOrWhiteSpace(rawMetadataJson))
            return null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawMetadataJson);
            System.Text.Json.JsonElement node;

            if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                assets.TryGetProperty(assetKey, out var nestedNode))
            {
                node = nestedNode;
            }
            else if (doc.RootElement.TryGetProperty(assetKey, out var rootNode))
            {
                node = rootNode;
            }
            else
            {
                return null;
            }

            if (node.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                {
                    if (item.ValueKind == System.Text.Json.JsonValueKind.Object &&
                        item.TryGetProperty("url", out var url) &&
                        url.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return url.GetString();
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<List<FixEntry>> EnrichRecentFixesHeroCoverAsync(List<FixEntry> recent)
    {
        if (recent.Count == 0)
            return recent;

        var results = new FixEntry[recent.Count];
        using var gate = new SemaphoreSlim(4, 4);
        var tasks = recent.Select(async (fix, idx) =>
        {
            await gate.WaitAsync();
            try
            {
                var heroCover = fix.PosterUrl;

                var catalogOv = await _nexaPlayOverride.GetCatalogOverrideAsync(fix.AppId);
                var hasHeroOverride = catalogOv?.LibraryHero2x is not null;
                var hasCapsuleOverride = catalogOv?.LibraryCapsule is not null;

                if (hasHeroOverride)
                {
                    heroCover = catalogOv!.LibraryHero2x!;
                }
                else
                {
                    var detail = await _storeService.GetDetailAsync(fix.AppId);
                    if (!hasCapsuleOverride && !string.IsNullOrWhiteSpace(detail?.LibraryCapsuleUrl))
                    {
                        heroCover = detail.LibraryCapsuleUrl;
                    }
                    if (!string.IsNullOrWhiteSpace(detail?.RawMetadataJson))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(detail.RawMetadataJson);
                            if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                                assets.TryGetProperty("library_hero_2x", out var heroArr) &&
                                heroArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var item in heroArr.EnumerateArray())
                                {
                                    if (item.TryGetProperty("url", out var urlProp) &&
                                        urlProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        var heroUrl = urlProp.GetString();
                                        if (!string.IsNullOrWhiteSpace(heroUrl))
                                        {
                                            heroCover = heroUrl;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                results[idx] = new FixEntry
                {
                    AppId = fix.AppId,
                    Title = fix.Title,
                    Publisher = fix.Publisher,
                    Category = fix.Category,
                    PosterUrl = heroCover,
                    Password = fix.Password,
                    IsPremium = fix.IsPremium,
                    ExeHint = fix.ExeHint,
                    UseShortcut = fix.UseShortcut,
                    Files = fix.Files
                };
            }
            catch
            {
                results[idx] = fix;
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }
}
