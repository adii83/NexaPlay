using CommunityToolkit.Mvvm.ComponentModel;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Models;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class GamesViewModel : ObservableObject
{
    private readonly IBypassGamesDataService _fixData;

    [ObservableProperty] public partial string SearchQuery { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial IReadOnlyList<FixEntry> Games { get; set; }
    [ObservableProperty] public partial int TotalCount { get; set; }

    private IReadOnlyList<FixEntry> _allGames = Array.Empty<FixEntry>();

    public bool IsEmpty => !IsLoading && Games.Count == 0;

    public GamesViewModel(IBypassGamesDataService fixData)
    {
        _fixData = fixData;

        // Default values for partial properties
        SearchQuery = string.Empty;
        Games       = Array.Empty<FixEntry>();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allGames  = await _fixData.GetAllFixesAsync();
            TotalCount = _allGames.Count;
            Games      = _allGames;
        }
        finally { IsLoading = false; }
    }

    public void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            Games = _allGames;
            return;
        }
        var q = SearchQuery.ToLowerInvariant();
        Games = _allGames.Where(g => g.Title.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    partial void OnSearchQueryChanged(string value) => Search();

    partial void OnGamesChanged(IReadOnlyList<FixEntry> value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));
}
