using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Infrastructure.Persistence;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NexaPlay.Infrastructure.Services;

public sealed class TelegramAccountReportService : IAccountReportService
{
    private const string BotToken = "8122332462:AAFwFdGrIA2w5WaEOWetf-bCSkz0luJ_KSo";
    private const string ChatId = "8491267458";

    private readonly AccountReportLimitStore _limitStore;
    private readonly IAppLogService _log;
    private readonly HttpClient _http = new() { Timeout = AppConstants.HttpDefaultTimeout };

    public TelegramAccountReportService(AccountReportLimitStore limitStore, IAppLogService log)
    {
        _limitStore = limitStore;
        _log = log;
    }

    public async Task<AccountReportResult> ReportAsync(FixEntry entry, CancellationToken ct = default)
    {
        if (!entry.IsSteamType
            || entry.AppId <= 0
            || string.IsNullOrWhiteSpace(entry.Title)
            || string.IsNullOrWhiteSpace(entry.Username)
            || string.IsNullOrWhiteSpace(entry.Password))
        {
            return new(false, "Laporkan Akun", "Data akun Steam tidak ditemukan. Coba buka ulang halaman ini.");
        }

        if (!await _limitStore.CanReportAsync(entry.AppId, ct))
        {
            return new(false, "Batas Laporan Tercapai", "Anda sudah mengirim 2 laporan untuk akun ini dalam 24 jam terakhir. Tunggu hingga besok untuk mengirim laporan baru.");
        }

        var text = string.Join('\n',
            "🚨 Laporan Akun Steam",
            $"AppID : {entry.AppId} ",
            $"Game  : {entry.Title} ",
            $"User  : {entry.Username} ",
            $"Pass  : {entry.Password} ",
            "Status: Tidak bisa login (dilaporkan user)");

        try
        {
            using var response = await _http.PostAsJsonAsync(
                $"https://api.telegram.org/bot{BotToken}/sendMessage",
                new { chat_id = ChatId, text },
                ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TelegramResponse>(cancellationToken: ct);
            if (result?.Ok != true)
            {
                throw new HttpRequestException("Telegram menolak laporan.");
            }

            await _limitStore.RecordSuccessAsync(entry.AppId, ct);
            _log.Log("AccountReport", $"Telegram report sent appid={entry.AppId} category={entry.Category}");
            return new(true, "Laporan Terkirim", "Laporan akun sudah dikirim. Admin akan segera memeriksa.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _log.Log("AccountReport", $"Telegram report failed appid={entry.AppId} category={entry.Category} error={ex.GetType().Name}");
            return new(false, "Laporan Gagal", "Gagal mengirim laporan. Mohon coba lagi beberapa saat.");
        }
    }

    private sealed class TelegramResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; init; }
    }
}
