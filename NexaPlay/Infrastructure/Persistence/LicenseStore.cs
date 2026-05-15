using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Contracts.Services;
using NexaPlay.Infrastructure.Platform;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Persistence;

/// <summary>Persists and validates license data (offline + online).</summary>
public sealed class LicenseStore
{
    private readonly string _filePath;

    public LicenseStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, AppConstants.LicenseFileName);
    }

    public LicenseInfo? Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return null;
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<LicenseInfo>(json);
        }
        catch { return null; }
    }

    public async Task SaveAsync(LicenseInfo info)
    {
        try
        {
            var json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch { }
    }

    public async Task DeleteAsync()
    {
        try { if (File.Exists(_filePath)) File.Delete(_filePath); }
        catch { }
        await Task.CompletedTask;
    }
}
