using NexaPlay.Contracts.Services;
using NexaPlay.Core.Models;
using Microsoft.Win32;
using System.Diagnostics;

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
}
