using NexaPlay.Core.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Downloads only the mapped Drive file, including its public confirmation form.</summary>
internal sealed class OnlineFixArchiveDownloader(HttpClient http)
{
    internal const long MaxPackageBytes = 2L * 1024 * 1024 * 1024;
    private const int MaxConfirmationBytes = 256 * 1024;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    internal async Task DownloadAsync(OnlineFixPackage package, string target,
        Action<long, long> progress, CancellationToken ct)
    {
        var uri = new Uri(package.DownloadUrl);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("NexaPlay/1.0");
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                if (attempt != 0) throw DriveError();
                var html = await ReadConfirmationAsync(response.Content, ct).ConfigureAwait(false);
                uri = ParseConfirmation(html, package.DriveId);
                continue;
            }
            var total = response.Content.Headers.ContentLength ?? 0;
            if (total > MaxPackageBytes) throw new InvalidDataException("Paket Online-Fix terlalu besar (maksimal 2 GiB).");
            await using var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var buffer = new byte[81920];
            long read = 0;
            while (true)
            {
                using var idle = CancellationTokenSource.CreateLinkedTokenSource(ct);
                idle.CancelAfter(TimeSpan.FromSeconds(60));
                int count;
                try { count = await input.ReadAsync(buffer, idle.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                { throw new TimeoutException("Unduhan Online-Fix tidak merespons selama 60 detik."); }
                if (count == 0) break;
                read += count;
                if (read > MaxPackageBytes) throw new InvalidDataException("Paket Online-Fix terlalu besar.");
                await output.WriteAsync(buffer.AsMemory(0, count), ct).ConfigureAwait(false);
                progress(read, total);
            }
            if (read == 0 || total > 0 && read != total)
                throw new InvalidDataException("Unduhan Online-Fix kosong atau tidak lengkap.");
            await output.FlushAsync(ct).ConfigureAwait(false);
            return;
        }
        throw DriveError();
    }

    internal static Uri ParseConfirmation(string html, string expectedId)
    {
        var forms = Regex.Matches(html, @"<form\b(?<attrs>[^>]*)>(?<body>[\s\S]*?)</form>",
            RegexOptions.IgnoreCase, RegexTimeout);
        foreach (Match form in forms)
        {
            var attributes = Attributes(form.Groups["attrs"].Value);
            if (!attributes.TryGetValue("action", out var action) ||
                !Uri.TryCreate(action, UriKind.Absolute, out var uri) || !OnlineFixCatalogService.IsDriveDownloadUri(uri)) continue;
            if (attributes.TryGetValue("method", out var method) && !method.Equals("get", StringComparison.OrdinalIgnoreCase)) continue;
            var query = OnlineFixCatalogService.GetQuery(uri);
            foreach (Match input in Regex.Matches(form.Groups["body"].Value, @"<input\b(?<attrs>[^>]*)>", RegexOptions.IgnoreCase, RegexTimeout))
            {
                var values = Attributes(input.Groups["attrs"].Value);
                if (values.TryGetValue("name", out var name) && values.TryGetValue("value", out var value) &&
                    name is "id" or "export" or "confirm" or "uuid") query[name] = value;
            }
            if (!query.TryGetValue("id", out var id) || id != expectedId || !query.ContainsKey("confirm")) continue;
            query["export"] = "download";
            return new UriBuilder(uri) { Query = string.Join("&", query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")) }.Uri;
        }
        throw DriveError();
    }

    private static Dictionary<string, string> Attributes(string html)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(html, "(?<key>[a-zA-Z_-]+)\\s*=\\s*(?:\"(?<value>[^\"]*)\"|'(?<value>[^']*)')", RegexOptions.None, RegexTimeout))
            result[match.Groups["key"].Value] = WebUtility.HtmlDecode(match.Groups["value"].Value);
        return result;
    }

    private static async Task<string> ReadConfirmationAsync(HttpContent content, CancellationToken ct)
    {
        if (content.Headers.ContentLength > MaxConfirmationBytes) throw DriveError();
        await using var input = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            if (output.Length + count > MaxConfirmationBytes) throw DriveError();
            output.Write(buffer, 0, count);
        }
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    private static InvalidDataException DriveError() => new(
        "Google Drive tidak memberikan arsip. Periksa link mapping, akses publik, dan kuota unduhan. Arsip yang memerlukan login tidak didukung.");
}
