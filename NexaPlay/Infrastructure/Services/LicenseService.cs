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
        if (_cached is not null) return _cached;
        var stored = _store.Load();
        if (stored is null)
        {
            _cached = new LicenseInfo { Status = LicenseStatus.Unknown };
            return _cached;
        }
        _log.Log("License", $"Loaded license: plan={stored.Plan} status={stored.Status}");
        _cached = stored;
        return stored;
    }

    public async Task<LicenseInfo> ActivateAsync(string licenseKey, CancellationToken ct = default)
    {
        _log.Log("License", $"Activating key (hidden)");
        var deviceId = GetDeviceId();
        var result = await ValidateOnlineAsync(licenseKey, deviceId, ct);
        if (result.IsValid)
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

            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var body = await resp.Content.ReadAsStringAsync(cts.Token);

            if (!resp.IsSuccessStatusCode)
            {
                _log.Log("License", $"HTTP error {resp.StatusCode}");
                return new LicenseInfo { Key = licenseKey, Status = LicenseStatus.Invalid, Message = $"Server error: {resp.StatusCode}", DeviceId = deviceId };
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
            var plan   = root.TryGetProperty("plan",   out var p) ? p.GetString() : null;
            var msg    = root.TryGetProperty("message", out var m) ? m.GetString()
                       : root.TryGetProperty("reason",  out var r) ? r.GetString() : null;

            var licStatus = status switch
            {
                "active" => LicenseStatus.Valid,
                "banned" => LicenseStatus.Banned,
                "invalid" or "error" => LicenseStatus.Invalid,
                _ => LicenseStatus.Invalid
            };

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
