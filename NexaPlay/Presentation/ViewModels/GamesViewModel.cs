using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class GamesViewModel : ObservableObject
{
    private readonly IMetadataService _metadata;
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
    private int PageSize => _gridColumns * RowsPerPage;
    private readonly Dictionary<int, FixEntry> _cardCache = new();
    private CancellationTokenSource? _searchDebounceCts;
    private readonly string _gamesIndexCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NexaPlay",
        "runtime_catalog_sources",
        "games_filter_index_cache.json");

    public bool IsEmpty => !IsLoading && Games.Count == 0;

    public GamesViewModel(IMetadataService metadata)
    {
        _metadata = metadata;

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
                (game.Name ?? string.Empty).ToLowerInvariant(),
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
                var lowered = q.ToLowerInvariant();
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

        _filteredAppIds = query.Select(x => x.AppId).ToList();
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
    private void ToggleGenre(string genre)
    {
        var set = SelectedGenres.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!set.Add(genre))
        {
            set.Remove(genre);
        }
        SelectedGenres = set.ToList();
        _ = ApplyFiltersAndPaginationAsync();
    }

    partial void OnSearchQueryChanged(string value) => DebounceSearch();
    partial void OnFilterStandardChanged(bool value) => _ = ApplyFiltersAndPaginationAsync();
    partial void OnFilterPremiumChanged(bool value) => _ = ApplyFiltersAndPaginationAsync();
    partial void OnFilterDenuvoChanged(bool value) => _ = ApplyFiltersAndPaginationAsync();
    partial void OnFilterNonDenuvoChanged(bool value) => _ = ApplyFiltersAndPaginationAsync();

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
        var results = new List<FixEntry>(pageIds.Count);
        foreach (var appId in pageIds)
        {
            var card = await GetOrBuildCardAsync(appId);
            if (card is not null)
            {
                results.Add(card);
            }
        }

        return results;
    }

    private async Task<FixEntry?> GetOrBuildCardAsync(int appId)
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

        var card = new FixEntry
        {
            AppId = metadata.AppId,
            Title = metadata.Name,
            Publisher = metadata.PublisherDisplay,
            PosterUrl = metadata.PopularCoverImageUrl ?? metadata.HeaderImageUrl,
            IsPremium = metadata.IsPremium,
            HasDenuvo = metadata.HasDenuvo,
            Category = ParseCategory(metadata.Genre)
        };

        _cardCache[appId] = card;
        return card;
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
        bool IsPremium,
        bool HasDenuvo,
        HashSet<string> GenreTokens);

    private sealed record GameFilterIndexCacheItem(
        int AppId,
        string NameLower,
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
