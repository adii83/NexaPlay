using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Platform;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Persistence;

/// <summary>Persists and validates license data using GameHub's AES encryption parity.</summary>
public sealed class LicenseStore
{
    private const string SECRET = "adigeel83271120043522012711040003";

    private readonly string _nexaPlayDir;
    private readonly string _nexaPlayFilePath;
    private readonly string _gameHubFilePath;
    private readonly string _appLogPath;

    public LicenseStore()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        _nexaPlayDir = Path.Combine(localAppData, AppConstants.AppDataFolder);
        Directory.CreateDirectory(_nexaPlayDir);
        _nexaPlayFilePath = Path.Combine(_nexaPlayDir, AppConstants.LicenseFileName);
        _appLogPath = Path.Combine(_nexaPlayDir, AppConstants.LogFileName);
        
        _gameHubFilePath = Path.Combine(localAppData, "GameHub", "license.dat");
    }

    private class StoredLicense
    {
        public string? license_key { get; set; }
        public string? device_id { get; set; }
        public string? plan { get; set; }
        public string? activated_at { get; set; }
    }

    public LicenseInfo? Load()
    {
        try
        {
            AppendStoreTrace($"Load entered. nexaplay_exists={File.Exists(_nexaPlayFilePath)} gamehub_exists={File.Exists(_gameHubFilePath)}");
            string targetPath = _nexaPlayFilePath;
            
            // Fallback to GameHub directory if NexaPlay's license doesn't exist
            if (!File.Exists(targetPath))
            {
                if (File.Exists(_gameHubFilePath))
                {
                    targetPath = _gameHubFilePath;
                }
                else
                {
                    AppendStoreTrace("Load found no license file in NexaPlay or GameHub");
                    return null;
                }
            }

            AppendStoreTrace($"Load using file: {targetPath}");
            string encrypted = File.ReadAllText(targetPath);
            AppendStoreTrace($"Load read encrypted file length={encrypted.Length}");
            string json = Decrypt(encrypted);
            AppendStoreTrace($"Load decrypted payload length={json.Length}");

            AppendStoreTrace("Load parsing JSON payload");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? licenseKey = root.TryGetProperty("license_key", out var licenseKeyProp)
                ? licenseKeyProp.GetString()
                : null;
            string? deviceId = root.TryGetProperty("device_id", out var deviceIdProp)
                ? deviceIdProp.GetString()
                : null;
            string? plan = root.TryGetProperty("plan", out var planProp)
                ? planProp.GetString()
                : null;

            if (string.IsNullOrEmpty(licenseKey))
            {
                AppendStoreTrace("Load parsed empty license payload");
                return null;
            }

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                DeviceIdHelper.PrimeCachedDeviceId(deviceId);
                AppendStoreTrace("Load primed cached device id from stored license");
            }

            AppendStoreTrace($"Load parsed plan={plan ?? "(null)"} stored_device_present={!string.IsNullOrWhiteSpace(deviceId)}");

            var info = new LicenseInfo
            {
                Key = licenseKey,
                Plan = ParsePlan(plan),
                Status = LicenseStatus.Valid,
                DeviceId = deviceId ?? string.Empty,
                Message = "License valid offline."
            };

            // Migrate to NexaPlay dir if it was loaded from GameHub
            if (targetPath == _gameHubFilePath)
            {
                AppendStoreTrace("Load migrating GameHub license into NexaPlay directory");
                _ = SaveAsync(info); // Fire and forget migration
            }

            AppendStoreTrace($"Load returning Valid plan={info.Plan}");
            return info;
        }
        catch (Exception ex)
        {
            AppendStoreTrace($"Load exception: {ex}");
            return null;
        }
    }

    public async Task SaveAsync(LicenseInfo info)
    {
        try
        {
            var stored = new StoredLicense
            {
                license_key = info.Key,
                device_id = info.DeviceId,
                plan = info.Plan.ToString().ToLowerInvariant(),
                activated_at = DateTime.UtcNow.ToString("o")
            };

            string json = JsonSerializer.Serialize(stored);
            string encrypted = Encrypt(json);

            await File.WriteAllTextAsync(_nexaPlayFilePath, encrypted);
        }
        catch { }
    }

    public async Task DeleteAsync()
    {
        try { if (File.Exists(_nexaPlayFilePath)) File.Delete(_nexaPlayFilePath); } catch { }
        try { if (File.Exists(_gameHubFilePath)) File.Delete(_gameHubFilePath); } catch { }
        await Task.CompletedTask;
    }

    private static LicensePlan ParsePlan(string? planStr)
    {
        return planStr?.ToLowerInvariant() switch
        {
            "premium" => LicensePlan.Premium,
            "standard" => LicensePlan.Standard,
            _ => LicensePlan.Standard
        };
    }

    // === AES ENCRYPTION ===
    private static string Encrypt(string plain)
    {
        using var aes = Aes.Create();
        aes.Key = Sha256(SECRET);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        byte[] inputBytes = Encoding.UTF8.GetBytes(plain);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

        byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    // === AES DECRYPTION ===
    private static string Decrypt(string encryptedBase64)
    {
        byte[] fullBytes = Convert.FromBase64String(encryptedBase64);

        using var aes = Aes.Create();
        aes.Key = Sha256(SECRET);

        byte[] iv = new byte[16];
        byte[] cipherBytes = new byte[fullBytes.Length - 16];

        Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
        Buffer.BlockCopy(fullBytes, 16, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static byte[] Sha256(string s)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
    }

    private void AppendStoreTrace(string message)
    {
        try
        {
            File.AppendAllText(_appLogPath, $"[{DateTime.Now:HH:mm:ss}] [LicenseStore] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}
