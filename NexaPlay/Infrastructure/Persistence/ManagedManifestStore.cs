using NexaPlay.Core.Constants;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Persistence;

public sealed class ManagedManifestStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ManagedManifestStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "managed_appmanifests.json");
    }

    public async Task<IReadOnlyList<int>> GetAllAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
                return Array.Empty<int>();

            return (JsonSerializer.Deserialize<List<int>>(await File.ReadAllTextAsync(_filePath, ct)) ?? [])
                .Where(static appId => appId > 0)
                .Distinct()
                .ToArray();
        }
        catch
        {
            return Array.Empty<int>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RecordAsync(int appId, CancellationToken ct = default)
    {
        if (appId <= 0)
            return;

        await _lock.WaitAsync(ct);
        try
        {
            var appIds = File.Exists(_filePath)
                ? JsonSerializer.Deserialize<List<int>>(await File.ReadAllTextAsync(_filePath, ct)) ?? []
                : [];
            if (appIds.Contains(appId))
                return;

            appIds.Add(appId);
            var tempPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(appIds), ct);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }
}
