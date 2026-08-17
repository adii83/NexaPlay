using NexaPlay.Core.Models;
using System.Text.Json;

namespace NexaPlay.Core.Helpers;

public static class NewGamesCatalog
{
    private static readonly JsonSerializerOptions SnapshotOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static int[]? ParseAppIds(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            var appIds = new HashSet<int>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Number ||
                    !item.TryGetInt32(out var appId) ||
                    appId <= 0)
                {
                    return null;
                }

                appIds.Add(appId);
            }

            return appIds.Order().ToArray();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static NewGamesCatalogEntry? ParseMetadata(int appId, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return ParseMetadata(appId, document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static NewGamesCatalogEntry? ParseMetadata(int appId, JsonElement root)
    {
        if (appId <= 0 || root.ValueKind != JsonValueKind.Object)
            return null;

        var name = ReadString(root, "name") ?? ReadString(root, "title");
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var store = TryGetObject(root, "store_data");
        var developers = ReadStringArray(store, "developers");
        var publishers = ReadStringArray(store, "publishers");
        var genres = ReadDescriptionArray(store, "genres");
        var price = TryGetObject(store, "price_overview");
        var releaseDate = TryGetObject(store, "release_date");

        return new NewGamesCatalogEntry
        {
            AppId = appId,
            Name = name.Trim(),
            Developer = developers.FirstOrDefault(),
            Publisher = publishers.FirstOrDefault(),
            Developers = developers,
            Publishers = publishers,
            Genre = genres.Length == 0 ? null : string.Join(", ", genres),
            ShortDescription = ReadString(store, "short_description"),
            ReleaseDate = ReadString(releaseDate, "date") ?? ReadString(store, "release_date"),
            PriceNormalized = ReadInt(root, "price_normalized") ?? 0,
            PriceDisplay = ReadString(price, "final_formatted"),
            Protection = ReadProtection(root),
            HeaderImageUrl = ReadAssetUrl(root, "header"),
            IconImageUrl = ReadAssetUrl(root, "icon"),
            LibraryCapsuleUrl = ReadAssetUrl(root, "library_capsule") ?? ReadAssetUrl(root, "library_capsule_2x"),
            LibraryHero2xUrl = ReadAssetUrl(root, "library_hero_2x") ?? ReadAssetUrl(root, "library_hero"),
            BackgroundRawImageUrl = ReadAssetUrl(root, "background_raw") ?? ReadAssetUrl(root, "background")
        };
    }

    public static NewGamesCatalogEntry[]? ParseSnapshot(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            var entries = JsonSerializer.Deserialize<NewGamesCatalogEntry[]>(json, SnapshotOptions);
            if (entries is null || !AreValidUniqueEntries(entries))
                return null;

            return entries.OrderBy(entry => entry.AppId).ToArray();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string SerializeSnapshot(IEnumerable<NewGamesCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var ordered = entries.ToArray();
        if (!AreValidUniqueEntries(ordered))
            throw new JsonException("Snapshot contains an invalid or duplicate entry.");

        Array.Sort(ordered, static (left, right) => left.AppId.CompareTo(right.AppId));
        return JsonSerializer.Serialize(ordered, SnapshotOptions);
    }

    public static int[] SelectFetchAppIds(
        IEnumerable<int> requestedAppIds,
        ISet<int> primaryAppIds,
        IReadOnlyDictionary<int, NewGamesCatalogEntry> cachedEntries)
    {
        ArgumentNullException.ThrowIfNull(requestedAppIds);
        ArgumentNullException.ThrowIfNull(primaryAppIds);
        ArgumentNullException.ThrowIfNull(cachedEntries);

        return requestedAppIds
            .Where(appId => appId > 0 && !primaryAppIds.Contains(appId) && !cachedEntries.ContainsKey(appId))
            .Distinct()
            .Order()
            .ToArray();
    }

    public static NewGamesCatalogEntry[] ComposeSnapshot(
        IEnumerable<int> requestedAppIds,
        ISet<int> primaryAppIds,
        IEnumerable<NewGamesCatalogEntry> materializedEntries)
    {
        ArgumentNullException.ThrowIfNull(requestedAppIds);
        ArgumentNullException.ThrowIfNull(primaryAppIds);
        ArgumentNullException.ThrowIfNull(materializedEntries);

        var requested = requestedAppIds.Where(appId => appId > 0).ToHashSet();
        var materialized = materializedEntries.ToArray();
        if (!AreValidUniqueEntries(materialized))
            throw new JsonException("Materialized entries contain an invalid or duplicate entry.");

        return materialized
            .Where(entry => requested.Contains(entry.AppId) && !primaryAppIds.Contains(entry.AppId))
            .OrderBy(entry => entry.AppId)
            .ToArray();
    }

    private static bool AreValidUniqueEntries(IEnumerable<NewGamesCatalogEntry?> entries)
    {
        var appIds = new HashSet<int>();
        foreach (var entry in entries)
        {
            if (entry is null || entry.AppId <= 0 || string.IsNullOrWhiteSpace(entry.Name) || !appIds.Add(entry.AppId))
                return false;
        }

        return true;
    }

    private static JsonElement TryGetObject(JsonElement parent, string propertyName)
    {
        return parent.ValueKind == JsonValueKind.Object &&
               parent.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.Object
            ? value
            : default;
    }

    private static string? ReadString(JsonElement parent, string propertyName)
    {
        return parent.ValueKind == JsonValueKind.Object &&
               parent.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement parent, string propertyName)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(propertyName, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
            return number;

        return null;
    }

    private static bool ReadProtection(JsonElement root)
    {
        if (!root.TryGetProperty("protection", out var value))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => value.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true ||
                                    value.GetString()?.Contains("denuvo", StringComparison.OrdinalIgnoreCase) == true,
            _ => false
        };
    }

    private static string[] ReadStringArray(JsonElement parent, string propertyName)
    {
        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(propertyName, out var array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!)
            .ToArray();
    }

    private static string[] ReadDescriptionArray(JsonElement parent, string propertyName)
    {
        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(propertyName, out var array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Select(item => ReadString(item, "description"))
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Select(description => description!)
            .ToArray();
    }

    private static string? ReadAssetUrl(JsonElement root, string key)
    {
        if (!root.TryGetProperty("assets", out var assets) ||
            assets.ValueKind != JsonValueKind.Object ||
            !assets.TryGetProperty(key, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
            return string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString();

        if (value.ValueKind == JsonValueKind.Object)
            return ReadString(value, "url");

        if (value.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in value.EnumerateArray())
        {
            var url = item.ValueKind == JsonValueKind.String ? item.GetString() : ReadString(item, "url");
            if (!string.IsNullOrWhiteSpace(url))
                return url;
        }

        return null;
    }
}
