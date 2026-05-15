using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Models;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IFixGamesDataService _fixData;
    private readonly IAddGameService _addGame;

    [ObservableProperty] private int _totalFixes;
    [ObservableProperty] private int _libraryCount;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private IReadOnlyList<FixEntry> _recentFixes = Array.Empty<FixEntry>();
    [ObservableProperty] private IReadOnlyList<FixEntry> _popularGames = Array.Empty<FixEntry>();
    [ObservableProperty] private FixEntry? _heroGame;

    public HomeViewModel(IFixGamesDataService fixData, IAddGameService addGame)
    {
        _fixData = fixData;
        _addGame = addGame;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var fixes = await _fixData.GetAllFixesAsync();
            TotalFixes   = fixes.Count;
            LibraryCount = _addGame.ListLibraryGames().Count;
            
            if (fixes.Count > 0)
            {
                // Mocking data for the UI
                HeroGame = fixes[fixes.Count > 5 ? 5 : 0]; // Just pick one for Hero
                RecentFixes = fixes.Take(12).ToList();
                PopularGames = fixes.Skip(Math.Max(0, fixes.Count - 12)).Take(12).ToList(); // Mock popular
            }
        }
        finally { IsLoading = false; }
    }
}
