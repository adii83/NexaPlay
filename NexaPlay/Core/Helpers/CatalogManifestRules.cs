using NexaPlay.Core.Models;
using System.Text.Json;

namespace NexaPlay.Core.Helpers;

public static class CatalogManifestRules
{
    public static CatalogManifestDocument? Parse(string json, IReadOnlySet<string> allowedNames)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(allowedNames);

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("schema", out var schemaNode) ||
                !schemaNode.TryGetInt32(out var schema) ||
                schema != 1 ||
                !root.TryGetProperty("revision", out var revisionNode) ||
                revisionNode.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(revisionNode.GetString()) ||
                !root.TryGetProperty("files", out var filesNode) ||
                filesNode.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var files = new Dictionary<string, CatalogManifestEntry>(StringComparer.OrdinalIgnoreCase);
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in filesNode.EnumerateObject())
            {
                if (!seenNames.Add(property.Name))
                    return null;
                if (!allowedNames.Contains(property.Name))
                    continue;

                var node = property.Value;
                if (node.ValueKind != JsonValueKind.Object ||
                    !node.TryGetProperty("url", out var urlNode) ||
                    urlNode.ValueKind != JsonValueKind.String ||
                    !Uri.TryCreate(urlNode.GetString(), UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps ||
                    !node.TryGetProperty("size", out var sizeNode) ||
                    !sizeNode.TryGetInt64(out var size) ||
                    size <= 0 ||
                    !node.TryGetProperty("sha256", out var hashNode) ||
                    hashNode.ValueKind != JsonValueKind.String ||
                    !IsSha256(hashNode.GetString()))
                {
                    return null;
                }

                files[property.Name] = new CatalogManifestEntry(
                    property.Name,
                    uri.AbsoluteUri,
                    size,
                    hashNode.GetString()!.ToLowerInvariant());
            }

            return files.Count == 0
                ? null
                : new CatalogManifestDocument(schema, revisionNode.GetString()!.Trim(), files);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string[] SelectChangedFiles(
        CatalogManifestDocument manifest,
        IReadOnlyDictionary<string, CatalogFileState> localFiles)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(localFiles);

        return manifest.Files.Values
            .Where(file => !localFiles.TryGetValue(file.Name, out var local) ||
                           local.Size != file.Size ||
                           !string.Equals(local.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Name)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);
}
