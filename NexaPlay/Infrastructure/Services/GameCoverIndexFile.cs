using System.IO.Compression;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public static class GameCoverIndexFile
{
    public static async Task PublishValidatedAsync(
        string candidatePath,
        string activePath,
        bool isGzip,
        CancellationToken ct = default)
    {
        await using (var file = File.OpenRead(candidatePath))
        await using (Stream stream = isGzip ? new GZipStream(file, CompressionMode.Decompress) : file)
        using (var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct))
        {
            if (!ContainsCover(document.RootElement))
            {
                throw new JsonException("Library capsule cover index is empty or invalid.");
            }
        }

        File.Move(candidatePath, activePath, overwrite: true);
    }

    private static bool ContainsCover(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (!int.TryParse(property.Name, out _) || property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (HasString(property.Value, "library_capsule") ||
                HasString(property.Value, "library_capsule_2x"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasString(JsonElement node, string name) =>
        node.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(value.GetString());
}
