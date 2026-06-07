using System.Management;
using System.Security.Cryptography;
using System.Text;
using NexaPlay.Core.Constants;

namespace NexaPlay.Infrastructure.Platform;

/// <summary>Generates a stable, unique device ID from hardware characteristics, identical to GameHub's WMI implementation.</summary>
public static class DeviceIdHelper
{
    public static void PrimeCachedDeviceId(string? deviceId)
    {
        TryWriteCachedDeviceId(deviceId ?? string.Empty);
    }

    public static string GetDeviceId()
    {
        try
        {
            var cached = TryReadCachedDeviceId();
            if (!string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            string cpu = GetCpuId();
            string mb = GetMotherboardSerial();
            string uuid = GetSystemUuid();

            // Fallback jika ada yang kosong
            if (string.IsNullOrEmpty(cpu)) cpu = "missing_cpu";
            if (string.IsNullOrEmpty(mb)) mb = "missing_mb";
            if (string.IsNullOrEmpty(uuid)) uuid = "missing_uuid";

            string raw = cpu + "|" + mb + "|" + uuid;
            string deviceId = Sha256(raw);
            TryWriteCachedDeviceId(deviceId);
            return deviceId;
        }
        catch
        {
            // Worst case fallback
            string fallback = Sha256(Environment.MachineName + "_" + Environment.UserName);
            TryWriteCachedDeviceId(fallback);
            return fallback;
        }
    }

    // === Hardware Collectors ===

    private static string GetCpuId()
    {
        try
        {
#pragma warning disable CA1416 // Validate platform compatibility
            using var searcher = new ManagementObjectSearcher("select ProcessorId from Win32_Processor");
            foreach (var item in searcher.Get())
                return item["ProcessorId"]?.ToString()?.Trim() ?? "";
#pragma warning restore CA1416
        }
        catch { }
        return "";
    }

    private static string GetMotherboardSerial()
    {
        try
        {
#pragma warning disable CA1416
            using var searcher = new ManagementObjectSearcher("select SerialNumber from Win32_BaseBoard");
            foreach (var item in searcher.Get())
                return item["SerialNumber"]?.ToString()?.Trim() ?? "";
#pragma warning restore CA1416
        }
        catch { }
        return "";
    }

    private static string GetSystemUuid()
    {
        try
        {
#pragma warning disable CA1416
            using var searcher = new ManagementObjectSearcher("select UUID from Win32_ComputerSystemProduct");
            foreach (var item in searcher.Get())
                return item["UUID"]?.ToString()?.Trim() ?? "";
#pragma warning restore CA1416
        }
        catch { }
        return "";
    }

    // === HASH FUNCTION ===

    private static string Sha256(string input)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static string GetCachePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "device_id.cache");
    }

    private static string? TryReadCachedDeviceId()
    {
        try
        {
            var path = GetCachePath();
            if (!File.Exists(path))
            {
                return null;
            }

            var cached = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(cached) ? null : cached;
        }
        catch
        {
            return null;
        }
    }

    private static void TryWriteCachedDeviceId(string deviceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return;
            }

            File.WriteAllText(GetCachePath(), deviceId);
        }
        catch
        {
        }
    }
}
