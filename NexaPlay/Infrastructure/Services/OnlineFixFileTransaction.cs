using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Stages first, records original bytes durably, then commits recoverable file replacements.</summary>
internal static class OnlineFixFileTransaction
{
    private const string StateFolder = ".nexaplay-onlinefix";
    private const long MaxExpandedBytes = 4L * 1024 * 1024 * 1024;
    private const int MaxFiles = 20000;
    private static readonly uint[] CrcTable = BuildCrcTable();

    internal static string ManifestPath(string root, int appId) =>
        Path.Combine(root, StateFolder, appId.ToString(), "transaction.json");

    internal static bool HasTransaction(string root, int appId) => Directory.Exists(StateRoot(root, appId));

    internal static void Extract(IReadOnlyList<string> packages, string staging, int appId,
        Action<int, string> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(staging);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long expanded = 0;
        foreach (var group in packages.GroupBy(VolumeGroup, StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();
            using var archive = ArchiveFactory.OpenArchive(group.OrderBy(VolumeNumber).Select(p => new FileInfo(p)).ToArray(),
                new ReaderOptions { LeaveStreamOpen = false });
            var entries = archive.Entries.Take(MaxFiles + 1).ToList();
            if (entries.Count > MaxFiles) throw new InvalidDataException("Arsip Online-Fix memiliki terlalu banyak entry.");
            var files = entries.Where(e => !e.IsDirectory).ToList();
            var prefix = appId + "/";
            var stripPrefix = files.Count > 0 && files.All(e => (e.Key ?? "").Replace('\\', '/').StartsWith(prefix, StringComparison.Ordinal));
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(entry.LinkTarget) || (entry.Attrib.GetValueOrDefault() & (int)FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Arsip Online-Fix tidak boleh berisi link filesystem.");
                var key = (entry.Key ?? "").Replace('\\', '/').TrimEnd('/');
                if (key.Length == 0) continue;
                // Validate directory entries as well, even though no links/directories are extracted directly.
                SafePath(staging, key);
                if (entry.IsDirectory) continue;
                if (entry.IsEncrypted) throw new InvalidDataException("Arsip Online-Fix diproteksi password. Sediakan arsip tanpa password pada mapping.");
                if (!entry.IsComplete) throw new InvalidDataException("Bagian arsip Online-Fix tidak lengkap.");
                var relative = stripPrefix ? key[prefix.Length..] : key;
                var target = SafePath(staging, relative);
                if (!names.Add(relative) || names.Count > MaxFiles)
                    throw new InvalidDataException("Path file Online-Fix duplikat atau jumlah file melampaui batas.");
                if (entry.Size < 0 || entry.Size > MaxExpandedBytes - expanded)
                    throw new InvalidDataException("Ukuran hasil ekstraksi Online-Fix melampaui 4 GiB.");
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                // Opening a ZIP entry may replace central-header metadata with its local header.
                var expectedCrc = entry.Crc;
                var expectedSize = entry.Size;
                using var input = entry.OpenEntryStream();
                using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                var buffer = new byte[81920];
                long actual = 0;
                uint crc = uint.MaxValue;
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    actual += count;
                    expanded += count;
                    if (actual > expectedSize || expanded > MaxExpandedBytes) throw new InvalidDataException("Ukuran arsip tidak sesuai metadata.");
                    for (var i = 0; i < count; i++) crc = CrcTable[(crc ^ buffer[i]) & 255] ^ (crc >> 8);
                    output.Write(buffer, 0, count);
                }
                if (actual != expectedSize ||
                    (archive.Type == ArchiveType.Zip || archive.Type == ArchiveType.Rar && expectedCrc != 0) && ~crc != (uint)expectedCrc)
                    throw new InvalidDataException("Arsip Online-Fix rusak (ukuran/checksum tidak cocok).");
                progress(names.Count, relative);
            }
        }
        if (names.Count == 0) throw new InvalidDataException("Arsip Online-Fix tidak berisi file.");
        foreach (var relative in names.Where(n => Path.GetFileName(n).Equals("unsteam.ini", StringComparison.OrdinalIgnoreCase)))
        {
            ct.ThrowIfCancellationRequested();
            var path = SafePath(staging, relative);
            if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("unsteam.ini terlalu besar.");
            // Latin-1 round-trips every byte, preserving BOMs and all non-placeholder content.
            var content = Encoding.Latin1.GetString(File.ReadAllBytes(path));
            var patched = content.Replace("<appid>", appId.ToString(), StringComparison.OrdinalIgnoreCase);
            if (content != patched) File.WriteAllBytes(path, Encoding.Latin1.GetBytes(patched));
        }
    }

    internal static void Install(string root, string staging, int appId, Action<int> progress, CancellationToken ct)
    {
        var stateRoot = StateRoot(root, appId);
        var manifestPath = ManifestPath(root, appId);
        if (Directory.Exists(stateRoot))
            throw new IOException("Data pemulihan Online-Fix masih ada. Jalankan Remove Online-Fix sebelum memasang ulang.");
        var files = Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        if (files.Count is 0 or > MaxFiles) throw new InvalidDataException("Jumlah file Online-Fix tidak valid.");
        var journal = new Journal { AppId = appId, State = "Applying" };
        var journalWritten = false;
        try
        {
            Directory.CreateDirectory(stateRoot);
            foreach (var source in files)
            {
                ct.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(staging, source).Replace('\\', '/');
                var target = SafePath(root, relative);
                if (Directory.Exists(target)) throw new IOException($"Target Online-Fix berupa folder: {relative}");
                var original = File.Exists(target);
                var entry = new JournalFile
                {
                    Path = relative,
                    InstalledHash = Hash(source, ct),
                    OriginalHash = original ? Hash(target, ct) : null
                };
                if (original)
                {
                    var backup = BackupPath(root, appId, relative);
                    AtomicCopy(target, backup, ct);
                    if (Hash(backup, ct) != entry.OriginalHash) throw new IOException("File game berubah selama persiapan backup.");
                }
                journal.Files.Add(entry);
            }
            // All backups exist and the write-ahead journal is durable before touching game files.
            Save(manifestPath, journal);
            journalWritten = true;
            for (var i = 0; i < journal.Files.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var entry = journal.Files[i];
                var target = SafePath(root, entry.Path);
                var currentHash = File.Exists(target) ? Hash(target, ct) : null;
                if (currentHash != entry.OriginalHash) throw new IOException($"File game berubah sebelum pemasangan: {entry.Path}");
                AtomicCopy(SafePath(staging, entry.Path), target, ct);
                progress((i + 1) * 100 / journal.Files.Count);
            }
            ct.ThrowIfCancellationRequested();
            journal.State = "Applied";
            Save(manifestPath, journal);
        }
        catch (Exception original)
        {
            if (journalWritten)
            {
                try { Restore(root, appId, journal, CancellationToken.None); }
                catch (Exception recovery)
                {
                    throw new IOException("Pemasangan berhenti dan pemulihan belum selesai. Backup disimpan; jalankan Remove Online-Fix. " + recovery.Message, original);
                }
            }
            Cleanup(root, appId);
            throw;
        }
    }

    internal static void Remove(string root, int appId, CancellationToken ct)
    {
        var manifest = SafeStatePath(root, appId, "transaction.json");
        // A crash during backup preparation cannot have changed game files yet.
        if (!File.Exists(manifest)) { Cleanup(root, appId); return; }
        if (new FileInfo(manifest).Length > 16 * 1024 * 1024) throw new InvalidDataException("Log pemulihan terlalu besar.");
        var journal = JsonSerializer.Deserialize<Journal>(File.ReadAllText(manifest)) ?? throw new InvalidDataException("Log pemulihan tidak valid.");
        if (journal.Version != 1 || journal.AppId != appId || journal.Files.Count is 0 or > MaxFiles ||
            journal.State is not ("Applied" or "Applying" or "Removing" or "Removed"))
            throw new InvalidDataException("Log pemulihan tidak cocok dengan game.");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in journal.Files)
        {
            SafePath(root, entry.Path);
            if (!paths.Add(entry.Path) || !IsHash(entry.InstalledHash) || entry.OriginalHash is not null && !IsHash(entry.OriginalHash))
                throw new InvalidDataException("Log pemulihan mengandung entry yang tidak valid.");
        }
        if (journal.State != "Removed") Restore(root, appId, journal, ct);
        Cleanup(root, appId);
    }

    internal static void RemoveLegacy(string root, string logPath, CancellationToken ct)
    {
        RejectLinks(Path.GetFullPath(root), logPath);
        if (new FileInfo(logPath).Length > 4 * 1024 * 1024) throw new InvalidDataException("Log Online-Fix lama terlalu besar.");
        var paths = File.ReadAllLines(logPath).Where(line =>
            !string.IsNullOrWhiteSpace(line) && !line.StartsWith('[') && line != "---" &&
            !line.StartsWith("Date:", StringComparison.OrdinalIgnoreCase) &&
            !line.StartsWith("AppId:", StringComparison.OrdinalIgnoreCase) &&
            !line.StartsWith("Files:", StringComparison.OrdinalIgnoreCase))
            .Select(line => SafePath(root, line)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (paths.Length > MaxFiles) throw new InvalidDataException("Log Online-Fix lama memiliki terlalu banyak file.");
        foreach (var path in paths)
        {
            ct.ThrowIfCancellationRequested();
            RejectLinks(Path.GetFullPath(root), path);
            if (Directory.Exists(path)) throw new IOException("Target penghapusan berupa folder.");
            if (File.Exists(path)) File.Delete(path);
        }
        File.Delete(logPath);
    }

    private static void Restore(string root, int appId, Journal journal, CancellationToken ct)
    {
        // Preflight every backup/path before making further changes. Modified user files are preserved.
        foreach (var entry in journal.Files)
        {
            ct.ThrowIfCancellationRequested();
            var target = SafePath(root, entry.Path);
            if (Directory.Exists(target)) throw new IOException($"Target pemulihan berupa folder: {entry.Path}");
            if (entry.OriginalHash is not null && Hash(BackupPath(root, appId, entry.Path), ct) != entry.OriginalHash)
                throw new IOException($"Backup file asli hilang atau rusak: {entry.Path}");
            if (File.Exists(target))
            {
                var current = Hash(target, ct);
                if (current != entry.InstalledHash && current != entry.OriginalHash)
                    throw new IOException($"File telah berubah setelah pemasangan; pemulihan dihentikan untuk melindunginya: {entry.Path}");
            }
        }
        journal.State = "Removing";
        Save(ManifestPath(root, appId), journal);
        foreach (var entry in journal.Files.AsEnumerable().Reverse())
        {
            ct.ThrowIfCancellationRequested();
            var target = SafePath(root, entry.Path);
            if (entry.OriginalHash is not null)
            {
                if (!File.Exists(target) || Hash(target, ct) != entry.OriginalHash)
                    AtomicCopy(BackupPath(root, appId, entry.Path), target, ct);
            }
            else if (File.Exists(target)) File.Delete(target);
        }
        journal.State = "Removed";
        Save(ManifestPath(root, appId), journal);
    }

    internal static string SafePath(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || relative.Length > 2048 || Path.IsPathRooted(relative))
            throw new InvalidDataException("Path arsip Online-Fix tidak valid.");
        var parts = relative.Replace('\\', '/').Split('/');
        foreach (var part in parts)
        {
            if (part.Length == 0 || part is "." or ".." || part.EndsWith('.') || part.EndsWith(' ') ||
                part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.Any(c => c < 32) ||
                part.Equals(StateFolder, StringComparison.OrdinalIgnoreCase) ||
                part.StartsWith("nexaplay-fix-log-", StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(part, @"\A(CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|\z)", RegexOptions.IgnoreCase))
                throw new InvalidDataException($"Path arsip Online-Fix ditolak: {relative}");
        }
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var target = Path.GetFullPath(Path.Combine(fullRoot, Path.Combine(parts)));
        if (!target.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Path Online-Fix keluar dari folder game.");
        RejectLinks(fullRoot, target);
        return target;
    }

    private static void RejectLinks(string root, string target)
    {
        for (var current = target; current is not null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Online-Fix tidak dapat melewati symbolic link/junction.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            if (string.Equals(current, root, StringComparison.OrdinalIgnoreCase)) break;
        }
    }

    private static string StateRoot(string root, int appId)
    {
        if (appId <= 0) throw new ArgumentOutOfRangeException(nameof(appId));
        var path = Path.Combine(Path.GetFullPath(root), StateFolder, appId.ToString());
        RejectLinks(Path.GetFullPath(root), path);
        return path;
    }

    private static string SafeStatePath(string root, int appId, string filename)
    {
        var path = Path.Combine(StateRoot(root, appId), filename);
        RejectLinks(Path.GetFullPath(root), path);
        return path;
    }

    private static string BackupPath(string root, int appId, string relative) =>
        SafePath(SafeStatePath(root, appId, "backup"), relative);

    private static bool IsHash(string hash) => hash is not null && Regex.IsMatch(hash, @"\A[0-9A-F]{64}\z");

    private static string Hash(string file, CancellationToken ct)
    {
        using var input = File.OpenRead(file);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        int count;
        while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            hash.AppendData(buffer, 0, count);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void AtomicCopy(string source, string target, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".nexaplay-tmp";
        try
        {
            using (var input = File.OpenRead(source))
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    output.Write(buffer, 0, count);
                }
                output.Flush(flushToDisk: true);
            }
            ct.ThrowIfCancellationRequested();
            File.Move(temporary, target, overwrite: true);
        }
        finally { try { File.Delete(temporary); } catch { } }
    }

    private static void Save(string path, Journal journal)
    {
        var temporary = path + ".tmp";
        RejectLinks(Path.GetDirectoryName(path)!, temporary);
        using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(output, journal);
            output.Flush(flushToDisk: true);
        }
        File.Move(temporary, path, overwrite: true);
    }

    private static void Cleanup(string root, int appId)
    {
        // Only delete our verified, app-specific transaction directory after originals are restored.
        var path = StateRoot(root, appId);
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }

    private static string VolumeGroup(string path) => Regex.Replace(Regex.Replace(
        Regex.Replace(path, @"\.part\d+\.rar\z", ".rar", RegexOptions.IgnoreCase),
        @"\.r\d{2}\z", ".rar", RegexOptions.IgnoreCase),
        @"\.(zip|7z)\.\d{3}\z", ".$1", RegexOptions.IgnoreCase);

    private static int VolumeNumber(string path)
    {
        var classic = Regex.Match(path, @"\.r(?<n>\d{2})\z", RegexOptions.IgnoreCase);
        if (classic.Success) return int.Parse(classic.Groups["n"].Value) + 1;
        var match = Regex.Match(path, @"(?:\.part(?<n>\d+)\.rar|\.(?<n>\d{3}))\z", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups["n"].Value, out var number) ? number : 0;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++) value = (value & 1) != 0 ? 0xEDB88320 ^ (value >> 1) : value >> 1;
            table[i] = value;
        }
        return table;
    }

    internal sealed class Journal
    {
        public int Version { get; set; } = 1;
        public int AppId { get; set; }
        public string State { get; set; } = "Applying";
        public List<JournalFile> Files { get; set; } = [];
    }

    internal sealed class JournalFile
    {
        public string Path { get; set; } = "";
        public string InstalledHash { get; set; } = "";
        public string? OriginalHash { get; set; }
    }
}
