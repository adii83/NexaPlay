using NexaPlay.Core.Helpers;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public static class NewGamesCatalogFile
{
    public static Task PublishAppIdListAsync(string json, string activePath, CancellationToken ct = default) =>
        PublishAsync(json, activePath, text => NewGamesCatalog.ParseAppIds(text) is not null, ct);

    public static Task PublishSnapshotAsync(string json, string activePath, CancellationToken ct = default) =>
        PublishAsync(json, activePath, text => NewGamesCatalog.ParseSnapshot(text) is not null, ct);

    private static async Task PublishAsync(
        string json,
        string activePath,
        Func<string, bool> validate,
        CancellationToken ct)
    {
        if (!validate(json))
            throw new JsonException($"Invalid candidate for {Path.GetFileName(activePath)}.");

        var directory = Path.GetDirectoryName(activePath)
            ?? throw new ArgumentException("Active path must have a directory.", nameof(activePath));
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $".{Path.GetFileName(activePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(tempPath, json, ct);
            var readback = await File.ReadAllTextAsync(tempPath, ct);
            if (!validate(readback))
                throw new JsonException($"Candidate readback failed for {Path.GetFileName(activePath)}.");
            File.Move(tempPath, activePath, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }
}
