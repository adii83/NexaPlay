using NexaPlay.Core.Models;
using System.Text.Json;

namespace NexaPlay.Core.Helpers;

public static class DetailHeroSelector
{
    public static string Select(
        NexaPlayCatalogOverride? catalogOverride,
        GameDetailEntry? detail,
        GameEntry? game) =>
        catalogOverride?.LibraryHero2x
        ?? ReadFirstAssetUrl(detail?.RawMetadataJson, "library_hero")
        ?? ReadFirstAssetUrl(detail?.RawMetadataJson, "library_hero_2x")
        ?? detail?.SgdbHeroUrl
        ?? game?.LibraryHero2xUrl
        ?? ReadFirstAssetUrl(detail?.RawMetadataJson, "background_raw")
        ?? game?.BackgroundRawImageUrl
        ?? detail?.BackgroundImageUrl
        ?? ReadFirstAssetUrl(detail?.RawMetadataJson, "header")
        ?? game?.HeaderImageUrl
        ?? string.Empty;

    private static string? ReadFirstAssetUrl(string? rawMetadataJson, string assetKey)
    {
        if (string.IsNullOrWhiteSpace(rawMetadataJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawMetadataJson);
            JsonElement node;
            if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                assets.TryGetProperty(assetKey, out var nestedNode))
            {
                node = nestedNode;
            }
            else if (doc.RootElement.TryGetProperty(assetKey, out var rootNode))
            {
                node = rootNode;
            }
            else
            {
                return null;
            }

            if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("url", out var url) &&
                        url.ValueKind == JsonValueKind.String)
                        return url.GetString();
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
