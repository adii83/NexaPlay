using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Services;

/// <summary>
/// Packages GitHub branch archives using the supplied Lightning payload rules.
/// Only this preparation step filters Lua; installation copies final bytes unchanged.
/// </summary>
internal static class AddGamePayloadBuilder
{
    internal const long MaxZipBytes = 128L * 1024 * 1024;
    private const long MaxMemberBytes = 64L * 1024 * 1024;
    private const long MaxTotalBytes = 256L * 1024 * 1024;
    private const int MaxEntries = 10_000;
    private const string Footer = "-- Made with love by NexaPlay ID \U0001f5a4\U0001f90d";
    private const string JsWhitespace = "\u0009\u000b\u000c\u0020\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000\ufeff\u000a\u000d\u2028\u2029";
    private static readonly UTF8Encoding Utf8 = new(false);
    private static readonly char[] TrimCharacters = JsWhitespace.ToCharArray();
    private static readonly uint[] CrcTable = CreateCrcTable();

    internal static string ResolveMode(string configured, string url) => configured switch
    {
        "auto" => url.Contains("SPIN0ZAi", StringComparison.Ordinal) ? "spin0zai" : "standard",
        "standard" or "spin0zai" => configured,
        _ => throw new InvalidDataException("lua_processing harus auto, standard, atau spin0zai.")
    };

    internal static void Build(string sourceZip, string finalZip, string mode, CancellationToken ct)
    {
        if (new FileInfo(sourceZip).Length > MaxZipBytes)
            throw new InvalidDataException("ZIP melebihi 128 MiB.");

        var work = Path.Combine(Path.GetDirectoryName(finalZip)!, "payload-" + Guid.NewGuid().ToString("N"));
        var extracted = Path.Combine(work, "extracted");
        var final = Path.Combine(work, "final");
        Directory.CreateDirectory(extracted);
        Directory.CreateDirectory(final);
        try
        {
            using (var archive = ZipFile.Open(sourceZip, ZipArchiveMode.Read,
                CodePagesEncodingProvider.Instance.GetEncoding(437)))
            {
                if (archive.Entries.Count > MaxEntries)
                    throw new InvalidDataException("ZIP melebihi 10.000 anggota.");
                var metadata = ReadMetadata(sourceZip, archive.Entries.Count);

                long total = 0;
                for (var index = 0; index < archive.Entries.Count; index++)
                {
                    var entry = archive.Entries[index];
                    var member = metadata[index];
                    ct.ThrowIfCancellationRequested();
                    var name = member.Name.Replace('\\', '/');
                    var parts = ValidateMember(entry, name);
                    if ((member.Flags & 1) != 0)
                        throw new InvalidDataException("ZIP terenkripsi tidak didukung.");
                    if (name.StartsWith("__MACOSX/", StringComparison.Ordinal))
                        continue;

                    var target = Path.GetFullPath(Path.Combine(extracted, Path.Combine(parts)));
                    if (!target.StartsWith(extracted + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Path ZIP keluar dari folder sementara.");
                    if (name.EndsWith('/') || ((entry.ExternalAttributes >> 16) & 0xf000) == 0x4000 || member.DosDirectory)
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }

                    if (member.Method is not (0 or 8))
                        throw new InvalidDataException("Metode kompresi ZIP tidak didukung.");
                    total = checked(total + entry.Length);
                    if (entry.Length > MaxMemberBytes || total > MaxTotalBytes)
                        throw new InvalidDataException("Ukuran ekstraksi ZIP melebihi batas.");
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    using var input = entry.Open();
                    using var output = File.Create(target);
                    CopyBounded(input, output, entry.Length, ct, member.Crc);
                }
            }

            // Materialize, then walk the native directory order, as the supplied
            // Python script does. This preserves Windows collision/overwrite rules.
            long processedTotal = 0;
            foreach (var source in WalkFiles(extracted))
            {
                ct.ThrowIfCancellationRequested();
                var name = Path.GetFileName(source);
                var isLua = name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase);
                if (!isLua && !name.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
                    continue;
                var data = File.ReadAllBytes(source);
                if (isLua)
                    data = ProcessLua(data, mode);
                processedTotal = checked(processedTotal + data.LongLength);
                if (data.LongLength > MaxMemberBytes || processedTotal > MaxTotalBytes)
                    throw new InvalidDataException("Hasil pemrosesan melebihi batas ukuran.");
                File.WriteAllBytes(Path.Combine(final, name), data);
            }

            var files = WalkFiles(final).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
            if (files.Length == 0)
                throw new InvalidDataException("Tidak ada Lua atau manifest dalam ZIP.");
            using var result = ZipFile.Open(finalZip, ZipArchiveMode.Create);
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                var entry = result.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);
                entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
                using var input = File.OpenRead(file);
                using var output = entry.Open();
                CopyBounded(input, output, input.Length, ct);
            }
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }

    private static byte[] ProcessLua(byte[] data, string mode)
    {
        var lines = Regex.Split(Utf8.GetString(data), "\r?\n");
        var kept = mode == "spin0zai"
            ? lines.Where(raw =>
            {
                var line = raw.TrimStart(TrimCharacters);
                return line.StartsWith("addappid(", StringComparison.Ordinal)
                    || line.StartsWith("adddlc(", StringComparison.Ordinal)
                    || line.StartsWith("setManifestid(", StringComparison.Ordinal);
            })
            : lines.AsEnumerable(); // include_manifests=True in the supplied script.
        return Utf8.GetBytes(string.Join("\n", kept.Concat(new[] { "", Footer })));
    }

    private static IEnumerable<string> WalkFiles(string directory)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            if (Directory.Exists(entry))
            {
                foreach (var file in WalkFiles(entry))
                    yield return file;
            }
            else
                yield return entry;
        }
    }

    private sealed record MemberMetadata(string Name, ushort Flags, ushort Method, bool DosDirectory, uint Crc);

    // ZipArchive does not expose Info-ZIP's Unicode Path (0x7075) field.
    // Read central-directory metadata so filenames match the supplied Python/yauzl rules.
    private static MemberMetadata[] ReadMetadata(string path, int count)
    {
        using var file = File.OpenRead(path);
        using var reader = new BinaryReader(file, Utf8, leaveOpen: true);
        var tailSize = (int)Math.Min(file.Length, 65_557);
        file.Position = file.Length - tailSize;
        var tail = reader.ReadBytes(tailSize);
        var end = -1;
        for (var i = tail.Length - 22; i >= 0; i--)
        {
            if (BitConverter.ToUInt32(tail, i) == 0x06054b50 && i + 22 + BitConverter.ToUInt16(tail, i + 20) == tail.Length)
            {
                end = i;
                break;
            }
        }
        if (end < 0)
            throw new InvalidDataException("Direktori ZIP tidak valid.");
        long offset = BitConverter.ToUInt32(tail, end + 16);
        if (offset == uint.MaxValue)
        {
            file.Position = file.Length - tailSize + end - 20;
            if (reader.ReadUInt32() != 0x07064b50)
                throw new InvalidDataException("Direktori ZIP64 tidak valid.");
            reader.ReadUInt32();
            file.Position = checked((long)reader.ReadUInt64() + 48);
            offset = checked((long)reader.ReadUInt64());
        }
        file.Position = offset;
        var members = new MemberMetadata[count];
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadUInt32() != 0x02014b50)
                throw new InvalidDataException("Anggota direktori ZIP tidak valid.");
            var madeBy = reader.ReadUInt16();
            reader.ReadUInt16();
            var flags = reader.ReadUInt16();
            var method = reader.ReadUInt16();
            file.Position += 4;
            var crc = reader.ReadUInt32();
            file.Position += 8;
            var nameLength = reader.ReadUInt16();
            var extraLength = reader.ReadUInt16();
            var commentLength = reader.ReadUInt16();
            file.Position += 4;
            var attributes = reader.ReadUInt32();
            file.Position += 4;
            var rawName = reader.ReadBytes(nameLength);
            var extra = reader.ReadBytes(extraLength);
            if (rawName.Length != nameLength || extra.Length != extraLength)
                throw new InvalidDataException("Metadata anggota ZIP terpotong.");
            var encoding = (flags & 0x800) != 0 ? Utf8 : CodePagesEncodingProvider.Instance.GetEncoding(437)!;
            var name = encoding.GetString(rawName);
            for (var p = 0; p + 4 <= extra.Length;)
            {
                var tag = BitConverter.ToUInt16(extra, p);
                var length = BitConverter.ToUInt16(extra, p + 2);
                p += 4;
                if (p + length > extra.Length)
                    throw new InvalidDataException("Extra field ZIP terpotong.");
                if (tag == 0x7075 && length >= 6 && extra[p] == 1 && BitConverter.ToUInt32(extra, p + 1) == Crc32(rawName))
                {
                    name = Utf8.GetString(extra, p + 5, length - 5);
                    break;
                }
                p += length;
            }
            members[i] = new MemberMetadata(name, flags, method, (madeBy >> 8) == 0 && attributes == 16, crc);
            file.Position += commentLength;
        }
        return members;
    }

    private static uint Crc32(byte[] bytes)
    {
        var crc = uint.MaxValue;
        foreach (var value in bytes)
        {
            crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        }
        return ~crc;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint value = 0; value < table.Length; value++)
        {
            var crc = value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
            table[value] = crc;
        }
        return table;
    }

    private static string[] ValidateMember(ZipArchiveEntry entry, string name)
    {
        if (string.IsNullOrEmpty(name) || name.StartsWith('/') || name.Contains('\0'))
            throw new InvalidDataException("Nama anggota ZIP tidak aman.");
        var parts = name.TrimEnd('/').Split('/');
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part) || part is "." or ".." || part.EndsWith('.') || part.EndsWith(' ')
                || part.Any(c => c < 32 || "<>:\"|?*".Contains(c))
                || Regex.IsMatch(part, "^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\\..*)?$", RegexOptions.IgnoreCase))
                throw new InvalidDataException("Path anggota ZIP tidak aman.");
        }
        var kind = (entry.ExternalAttributes >> 16) & 0xf000;
        if (kind is not (0 or 0x8000 or 0x4000))
            throw new InvalidDataException("Symlink/berkas khusus dalam ZIP tidak didukung.");
        return parts;
    }

    internal static void CopyBounded(Stream input, Stream output, long expectedLength, CancellationToken ct, uint? expectedCrc = null)
    {
        if (expectedLength > MaxMemberBytes)
            throw new InvalidDataException("Anggota ZIP melebihi 64 MiB.");
        var buffer = new byte[81920];
        long count = 0;
        var crc = uint.MaxValue;
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
        {
            ct.ThrowIfCancellationRequested();
            count += read;
            if (count > expectedLength || count > MaxMemberBytes)
                throw new InvalidDataException("Ukuran anggota ZIP tidak sesuai metadata/batas.");
            if (expectedCrc.HasValue)
                for (var i = 0; i < read; i++)
                    crc = CrcTable[(crc ^ buffer[i]) & 0xff] ^ (crc >> 8);
            output.Write(buffer, 0, read);
        }
        if (count != expectedLength)
            throw new InvalidDataException("Anggota ZIP terpotong.");
        if (expectedCrc.HasValue && ~crc != expectedCrc.Value)
            throw new InvalidDataException("CRC anggota ZIP tidak cocok.");
    }

    internal static ZipArchiveEntry SelectLua(ZipArchive archive, string appId) =>
        archive.Entries.FirstOrDefault(e => Path.GetFileName(e.FullName.Replace('\\', '/')) == appId + ".lua")
        ?? archive.Entries.FirstOrDefault(e => Regex.IsMatch(Path.GetFileName(e.FullName.Replace('\\', '/')), "^[0-9]+\\.lua$"))
        ?? throw new InvalidDataException("Tidak ada file .lua numerik dalam ZIP.");

    internal static void ValidateInstallable(string path, string appId, CancellationToken ct)
    {
        if (new FileInfo(path).Length > MaxZipBytes)
            throw new InvalidDataException("ZIP melebihi 128 MiB.");
        using var archive = ZipFile.OpenRead(path);
        if (archive.Entries.Count > MaxEntries)
            throw new InvalidDataException("ZIP melebihi 10.000 anggota.");
        var metadata = ReadMetadata(path, archive.Entries.Count);
        var lua = SelectLua(archive, appId);
        long total = 0;
        for (var index = 0; index < archive.Entries.Count; index++)
        {
            var entry = archive.Entries[index];
            if (entry != lua && !entry.FullName.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
                continue;
            ValidateMember(entry, entry.FullName.Replace('\\', '/'));
            total = checked(total + entry.Length);
            if (total > MaxTotalBytes)
                throw new InvalidDataException("Total hasil ZIP melebihi 256 MiB.");
            using var input = entry.Open();
            CopyBounded(input, Stream.Null, entry.Length, ct, metadata[index].Crc);
        }
    }
}
