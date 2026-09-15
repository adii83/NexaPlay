namespace NexaPlay.Core.Helpers;

public static class ImageUriResolver
{
    public static bool TryCreate(string? raw, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (raw.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            raw = @"\\" + raw[8..];
        else if (raw.StartsWith(@"\\?\", StringComparison.Ordinal))
            raw = raw[4..];

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttp &&
             parsed.Scheme != Uri.UriSchemeHttps &&
             parsed.Scheme != Uri.UriSchemeFile &&
             parsed.Scheme != "ms-appx"))
        {
            return false;
        }

        uri = parsed;
        return true;
    }
}
