using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Persistence;
using NexaPlay.Infrastructure.Platform;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public sealed class LicenseService : ILicenseService
{
    private readonly LicenseStore _store = new();
    private readonly IAppLogService _log;
    private LicenseInfo? _cached;

    public LicenseService(IAppLogService log) => _log = log;

    public string GetDeviceId() => DeviceIdHelper.GetDeviceId();

    public async Task<LicenseInfo> LoadAsync()
    {
        try
        {
            _log.Log("License", $"LoadAsync entered. cache_hit={_cached is not null}");
            if (_cached is not null)
            {
                _log.Log("License", $"LoadAsync returning cached license status={_cached.Status} valid={_cached.IsValid}");
                return _cached;
            }

            _log.Log("License", "LoadAsync reading license store on background thread...");
            var stored = await Task.Run(() => _store.Load());
            if (stored is null)
            {
                _log.Log("License", "LoadAsync store returned null");
                _cached = new LicenseInfo { Status = LicenseStatus.Unknown };
                return _cached;
            }

            _log.Log("License", $"Loaded license: plan={stored.Plan} status={stored.Status}");
            _cached = stored;
            return stored;
        }
        catch (Exception ex)
        {
            _log.Log("License", $"LoadAsync exception: {ex}");
            _cached = new LicenseInfo
            {
                Status = LicenseStatus.Invalid,
                Message = "Gagal membaca license lokal."
            };
            return _cached;
        }
    }

    public async Task<LicenseInfo> ActivateAsync(string licenseKey, CancellationToken ct = default)
    {
        _log.Log("License", $"Activating key (hidden)");
        var deviceId = await Task.Run(() => GetDeviceId(), ct);
        var result = await ValidateOnlineAsync(licenseKey, deviceId, ct);
        if (result.IsValid)
        {
            await SaveAsync(result);
        }
        return result;
    }

    public async Task<LicenseInfo> ValidateExistingAsync(CancellationToken ct = default)
    {
        var existing = await LoadAsync();
        if (!existing.IsValid || string.IsNullOrEmpty(existing.Key))
        {
            return existing;
        }

        var currentDeviceId = await Task.Run(() => GetDeviceId(), ct);
        _log.Log("License", $"ValidateExistingAsync using current device id. stored_device_present={!string.IsNullOrWhiteSpace(existing.DeviceId)}");

        if (!string.IsNullOrWhiteSpace(existing.DeviceId) &&
            !string.Equals(existing.DeviceId, currentDeviceId, StringComparison.Ordinal))
        {
            _log.Log("License", "ValidateExistingAsync detected offline device mismatch before online validation");
            return new LicenseInfo
            {
                Key = existing.Key,
                Plan = existing.Plan,
                Status = LicenseStatus.DeviceMismatch,
                DeviceId = currentDeviceId,
                Message = "License terikat dengan perangkat lain."
            };
        }

        var result = await ValidateOnlineAsync(existing.Key, currentDeviceId, ct);
        
        // If banned, reset, or not found during background check, clean up
        if (result.Status == LicenseStatus.Banned || 
            result.Status == LicenseStatus.Reset || 
            result.Status == LicenseStatus.NotFound)
        {
            _log.Log("License", $"Background check returned {result.Status}, cleaning up offline license...");
            await DeactivateAsync();
        }
        // If successful, update cache/file just in case plan changed
        else if (result.IsValid)
        {
            await SaveAsync(result);
        }

        return result;
    }

    public async Task<LicenseInfo> ValidateOnlineAsync(string licenseKey, string deviceId, CancellationToken ct = default)
    {
        _log.Log("License", "Starting online validation...");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(AppConstants.LicenseOnlineTimeout);

        try
        {
            var url = $"{AppConstants.SupabaseUrl}/rest/v1/rpc/activate_license";
            var payload = JsonSerializer.Serialize(new { p_license_key = licenseKey, p_device_id = deviceId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var http = new HttpClient { Timeout = AppConstants.LicenseOnlineTimeout };
            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            req.Headers.Add("apikey", AppConstants.SupabaseAnonKey);
            req.Headers.Add("Authorization", $"Bearer {AppConstants.SupabaseAnonKey}");

            // Prefer representations to get JSON back
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            content.Headers.Add("Prefer", "return=representation");

            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var body = await resp.Content.ReadAsStringAsync(cts.Token);

            if (!resp.IsSuccessStatusCode)
            {
                _log.Log("License", $"HTTP error {resp.StatusCode}: {body}");
                return new LicenseInfo { Key = licenseKey, Status = LicenseStatus.Invalid, Message = $"Server error: {resp.StatusCode}", DeviceId = deviceId };
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
            var plan   = root.TryGetProperty("plan",   out var p) ? p.GetString() : null;
            var msg    = root.TryGetProperty("message", out var m) ? m.GetString()
                       : root.TryGetProperty("reason",  out var r) ? r.GetString() : null;

            var licStatus = LicenseStatus.Unknown;
            
            if (status == "success")
            {
                licStatus = LicenseStatus.Valid;
            }
            else if (status == "error")
            {
                string msgLower = msg?.ToLowerInvariant() ?? "";
                
                if (msgLower.Contains("banned") || msgLower.Contains("dibanned"))
                {
                    licStatus = LicenseStatus.Banned;
                }
                else if (msgLower.Contains("not_found") || msgLower.Contains("license tidak ditemukan"))
                {
                    licStatus = LicenseStatus.NotFound;
                }
                else if (msgLower.Contains("wrong_device") || msgLower.Contains("device berbeda"))
                {
                    licStatus = LicenseStatus.DeviceMismatch;
                }
                else if (msgLower.Contains("reset"))
                {
                    licStatus = LicenseStatus.Reset;
                }
                else
                {
                    licStatus = LicenseStatus.Invalid;
                }
            }
            else
            {
                licStatus = LicenseStatus.Invalid;
            }

            var licPlan = plan?.ToLowerInvariant() switch
            {
                "premium" => LicensePlan.Premium,
                "standard" => LicensePlan.Standard,
                _ => LicensePlan.Standard
            };

            _log.Log("License", $"Validation result: status={licStatus} plan={licPlan}");
            return new LicenseInfo { Key = licenseKey, Plan = licPlan, Status = licStatus, DeviceId = deviceId, Message = msg };
        }
        catch (OperationCanceledException)
        {
            _log.Log("License", "Online validation timed out — using offline mode");
            return new LicenseInfo { Key = licenseKey, Status = LicenseStatus.Offline, Message = "Network timeout", DeviceId = deviceId };
        }
        catch (Exception ex)
        {
            _log.Log("License", $"Online validation error: {ex.Message}");
            return new LicenseInfo { Key = licenseKey, Status = LicenseStatus.NetworkError, Message = ex.Message, DeviceId = deviceId };
        }
    }

    public async Task SaveAsync(LicenseInfo info)
    {
        _cached = info;
        await _store.SaveAsync(info);
        _log.Log("License", $"License saved: plan={info.Plan}");
    }

    public async Task DeactivateAsync()
    {
        _cached = null;
        await _store.DeleteAsync();
        _log.Log("License", "License deactivated");
    }
}
