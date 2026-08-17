using System.Text;
using System.Text.Json.Serialization;

namespace NexaPlay.Core.Helpers;

public readonly record struct CatalogSourceStamp(string Name, long Length, long LastWriteTicks);

public sealed record CatalogCacheEnvelope<T>(
    [property: JsonPropertyName("schema")] int Schema,
    [property: JsonPropertyName("sourceRevision")] string SourceRevision,
    [property: JsonPropertyName("items")] T Items);

public static class CatalogCacheStamp
{
    public static string CreateRevision(params CatalogSourceStamp[] sources)
    {
        var revision = new StringBuilder(sources.Length * 48);
        foreach (var source in sources)
        {
            revision.Append(source.Name)
                .Append(':')
                .Append(source.Length)
                .Append(':')
                .Append(source.LastWriteTicks)
                .Append('|');
        }

        return revision.ToString();
    }

    public static bool IsCurrent<T>(CatalogCacheEnvelope<T>? envelope, int schema, string sourceRevision) =>
        envelope is not null &&
        envelope.Schema == schema &&
        string.Equals(envelope.SourceRevision, sourceRevision, StringComparison.Ordinal) &&
        envelope.Items is not null;
}
