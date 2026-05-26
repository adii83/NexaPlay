using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using NexaPlay.Core.Helpers;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class GamesViewModel : ObservableObject
{
    private readonly IMetadataService _metadata;
    private readonly ISteamStoreService _storeService;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private const int RowsPerPage = 10;

    [ObservableProperty] public partial string SearchQuery { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial ObservableCollection<FixEntry> Games { get; set; }
    [ObservableProperty] public partial int TotalCount { get; set; }
    [ObservableProperty] public partial bool IsFilterOpen { get; set; }
    [ObservableProperty] public partial bool FilterStandard { get; set; }
    [ObservableProperty] public partial bool FilterPremium { get; set; }
    [ObservableProperty] public partial bool FilterDenuvo { get; set; }
    [ObservableProperty] public partial bool FilterNonDenuvo { get; set; }
    [ObservableProperty] public partial string CurrentPageLabel { get; set; }
    [ObservableProperty] public partial bool CanGoNext { get; set; }
    [ObservableProperty] public partial bool CanGoPrevious { get; set; }
    [ObservableProperty] public partial int TotalPages { get; set; }
    [ObservableProperty] public partial int CurrentPage { get; set; }
    public string TotalPagesLabel => $"/ {TotalPages}";
    public bool ShowPager => TotalPages > 1;
    public int PageSlot1 => TotalPages <= 3 ? 1 : Math.Clamp(CurrentPage - 1, 1, TotalPages - 2);
    public int PageSlot2 => TotalPages <= 3 ? Math.Min(2, TotalPages) : PageSlot1 + 1;
    public int PageSlot3 => TotalPages <= 3 ? Math.Min(3, TotalPages) : PageSlot1 + 2;
    public bool IsPage1Selected => CurrentPage == PageSlot1;
    public bool IsPage2Selected => CurrentPage == PageSlot2;
    public bool IsPage3Selected => CurrentPage == PageSlot3;
    public bool ShowPage1 => TotalPages >= 1;
    public bool ShowPage2 => TotalPages >= 2;
    public bool ShowPage3 => TotalPages >= 3;
    [ObservableProperty] public partial IReadOnlyList<string> GenreMaster { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> SelectedGenres { get; set; }

    private IReadOnlyList<GameFilterIndex> _allFilterIndex = Array.Empty<GameFilterIndex>();
    private List<int> _filteredAppIds = new();
    private int _currentPage = 1;
    private int _gridColumns = 5;
    private bool _isUpdatingFilters;
    private int PageSize => _gridColumns * RowsPerPage;
    private readonly Dictionary<int, FixEntry> _cardCache = new();
    private CancellationTokenSource? _searchDebounceCts;
    private CancellationTokenSource? _coverEnrichCts;
    private readonly string _gamesIndexCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NexaPlay",
        "runtime_catalog_sources",
        "games_filter_index_cache_v3.json");

    public bool IsEmpty => !IsLoading && Games.Count == 0;

    public GamesViewModel(
        IMetadataService metadata,
        ISteamStoreService storeService,
        INexaPlayOverrideService nexaPlayOverride)
    {
        _metadata = metadata;
        _storeService = storeService;
        _nexaPlayOverride = nexaPlayOverride;

        // Default values for partial properties
        Games = new ObservableCollection<FixEntry>();
        CurrentPageLabel = "Halaman 1";
        CurrentPage = 1;
        SelectedGenres = Array.Empty<string>();
        SearchQuery = string.Empty;
        GenreMaster = new[]
        {
            "Indie","Action","Casual","Adventure","RPG","Strategy","Sports","Racing",
            "Massively Multiplayer","Design & Illustration","Web Publishing","Utilities","Education",
            "Game Development","Simulation","Violent","Video Production","Audio Production",
            "Software Training","Gore","Movie","Photo Editing","Sexual Content","Nudity","Episodic",
            "Tutorial","Documentary","Accounting"
        };
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allFilterIndex = await LoadOrBuildFilterIndexAsync();

            TotalCount = _allFilterIndex.Count;
            await ApplyFiltersAndPaginationAsync();
        }
        finally { IsLoading = false; }
    }

    private async Task<IReadOnlyList<GameFilterIndex>> LoadOrBuildFilterIndexAsync()
    {
        var cached = await TryReadFilterIndexCacheAsync();
        if (cached.Count > 0)
        {
            return cached;
        }

        var snapshot = await _metadata.GetCatalogSnapshotAsync();
        var built = snapshot
            .Select(game => new GameFilterIndex(
                game.AppId,
                (game.Name ?? string.Empty).NormalizeForSearch(),
                game.PriceNormalized,
                game.IsPremium,
                game.Protection,
                ParseGenreTokens(game.Genre)))
            .ToList();

        await TryWriteFilterIndexCacheAsync(built);
        return built;
    }

    public void UpdateGridColumns(int columns)
    {
        var normalized = Math.Clamp(columns, 3, 6);
        if (_gridColumns == normalized)
        {
            return;
        }

        var previousPage = _currentPage;
        _gridColumns = normalized;
        _currentPage = previousPage;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    private async Task ApplyFiltersAndPaginationAsync(bool resetPage = true)
    {
        if (SelectedGenres is null)
        {
            SelectedGenres = Array.Empty<string>();
        }

        IEnumerable<GameFilterIndex> query = _allFilterIndex;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            if (int.TryParse(q, out var searchAppId))
            {
                query = query.Where(x => x.AppId == searchAppId);
            }
            else
            {
                var lowered = q.NormalizeForSearch();
                query = query.Where(x => x.NameLower.Contains(lowered, StringComparison.Ordinal));
            }
        }

        if (FilterPremium && !FilterStandard)
        {
            query = query.Where(x => x.IsPremium);
        }
        else if (FilterStandard && !FilterPremium)
        {
            query = query.Where(x => !x.IsPremium);
        }

        if (FilterDenuvo && !FilterNonDenuvo)
        {
            query = query.Where(x => x.HasDenuvo);
        }
        else if (FilterNonDenuvo && !FilterDenuvo)
        {
            query = query.Where(x => !x.HasDenuvo);
        }

        if (SelectedGenres.Count > 0)
        {
            var selected = SelectedGenres
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.Ordinal);

            query = query.Where(x => x.GenreTokens.Overlaps(selected));
        }

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            query = query.Where(x => x.PriceNormalized >= 100000);
        }

        var daysSinceEpoch = (DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;
        int seed = (int)(daysSinceEpoch / 2);
        var random = new Random(seed);

        _filteredAppIds = query.OrderBy(x => random.Next()).Select(x => x.AppId).ToList();
        TotalCount = _filteredAppIds.Count;

        if (resetPage)
        {
            _currentPage = 1;
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(_filteredAppIds.Count / (double)PageSize));
        TotalPages = totalPages;
        OnPropertyChanged(nameof(TotalPagesLabel));
        if (_currentPage > totalPages)
        {
            _currentPage = totalPages;
        }

        var skip = (_currentPage - 1) * PageSize;
        var pageIds = _filteredAppIds.Skip(skip).Take(PageSize).ToList();
        var targetPageItems = await BuildPageItemsAsync(pageIds);
        SyncGamesPageItems(targetPageItems);

        CurrentPageLabel = $"Halaman {_currentPage}";
        CurrentPage = _currentPage;
        CanGoPrevious = _currentPage > 1;
        CanGoNext = _currentPage < totalPages;
        OnPropertyChanged(nameof(ShowPager));
        OnPropertyChanged(nameof(PageSlot1));
        OnPropertyChanged(nameof(PageSlot2));
        OnPropertyChanged(nameof(PageSlot3));
        OnPropertyChanged(nameof(ShowPage1));
        OnPropertyChanged(nameof(ShowPage2));
        OnPropertyChanged(nameof(ShowPage3));
        OnPropertyChanged(nameof(IsPage1Selected));
        OnPropertyChanged(nameof(IsPage2Selected));
        OnPropertyChanged(nameof(IsPage3Selected));
    }

    [RelayCommand]
    private Task SearchNow() => ApplyFiltersAndPaginationAsync();

    [RelayCommand]
    private void NextPage()
    {
        if (!CanGoNext) return;
        _currentPage++;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (!CanGoPrevious) return;
        _currentPage--;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    [RelayCommand]
    private void GoToPage(object? pageParam)
    {
        int page;
        if (pageParam is int intPage)
        {
            page = intPage;
        }
        else if (pageParam is string pageText && int.TryParse(pageText, out var parsed))
        {
            page = parsed;
        }
        else
        {
            return;
        }

        if (page < 1 || page > TotalPages) return;
        _currentPage = page;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    [RelayCommand]
    private void ToggleFilter() => IsFilterOpen = !IsFilterOpen;

    [RelayCommand]
    private void ClearFilters()
    {
        _isUpdatingFilters = true;
        FilterStandard = false;
        FilterPremium = false;
        FilterDenuvo = false;
        FilterNonDenuvo = false;
        _isUpdatingFilters = false;
        SelectedGenres = Array.Empty<string>();
        SearchQuery = string.Empty;
        _ = ApplyFiltersAndPaginationAsync();
    }

    public void SetGenreFilter(string genre, bool isIncluded)
    {
        var set = SelectedGenres.ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool changed = false;
        
        if (isIncluded)
            changed = set.Add(genre);
        else
            changed = set.Remove(genre);

        if (changed)
        {
            SelectedGenres = set.ToList();
            _ = ApplyFiltersAndPaginationAsync();
        }
    }

    partial void OnSearchQueryChanged(string value) => DebounceSearch();
    partial void OnFilterStandardChanged(bool value) 
    {
        if (_isUpdatingFilters) return;
        if (value) { _isUpdatingFilters = true; FilterPremium = false; _isUpdatingFilters = false; }
        _ = ApplyFiltersAndPaginationAsync();
    }
    
    partial void OnFilterPremiumChanged(bool value) 
    {
        if (_isUpdatingFilters) return;
        if (value) { _isUpdatingFilters = true; FilterStandard = false; _isUpdatingFilters = false; }
        _ = ApplyFiltersAndPaginationAsync();
    }
    
    partial void OnFilterDenuvoChanged(bool value) 
    {
        if (_isUpdatingFilters) return;
        if (value) { _isUpdatingFilters = true; FilterNonDenuvo = false; _isUpdatingFilters = false; }
        _ = ApplyFiltersAndPaginationAsync();
    }
    
    partial void OnFilterNonDenuvoChanged(bool value) 
    {
        if (_isUpdatingFilters) return;
        if (value) { _isUpdatingFilters = true; FilterDenuvo = false; _isUpdatingFilters = false; }
        _ = ApplyFiltersAndPaginationAsync();
    }

    partial void OnGamesChanged(ObservableCollection<FixEntry> value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    private void SyncGamesPageItems(IReadOnlyList<FixEntry> targetItems)
    {
        while (Games.Count > targetItems.Count)
        {
            Games.RemoveAt(Games.Count - 1);
        }

        for (var index = 0; index < targetItems.Count; index++)
        {
            if (index < Games.Count)
            {
                if (!ReferenceEquals(Games[index], targetItems[index]))
                {
                    Games[index] = targetItems[index];
                }
            }
            else
            {
                Games.Add(targetItems[index]);
            }
        }
    }

    private async Task<IReadOnlyList<FixEntry>> BuildPageItemsAsync(IReadOnlyList<int> pageIds)
    {
        _coverEnrichCts?.Cancel();
        _coverEnrichCts?.Dispose();
        _coverEnrichCts = new CancellationTokenSource();

        var results = new List<FixEntry>(pageIds.Count);
        foreach (var appId in pageIds)
        {
            var card = await GetOrBuildCardFastAsync(appId);
            if (card is not null)
            {
                results.Add(card);
            }
        }

        _ = EnrichPageCoverAsync(pageIds, _coverEnrichCts.Token);
        return results;
    }

    private async Task<FixEntry?> GetOrBuildCardFastAsync(int appId)
    {
        if (_cardCache.TryGetValue(appId, out var cached))
        {
            return cached;
        }

        var metadata = await _metadata.GetMetadataAsync(appId);
        if (metadata is null)
        {
            return null;
        }

        var selectedCover = FirstNonEmpty(
            metadata.PopularCoverImageUrl,
            metadata.HeaderImageUrl,
            metadata.RawHeaderImageUrl,
            null);

        if (string.IsNullOrWhiteSpace(selectedCover))
        {
            selectedCover = "NO CONTENT";
        }

        var card = new FixEntry
        {
            AppId = metadata.AppId,
            Title = metadata.Name,
            Publisher = metadata.PublisherDisplay,
            PosterUrl = selectedCover,
            IsPremium = metadata.IsPremium,
            HasDenuvo = metadata.HasDenuvo,
            Category = ParseCategory(metadata.Genre)
        };

        _cardCache[appId] = card;
        return card;
    }

    private async Task EnrichPageCoverAsync(IReadOnlyList<int> pageIds, CancellationToken ct)
    {
        using var gate = new SemaphoreSlim(4, 4);
        var tasks = pageIds.Select(async appId =>
        {
            await gate.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                var card = await GetOrBuildCardFastAsync(appId);
                if (card is null)
                {
                    return;
                }

                var metadata = await _metadata.GetMetadataAsync(appId);
                if (metadata is null)
                {
                    return;
                }

                var overrideCover = (await _nexaPlayOverride.GetCatalogOverrideAsync(appId, ct))?.LibraryCapsule;
                var detail = await _storeService.GetDetailAsync(appId, ct);
                var detailApiCover = detail?.LibraryCapsuleUrl;

                var selectedCover = FirstNonEmpty(
                    overrideCover,
                    detailApiCover,
                    detail?.SgdbGridUrl,
                    metadata.PopularCoverImageUrl,
                    metadata.HeaderImageUrl,
                    metadata.RawHeaderImageUrl,
                    null);

                if (string.IsNullOrWhiteSpace(selectedCover))
                {
                    selectedCover = "NO CONTENT";
                }

                if (string.Equals(card.PosterUrl, selectedCover, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var updatedCard = new FixEntry
                {
                    AppId = card.AppId,
                    Title = card.Title,
                    Publisher = card.Publisher,
                    PosterUrl = selectedCover,
                    IsPremium = card.IsPremium,
                    HasDenuvo = card.HasDenuvo,
                    Category = card.Category
                };
                _cardCache[appId] = updatedCard;

                var index = Games.IndexOf(card);
                if (index >= 0)
                {
                    Games[index] = updatedCard;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
            finally
            {
                gate.Release();
            }
        });

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static GameCategory ParseCategory(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return GameCategory.Other;
        }

        foreach (GameCategory category in Enum.GetValues(typeof(GameCategory)))
        {
            if (category == GameCategory.Other)
            {
                continue;
            }

            if (genre.Contains(category.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        return GameCategory.Other;
    }

    private static HashSet<string> ParseGenreTokens(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var baseTokens = genre
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var expanded = new HashSet<string>(baseTokens, StringComparer.Ordinal);
        foreach (var token in baseTokens)
        {
            if (GenreAliasMap.TryGetValue(token, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    expanded.Add(alias);
                }
            }
        }

        return expanded;
    }

    private async void DebounceSearch()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();

        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;
        try
        {
            await Task.Delay(220, cts.Token);
            if (cts.IsCancellationRequested)
            {
                return;
            }

            await ApplyFiltersAndPaginationAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private sealed record GameFilterIndex(
        int AppId,
        string NameLower,
        int PriceNormalized,
        bool IsPremium,
        bool HasDenuvo,
        HashSet<string> GenreTokens);

    private sealed record GameFilterIndexCacheItem(
        int AppId,
        string NameLower,
        int PriceNormalized,
        bool IsPremium,
        bool HasDenuvo,
        string[] GenreTokens);

    private async Task<IReadOnlyList<GameFilterIndex>> TryReadFilterIndexCacheAsync()
    {
        try
        {
            if (!File.Exists(_gamesIndexCachePath))
            {
                return Array.Empty<GameFilterIndex>();
            }

            await using var fs = File.OpenRead(_gamesIndexCachePath);
            var cached = await JsonSerializer.DeserializeAsync<List<GameFilterIndexCacheItem>>(fs);
            if (cached is null || cached.Count == 0)
            {
                return Array.Empty<GameFilterIndex>();
            }

            return cached.Select(x => new GameFilterIndex(
                x.AppId,
                x.NameLower ?? string.Empty,
                x.PriceNormalized,
                x.IsPremium,
                x.HasDenuvo,
                (x.GenreTokens ?? Array.Empty<string>()).ToHashSet(StringComparer.Ordinal)))
                .ToList();
        }
        catch
        {
            return Array.Empty<GameFilterIndex>();
        }
    }

    private async Task TryWriteFilterIndexCacheAsync(IReadOnlyList<GameFilterIndex> source)
    {
        try
        {
            var dir = Path.GetDirectoryName(_gamesIndexCachePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var payload = source.Select(x => new GameFilterIndexCacheItem(
                x.AppId,
                x.NameLower,
                x.PriceNormalized,
                x.IsPremium,
                x.HasDenuvo,
                x.GenreTokens.ToArray()))
                .ToList();

            await using var fs = File.Create(_gamesIndexCachePath);
            await JsonSerializer.SerializeAsync(fs, payload);
        }
        catch
        {
        }
    }



    private static string? ReadFirstAssetUrl(string? rawMetadataJson, string assetKey)
    {
        if (string.IsNullOrWhiteSpace(rawMetadataJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawMetadataJson);
            JsonElement node;

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

            if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("url", out var url) &&
                        url.ValueKind == JsonValueKind.String)
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

    private static readonly Dictionary<string, string[]> GenreAliasMap = new(StringComparer.Ordinal)
    {
        ["role-playing"] = ["rpg"],
        ["rpg"] = ["role-playing"],
        ["massively multiplayer"] = ["mmo"],
        ["mmo"] = ["massively multiplayer"],
        ["sports game"] = ["sports"],
        ["racing game"] = ["racing"],
        ["simulation game"] = ["simulation"],
        ["indie game"] = ["indie"],
        ["adventure game"] = ["adventure"],
        ["action game"] = ["action"],
        ["strategy game"] = ["strategy"],
        ["casual game"] = ["casual"],
        ["mmorpg"] = ["mmo", "rpg", "massively multiplayer"]
    };
}


