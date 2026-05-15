using NexaPlay.Core.Constants;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Persistence;

/// <summary>Persists which games have had a fix applied.</summary>
public sealed class AppliedStateStore
{
    private readonly string _filePath;
    private Dictionary<int, bool> _state = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AppliedStateStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, AppConstants.AppliedStateFileName);
        LoadSync();
    }

    public bool IsApplied(int appId)
    {
        _state.TryGetValue(appId, out var v);
        return v;
    }

    public async Task SetAppliedAsync(int appId, bool value)
    {
        await _lock.WaitAsync();
        try
        {
            _state[appId] = value;
            await SaveAsync();
        }
        finally { _lock.Release(); }
    }

    public IReadOnlyDictionary<int, bool> GetAll() => _state;

    private void LoadSync()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            _state = JsonSerializer.Deserialize<Dictionary<int, bool>>(json) ?? new();
        }
        catch { _state = new(); }
    }

    private async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_state);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch { }
    }
}
