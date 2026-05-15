using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Diagnostics;
using System.Management;

namespace NexaPlay.Infrastructure.Platform;

/// <summary>Detects AV software via WMI and manages Windows Defender exclusions via PowerShell.</summary>
public sealed class WindowsDefenderService : IWindowsDefenderService
{
    private readonly IAppLogService _log;

    public WindowsDefenderService(IAppLogService log) => _log = log;

    public async Task<IReadOnlyList<AntivirusInfo>> DetectAntivirusAsync()
    {
        return await Task.Run(() =>
        {
            var results = new List<AntivirusInfo>();
            try
            {
                // WMI Security Center 2 (Windows 8+)
                using var searcher = new ManagementObjectSearcher(
                    @"root\SecurityCenter2",
                    "SELECT * FROM AntiVirusProduct");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = obj["displayName"]?.ToString() ?? string.Empty;
                    var stateHex = obj["productState"]?.ToString();
                    bool isActive = false;
                    if (!string.IsNullOrEmpty(stateHex) && uint.TryParse(stateHex, out var state))
                        isActive = ((state >> 12) & 0xF) >= 1;

                    results.Add(new AntivirusInfo
                    {
                        DisplayName = name,
                        Vendor = DetectVendor(name),
                        IsActive = isActive
                    });
                }
            }
            catch (Exception ex) { _log.Log("AV", $"WMI query error: {ex.Message}"); }

            // Always add Windows Defender status
            if (!results.Any(r => r.Vendor == AntivirusVendor.WindowsDefender))
            {
                results.Insert(0, new AntivirusInfo
                {
                    DisplayName = "Windows Defender",
                    Vendor = AntivirusVendor.WindowsDefender,
                    IsActive = true
                });
            }
            _log.Log("AV", $"Detected {results.Count} AV product(s)");
            return (IReadOnlyList<AntivirusInfo>)results;
        });
    }

    public async Task<bool> AddExclusionAsync(string path)
    {
        _log.Log("Defender", $"Adding exclusion for path...");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"Add-MpPreference -ExclusionPath '{EscapePs(path)}'\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            if (proc is null) return false;
            await proc.WaitForExitAsync();

            // Verify
            var verified = await IsPathExcludedAsync(path);
            _log.Log("Defender", $"Exclusion {(verified ? "added" : "failed")}");
            return verified;
        }
        catch (Exception ex)
        {
            _log.Log("Defender", $"Exclusion error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsPathExcludedAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -Command \"(Get-MpPreference).ExclusionPath\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc is null) return false;
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return output.Contains(path, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }

    public async Task<IReadOnlyList<string>> GetExclusionsAsync()
    {
        return await Task.Run(() =>
        {
            var list = new List<string>();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -Command \"(Get-MpPreference).ExclusionPath\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc is null) return (IReadOnlyList<string>)list;
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                list.AddRange(output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrEmpty(l)));
            }
            catch (Exception ex) { _log.Log("Defender", $"GetExclusions error: {ex.Message}"); }
            return list;
        });
    }

    private static AntivirusVendor DetectVendor(string name)
    {
        if (string.IsNullOrEmpty(name)) return AntivirusVendor.Unknown;
        var n = name.ToLowerInvariant();
        if (n.Contains("defender") || n.Contains("windows security")) return AntivirusVendor.WindowsDefender;
        if (n.Contains("avast"))       return AntivirusVendor.Avast;
        if (n.Contains("avg"))         return AntivirusVendor.AVG;
        if (n.Contains("kaspersky"))   return AntivirusVendor.Kaspersky;
        if (n.Contains("norton") || n.Contains("symantec")) return AntivirusVendor.Norton;
        if (n.Contains("bitdefender")) return AntivirusVendor.Bitdefender;
        if (n.Contains("mcafee"))      return AntivirusVendor.McAfee;
        if (n.Contains("eset"))        return AntivirusVendor.ESET;
        if (n.Contains("malwarebytes"))return AntivirusVendor.Malwarebytes;
        return AntivirusVendor.Unknown;
    }

    private static string EscapePs(string s) => s.Replace("'", "''");
}
