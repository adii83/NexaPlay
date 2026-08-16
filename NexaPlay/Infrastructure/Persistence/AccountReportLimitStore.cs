using NexaPlay.Core.Constants;
using System.Globalization;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Persistence;

public sealed class AccountReportLimitStore
{
    public const int MaxReportsPerDay = 2;

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<int, ReportLimitState> _limits = new();

    public AccountReportLimitStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, AppConstants.AccountReportLimitFileName);
        LoadSync();
    }

    public async Task<bool> CanReportAsync(int appId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var today = GetTodayKey();
            return !_limits.TryGetValue(appId, out var state)
                   || state.Date != today
                   || state.Count < MaxReportsPerDay;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RecordSuccessAsync(int appId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var today = GetTodayKey();
            if (!_limits.TryGetValue(appId, out var state) || state.Date != today)
            {
                state = new ReportLimitState { Date = today };
                _limits[appId] = state;
            }

            state.Count = Math.Min(state.Count + 1, MaxReportsPerDay);
            await SaveAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string GetTodayKey()
        => DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private void LoadSync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            _limits = JsonSerializer.Deserialize<Dictionary<int, ReportLimitState>>(
                File.ReadAllText(_filePath)) ?? new();
        }
        catch
        {
            _limits = new();
        }
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        var tempPath = _filePath + ".tmp";
        var json = JsonSerializer.Serialize(_limits);
        await File.WriteAllTextAsync(tempPath, json, ct);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private sealed class ReportLimitState
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
