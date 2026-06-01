using NexaPlay.Contracts.Services;
using NexaPlay.Core.Models;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace NexaPlay.Infrastructure.Platform;

/// <summary>Steam integration: path detection, library scanning, app manifest parsing.</summary>
public sealed class SteamPlatformService : ISteamService
{
    private readonly IAppLogService _log;

    public SteamPlatformService(IAppLogService log) => _log = log;

    public string? GetSteamBasePath()
    {
        string?[] candidates =
        [
            SafeRegGet(Registry.CurrentUser,  @"Software\Valve\Steam",             "SteamPath"),
            SafeRegGet(Registry.CurrentUser,  @"Software\WOW6432Node\Valve\Steam", "SteamPath"),
            SafeRegGet(Registry.LocalMachine, @"Software\Valve\Steam",             "InstallPath"),
            SafeRegGet(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
            Environment.GetEnvironmentVariable("STEAMPATH"),
            Environment.GetEnvironmentVariable("STEAM_PATH"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            @"C:\Program Files\Steam",
        ];

        foreach (var p in candidates)
        {
            try { if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p)) return p; }
            catch { }
        }

        // Fallback: running steam.exe process
        try
        {
            foreach (var proc in Process.GetProcessesByName("steam"))
            {
                var exe = proc.MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe))
                {
                    var dir = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) return dir;
                }
            }
        }
        catch { }

        return null;
    }

    public IReadOnlyList<string> GetLibraryPaths()
    {
        var libs = new List<string>();
        var basePath = GetSteamBasePath();
        if (string.IsNullOrEmpty(basePath)) return libs;

        libs.Add(basePath);
        var vdf = Path.Combine(basePath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdf)) return libs;

        try
        {
            foreach (var raw in File.ReadAllLines(vdf))
            {
                var line = raw.Trim();
                if (!line.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var path = parts[^1].Replace(@"\\", @"\").Trim();
                    try { if (Directory.Exists(path)) libs.Add(path); } catch { }
                }
            }
        }
        catch (Exception ex) { _log.Log("Steam", $"Error parsing libraryfolders.vdf: {ex.Message}"); }

        return libs;
    }

    public IReadOnlyList<InstalledGame> ScanInstalledGames()
    {
        var games = new List<InstalledGame>();
        foreach (var lib in GetLibraryPaths())
        {
            var steamapps = Path.Combine(lib, "steamapps");
            if (!Directory.Exists(steamapps)) continue;
            foreach (var manifest in Directory.GetFiles(steamapps, "appmanifest_*.acf"))
            {
                try
                {
                    var (installdir, name, appId) = ParseManifest(manifest);
                    if (appId <= 0 || string.IsNullOrWhiteSpace(installdir)) continue;
                    var installPath = Path.Combine(steamapps, "common", installdir);
                    if (!Directory.Exists(installPath)) continue;
                    games.Add(new InstalledGame
                    {
                        AppId = appId,
                        Name = name ?? $"App {appId}",
                        InstallPath = installPath
                    });
                }
                catch { }
            }
        }
        _log.Log("Steam", $"Scanned {games.Count} installed games");
        return games;
    }

    public string? ResolveGameInstallPath(int appId)
    {
        foreach (var lib in GetLibraryPaths())
        {
            var manifest = Path.Combine(lib, "steamapps", $"appmanifest_{appId}.acf");
            if (!File.Exists(manifest)) continue;
            var (installdir, _, _) = ParseManifest(manifest);
            if (string.IsNullOrWhiteSpace(installdir)) continue;
            var path = Path.Combine(lib, "steamapps", "common", installdir);
            if (Directory.Exists(path)) return path;
        }
        return null;
    }

    public string? GetGameName(int appId)
    {
        foreach (var lib in GetLibraryPaths())
        {
            var manifest = Path.Combine(lib, "steamapps", $"appmanifest_{appId}.acf");
            if (!File.Exists(manifest)) continue;
            var (_, name, _) = ParseManifest(manifest);
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }
        return null;
    }

    public async Task RestartAsync()
    {
        _log.Log("Steam", "Restarting Steam...");
        try
        {
            foreach (var proc in Process.GetProcessesByName("steam"))
            {
                proc.Kill();
                await Task.Delay(500);
            }
            var steamPath = GetSteamBasePath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                var exe = Path.Combine(steamPath, "steam.exe");
                if (File.Exists(exe)) Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            }
        }
        catch (Exception ex) { _log.Log("Steam", $"Restart error: {ex.Message}"); }
    }

    public async Task<bool> SetLaunchOptionsAndRestartAsync(int appId, string launchOptions)
    {
        if (appId <= 0 || string.IsNullOrWhiteSpace(launchOptions))
            return false;

        var steamBase = GetSteamBasePath();
        if (string.IsNullOrWhiteSpace(steamBase))
        {
            _log.Log("Steam", "SetLaunchOptions gagal: base path Steam tidak ditemukan.");
            return false;
        }

        var localconfig = FindUserLocalconfig(Path.Combine(steamBase, "userdata"));
        if (string.IsNullOrWhiteSpace(localconfig) || !File.Exists(localconfig))
        {
            _log.Log("Steam", "SetLaunchOptions gagal: localconfig.vdf tidak ditemukan.");
            return false;
        }

        try
        {
            KillSteamProcesses();
            await Task.Delay(3000);

            var backupPath = $"{localconfig}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
            File.Copy(localconfig, backupPath, overwrite: true);
            _log.Log("Steam", $"Backup localconfig dibuat: {backupPath}");

            var lines = File.ReadAllLines(localconfig).ToList();
            var updated = UpsertLaunchOptions(lines, appId.ToString(), launchOptions);
            if (!updated)
            {
                _log.Log("Steam", "SetLaunchOptions gagal: block apps tidak ditemukan di localconfig.vdf.");
                return false;
            }

            File.WriteAllLines(localconfig, lines, Encoding.UTF8);
            _log.Log("Steam", $"LaunchOptions berhasil di-set untuk appid={appId}");

            StartSteamProcess(steamBase);
            return true;
        }
        catch (Exception ex)
        {
            _log.Log("Steam", $"SetLaunchOptions error: {ex.Message}");
            return false;
        }
    }

    // --- Helpers ---

    private static (string? installdir, string? name, int appId) ParseManifest(string path)
    {
        string? installdir = null, name = null;
        int appId = 0;
        try
        {
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (TryParseAcfValue(line, "appid",      out var v)) int.TryParse(v, out appId);
                if (TryParseAcfValue(line, "installdir", out var i)) installdir = i;
                if (TryParseAcfValue(line, "name",       out var n)) name = n;
            }
        }
        catch { }
        return (installdir, name, appId);
    }

    private static bool TryParseAcfValue(string line, string key, out string? value)
    {
        value = null;
        if (!line.StartsWith($"\"{key}\"", StringComparison.OrdinalIgnoreCase)) return false;
        var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2) { value = parts[^1]; return true; }
        return false;
    }

    private static string? SafeRegGet(RegistryKey root, string subKey, string valueName)
    {
        try
        {
            using var k = root.OpenSubKey(subKey);
            return k?.GetValue(valueName) as string;
        }
        catch { return null; }
    }

    private static void KillSteamProcesses()
    {
        var processes = new[] { "steam.exe", "steamwebhelper.exe", "GameOverlayUI.exe" };
        foreach (var process in processes)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/F /IM {process}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit();
        }
    }

    private static void StartSteamProcess(string steamBase)
    {
        var steamExe = Path.Combine(steamBase, "steam.exe");
        if (File.Exists(steamExe))
        {
            Process.Start(new ProcessStartInfo(steamExe)
            {
                UseShellExecute = true
            });
        }
    }

    private static string? FindUserLocalconfig(string userdataPath)
    {
        if (!Directory.Exists(userdataPath)) return null;
        var candidates = Directory.GetFiles(userdataPath, "localconfig.vdf", SearchOption.AllDirectories)
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}config{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count == 0) return null;

        // Non-interactive: choose most recently modified profile.
        return candidates
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static bool UpsertLaunchOptions(List<string> lines, string appId, string launchOptions)
    {
        var root = FindBlock(lines, 0, lines.Count - 1, "UserLocalConfigStore");
        if (!root.found) return false;
        var software = FindBlock(lines, root.open + 1, root.close - 1, "Software");
        if (!software.found) return false;
        var valve = FindBlock(lines, software.open + 1, software.close - 1, "Valve");
        if (!valve.found) return false;
        var steam = FindBlock(lines, valve.open + 1, valve.close - 1, "Steam");
        if (!steam.found) return false;
        var apps = FindBlock(lines, steam.open + 1, steam.close - 1, "apps");
        if (!apps.found) return false;

        var appBlock = FindBlock(lines, apps.open + 1, apps.close - 1, appId);
        if (!appBlock.found)
        {
            var indent = GetIndent(lines[apps.keyLine]) + "\t";
            var escapedValue = EscapeVdfValue(launchOptions);
            var insertAt = apps.close;
            lines.Insert(insertAt++, $"{indent}\"{appId}\"");
            lines.Insert(insertAt++, $"{indent}{{");
            lines.Insert(insertAt++, $"{indent}\t\"LaunchOptions\"\t\t\"{escapedValue}\"");
            lines.Insert(insertAt, $"{indent}}}");
            return true;
        }

        var launchLineIdx = FindKeyLine(lines, appBlock.open + 1, appBlock.close - 1, "LaunchOptions");
        var appIndent = GetIndent(lines[appBlock.keyLine]) + "\t";
        var escaped = EscapeVdfValue(launchOptions);
        var newLine = $"{appIndent}\"LaunchOptions\"\t\t\"{escaped}\"";
        if (launchLineIdx >= 0)
            lines[launchLineIdx] = newLine;
        else
            lines.Insert(appBlock.close, newLine);

        return true;
    }

    private static (bool found, int keyLine, int open, int close) FindBlock(List<string> lines, int start, int end, string key)
    {
        for (var i = start; i <= end; i++)
        {
            if (!TryExtractQuotedKey(lines[i], out var lineKey) ||
                !string.Equals(lineKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            var open = -1;
            for (var j = i + 1; j <= end; j++)
            {
                var t = lines[j].Trim();
                if (string.IsNullOrWhiteSpace(t)) continue;
                if (t == "{")
                {
                    open = j;
                    break;
                }
                break;
            }
            if (open < 0) continue;
            var close = FindMatchingBrace(lines, open, end);
            if (close > open)
                return (true, i, open, close);
        }
        return (false, -1, -1, -1);
    }

    private static int FindMatchingBrace(List<string> lines, int openIndex, int end)
    {
        var depth = 0;
        for (var i = openIndex; i <= end; i++)
        {
            var t = lines[i].Trim();
            if (t == "{") depth++;
            else if (t == "}")
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static int FindKeyLine(List<string> lines, int start, int end, string key)
    {
        for (var i = start; i <= end; i++)
        {
            if (TryExtractQuotedKey(lines[i], out var lineKey) &&
                string.Equals(lineKey, key, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static bool TryExtractQuotedKey(string line, out string key)
    {
        key = string.Empty;
        var m = Regex.Match(line, "^\\s*\"([^\"]+)\"");
        if (!m.Success) return false;
        key = m.Groups[1].Value;
        return true;
    }

    private static string GetIndent(string line)
    {
        var i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
        return line[..i];
    }

    private static string EscapeVdfValue(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }
}
