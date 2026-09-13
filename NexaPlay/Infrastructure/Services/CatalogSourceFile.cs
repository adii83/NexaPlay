using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public static class CatalogSourceFile
{
    public static async Task<string> ValidateAsync(
        string path,
        bool isGzip,
        long? expectedSize = null,
        string? expectedSha256 = null,
        CancellationToken ct = default,
        string? sourceName = null)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0 || (expectedSize.HasValue && info.Length != expectedSize.Value))
            throw new InvalidDataException($"Invalid size for {info.Name}.");

        await using (var file = File.OpenRead(path))
        await using (Stream json = isGzip ? new GZipStream(file, CompressionMode.Decompress) : file)
        {
            using var document = await JsonDocument.ParseAsync(json, cancellationToken: ct);
            ValidateShape(sourceName, document.RootElement);
        }

        await using var hashFile = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(hashFile, ct)).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(expectedSha256) &&
            !string.Equals(hash, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"SHA-256 mismatch for {info.Name}.");
        }

        return hash;
    }

    private static void ValidateShape(string? sourceName, JsonElement root)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return;

        var normalized = sourceName.Replace('\\', '/');
        if (normalized.Equals("new_games.json", StringComparison.OrdinalIgnoreCase))
        {
            if (root.ValueKind != JsonValueKind.Array || root.EnumerateArray().Any(item =>
                    item.ValueKind != JsonValueKind.Number ||
                    !item.TryGetInt32(out var appId) ||
                    appId <= 0))
            {
                throw new JsonException("new_games.json must contain positive integer AppIDs.");
            }

            return;
        }

        if (normalized is "steam_data.json" or "steam_data.json.gz" or "override_data.json")
        {
            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException($"{sourceName} must contain a JSON object.");
            return;
        }

        if (root.ValueKind is not JsonValueKind.Array and not JsonValueKind.Object)
            throw new JsonException($"{sourceName} must contain a JSON array or object.");
    }

    public static void Publish(string candidatePath, string activePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(activePath)!);
        File.Move(candidatePath, activePath, overwrite: true);
    }

    public static void PublishBatch(IReadOnlyList<(string CandidatePath, string ActivePath)> files)
    {
        var backups = new List<(string ActivePath, string BackupPath, bool Existed)>();
        try
        {
            foreach (var file in files)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file.ActivePath)!);
                var backupPath = $"{file.ActivePath}.{Guid.NewGuid():N}.bak";
                var existed = File.Exists(file.ActivePath);
                if (existed)
                    File.Move(file.ActivePath, backupPath, overwrite: false);
                backups.Add((file.ActivePath, backupPath, existed));
            }

            foreach (var file in files)
                Publish(file.CandidatePath, file.ActivePath);
        }
        catch
        {
            foreach (var backup in backups.AsEnumerable().Reverse())
            {
                try
                {
                    if (backup.Existed && File.Exists(backup.BackupPath))
                        File.Move(backup.BackupPath, backup.ActivePath, overwrite: true);
                    else if (!backup.Existed)
                        TryDelete(backup.ActivePath);
                }
                catch { }
            }

            throw;
        }
        finally
        {
            foreach (var backup in backups)
                TryDelete(backup.BackupPath);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
