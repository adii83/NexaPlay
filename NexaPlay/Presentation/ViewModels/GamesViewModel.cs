using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class GamesViewModel : ObservableObject
{
    private readonly IBypassGamesDataService _fixData;
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
    public bool IsPage1Selected => CurrentPage == 1;
    public bool IsPage2Selected => CurrentPage == 2;
    public bool IsPage3Selected => CurrentPage == 3;
    public bool ShowPage1 => TotalPages >= 1;
    public bool ShowPage2 => TotalPages >= 2;
    public bool ShowPage3 => TotalPages >= 3;
    [ObservableProperty] public partial IReadOnlyList<string> GenreMaster { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> SelectedGenres { get; set; }

    private IReadOnlyList<FixEntry> _allGames = Array.Empty<FixEntry>();
    private List<FixEntry> _filteredGames = new();
    private int _currentPage = 1;
    private int _gridColumns = 5;
    private int PageSize => _gridColumns * RowsPerPage;

    public bool IsEmpty => !IsLoading && Games.Count == 0;

    public GamesViewModel(IBypassGamesDataService fixData)
    {
        _fixData = fixData;

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
            _allGames  = await _fixData.GetAllFixesAsync();
            TotalCount = _allGames.Count;
            ApplyFiltersAndPagination();
        }
        finally { IsLoading = false; }
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
        ApplyFiltersAndPagination(resetPage: false);
    }

    private void ApplyFiltersAndPagination(bool resetPage = true)
    {
        if (SelectedGenres is null)
        {
            SelectedGenres = Array.Empty<string>();
        }

        IEnumerable<FixEntry> query = _allGames;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            if (int.TryParse(q, out var appIdQuery))
            {
                query = query.Where(g => g.AppId == appIdQuery);
            }
            else
            {
                query = query.Where(g => g.Title.Contains(q, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (FilterPremium && !FilterStandard)
        {
            query = query.Where(g => g.IsPremium);
        }
        else if (FilterStandard && !FilterPremium)
        {
            query = query.Where(g => !g.IsPremium);
        }

        if (FilterDenuvo && !FilterNonDenuvo)
        {
            query = query.Where(g => false);
        }
        else if (FilterNonDenuvo && !FilterDenuvo)
        {
            query = query.Where(g => true);
        }

        if (SelectedGenres.Count > 0)
        {
            query = query.Where(g => SelectedGenres.Any(x => g.Category.ToString().Contains(x, StringComparison.OrdinalIgnoreCase)));
        }

        _filteredGames = query.ToList();
        TotalCount = _filteredGames.Count;

        if (resetPage)
        {
            _currentPage = 1;
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(_filteredGames.Count / (double)PageSize));
        TotalPages = totalPages;
        OnPropertyChanged(nameof(TotalPagesLabel));
        if (_currentPage > totalPages)
        {
            _currentPage = totalPages;
        }

        var skip = (_currentPage - 1) * PageSize;
        var targetPageItems = _filteredGames.Skip(skip).Take(PageSize).ToList();
        SyncGamesPageItems(targetPageItems);

        CurrentPageLabel = $"Halaman {_currentPage}";
        CurrentPage = _currentPage;
        CanGoPrevious = _currentPage > 1;
        CanGoNext = _currentPage < totalPages;
        OnPropertyChanged(nameof(ShowPager));
        OnPropertyChanged(nameof(ShowPage1));
        OnPropertyChanged(nameof(ShowPage2));
        OnPropertyChanged(nameof(ShowPage3));
        OnPropertyChanged(nameof(IsPage1Selected));
        OnPropertyChanged(nameof(IsPage2Selected));
        OnPropertyChanged(nameof(IsPage3Selected));
    }

    [RelayCommand]
    private void SearchNow() => ApplyFiltersAndPagination();

    [RelayCommand]
    private void NextPage()
    {
        if (!CanGoNext) return;
        _currentPage++;
        ApplyFiltersAndPagination(resetPage: false);
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (!CanGoPrevious) return;
        _currentPage--;
        ApplyFiltersAndPagination(resetPage: false);
    }

    [RelayCommand]
    private void GoToPage(string pageText)
    {
        if (!int.TryParse(pageText, out var page)) return;
        if (page < 1 || page > TotalPages) return;
        _currentPage = page;
        ApplyFiltersAndPagination(resetPage: false);
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
        ApplyFiltersAndPagination();
    }

    partial void OnSearchQueryChanged(string value) => ApplyFiltersAndPagination();
    partial void OnFilterStandardChanged(bool value) => ApplyFiltersAndPagination();
    partial void OnFilterPremiumChanged(bool value) => ApplyFiltersAndPagination();
    partial void OnFilterDenuvoChanged(bool value) => ApplyFiltersAndPagination();
    partial void OnFilterNonDenuvoChanged(bool value) => ApplyFiltersAndPagination();

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
}
