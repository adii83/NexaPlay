using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace NexaPlay.Infrastructure.Platform;

/// <summary>Generates a stable, unique device ID from hardware characteristics.</summary>
public static class DeviceIdHelper
{
    public static string GetDeviceId()
    {
        try
        {
            var parts = new List<string>();

            // Machine GUID from registry
            var machineGuid = SafeRegGet(Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Cryptography", "MachineGuid");
            if (!string.IsNullOrWhiteSpace(machineGuid)) parts.Add(machineGuid);

            // Processor ID
            var cpuId = SafeRegGet(Registry.LocalMachine,
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString");
            if (!string.IsNullOrWhiteSpace(cpuId)) parts.Add(cpuId);

            // Computer name
            parts.Add(Environment.MachineName);

            // Username
            parts.Add(Environment.UserName);

            var raw = string.Join("|", parts);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var hex = Convert.ToHexString(hash)[..24];
            return $"WIN-{hex[..8]}-{hex[8..16]}-{hex[16..24]}".ToUpperInvariant();
        }
        catch
        {
            // Fallback: machine name hash
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
            var hex = Convert.ToHexString(hash)[..24];
            return $"WIN-{hex[..8]}-{hex[8..16]}-{hex[16..24]}".ToUpperInvariant();
        }
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
