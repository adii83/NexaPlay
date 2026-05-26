using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class BypassGameDetailViewModel : ObservableObject
{
    private readonly IMetadataService _metadata;
    private readonly ISteamStoreService _storeService;
    private readonly IBypassGamesDataService _bypassGamesData;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly IAppLogService _log;
    private readonly INavigationService _nav;

    [ObservableProperty] public partial GameEntry? Game { get; set; }
    [ObservableProperty] public partial GameDetailEntry? Detail { get; set; }
    [ObservableProperty] public partial bool IsDetailLoading { get; set; }
    [ObservableProperty] public partial bool IsDetailAvailable { get; set; }

    [ObservableProperty] public partial string HeroBackgroundUrl { get; set; }
    [ObservableProperty] public partial string GameIconUrl { get; set; }
    [ObservableProperty] public partial FixEntry? BypassEntry { get; set; }
    [ObservableProperty] public partial string CoverArtUrl { get; set; }

    public IReadOnlyList<string> GenreTags => BuildGenreTags(Game?.Genre);
    public string DisplayShortDescription => Detail?.ShortDescription ?? Game?.ShortDescription ?? string.Empty;
    public string DisplayDeveloper => Detail?.Developers.Count > 0 ? string.Join(", ", Detail.Developers) : Game?.DeveloperDisplay ?? string.Empty;
    public string DisplayPublisher => Detail?.Publishers.Count > 0 ? string.Join(", ", Detail.Publishers) : Game?.PublisherDisplay ?? string.Empty;
    public string DisplayReleaseDate => Detail?.ReleaseDate ?? Game?.ReleaseDate ?? string.Empty;
    public string DisplayPrice => Game?.PriceNormalized > 0 ? $"Rp {Game.PriceNormalized:N0}" : "Free";
    public bool IsPremiumGame => Game?.IsPremium == true;
    public bool ShowAktivasiOfflineBadge => BypassEntry?.AktivasiOffline == true;
    public bool ShowSteamSharingBadge => BypassEntry?.Category == GameCategory.SteamSharing;
    public bool ShowThirdPartySection => BypassEntry is not null && !BypassEntry.IsSteamType;
    public bool ShowSteamSection => BypassEntry is not null && BypassEntry.IsSteamType;

    public string SteamUsername => BypassEntry?.Username ?? string.Empty;
    public string SteamPassword => BypassEntry?.Password ?? string.Empty;

    private int _loadVersion;

    partial void OnGameChanged(GameEntry? value) => NotifyStatusProperties();

    public BypassGameDetailViewModel(
        IMetadataService metadata,
        ISteamStoreService storeService,
        IBypassGamesDataService bypassGamesData,
        INexaPlayOverrideService nexaPlayOverride,
        IAppLogService log,
        INavigationService nav)
    {
        _metadata = metadata;
        _storeService = storeService;
        _bypassGamesData = bypassGamesData;
        _nexaPlayOverride = nexaPlayOverride;
        _log = log;
        _nav = nav;

        HeroBackgroundUrl = string.Empty;
        GameIconUrl = string.Empty;
        CoverArtUrl = string.Empty;
    }

    public async Task LoadAsync(int appId, FixEntry? preferredBypassEntry, CancellationToken ct = default)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        ct.ThrowIfCancellationRequested();

        var baseGame = await _metadata.GetMetadataAsync(appId, ct);
        ct.ThrowIfCancellationRequested();
        if (loadVersion != _loadVersion) return;

        Game = baseGame;
        BypassEntry = preferredBypassEntry is not null && preferredBypassEntry.AppId == appId
            ? preferredBypassEntry
            : await ResolveBypassEntryAsync(appId, ct);
        GameIconUrl = Game?.IconImageUrl
            ?? Game?.HeaderImageUrl
            ?? string.Empty;
        OnPropertyChanged(nameof(GenreTags));
        NotifyDisplayProperties();

        IsDetailLoading = true;
        IsDetailAvailable = false;
        try
        {
            var fetched = await _storeService.GetDetailAsync(appId, ct);
            ct.ThrowIfCancellationRequested();
            if (loadVersion != _loadVersion) return;

            Detail = fetched;
            IsDetailAvailable = Detail is not null;

            NotifyDisplayProperties();
            var heroOverride = (await _nexaPlayOverride.GetCatalogOverrideAsync(appId))?.LibraryHero2x;
            HeroBackgroundUrl = heroOverride
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "library_hero_2x")
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "library_hero")
                ?? Detail?.SgdbHeroUrl
                ?? Game?.LibraryHero2xUrl
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "background_raw")
                ?? Game?.BackgroundRawImageUrl
                ?? Detail?.BackgroundImageUrl
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "header")
                ?? Game?.HeaderImageUrl
                ?? string.Empty;

            var iconOverride = (await _nexaPlayOverride.GetCatalogOverrideAsync(appId))?.Icon;
            GameIconUrl = iconOverride
                ?? ReadFirstAssetUrl(Detail?.RawMetadataJson, "icon")
                ?? Detail?.SgdbIconUrl
                ?? Game?.IconImageUrl
                ?? Game?.HeaderImageUrl
                ?? string.Empty;

            var overrideCover = (await _nexaPlayOverride.GetCatalogOverrideAsync(appId))?.LibraryCapsule;
            var apiCover = Detail?.LibraryCapsuleUrl;
            var sgdbGrid = Detail?.SgdbGridUrl;

            // Cover art for default 3rd-party section (library capsule portrait 2:3)
            CoverArtUrl = !string.IsNullOrWhiteSpace(BypassEntry?.PosterUrl) ? BypassEntry.PosterUrl
                : !string.IsNullOrWhiteSpace(overrideCover) ? overrideCover
                : !string.IsNullOrWhiteSpace(apiCover) ? apiCover
                : !string.IsNullOrWhiteSpace(sgdbGrid) ? sgdbGrid
                : !string.IsNullOrWhiteSpace(Game?.LibraryCapsuleUrl) ? Game.LibraryCapsuleUrl
                : !string.IsNullOrWhiteSpace(Game?.HeaderImageUrl) ? Game.HeaderImageUrl
                : appId > 0 ? $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/library_600x900_2x.jpg"
                : string.Empty;

            OnPropertyChanged(nameof(CoverArtUrl));
            OnPropertyChanged(nameof(ShowSteamSection));
            OnPropertyChanged(nameof(ShowThirdPartySection));
            OnPropertyChanged(nameof(SteamUsername));
            OnPropertyChanged(nameof(SteamPassword));
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    public Task LoadAsync(int appId, CancellationToken ct = default) =>
        LoadAsync(appId, preferredBypassEntry: null, ct);

    [RelayCommand]
    private void StartBypassGame()
    {
        // Placeholder action for the default/no-status layout stage.
        // Final bypass execution flow can be wired in the next batch.
        _log.Log("BypassDetail", $"StartBypassGame clicked for appid={Game?.AppId}");
    }

    [RelayCommand]
    private void CopySteamUsername()
    {
        if (!string.IsNullOrEmpty(SteamUsername))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(SteamUsername);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    [RelayCommand]
    private void CopySteamPassword()
    {
        if (!string.IsNullOrEmpty(SteamPassword))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(SteamPassword);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    [RelayCommand]
    private async Task ReportAccountAsync()
    {
        // Navigasi ke tautan pelaporan / WhatsApp admin
        var uri = new Uri("https://wa.me/6281234567890?text=Lapor%20akun%20bermasalah%20di%20Game:%20" + Uri.EscapeDataString(BypassEntry?.Title ?? "Unknown"));
        await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private static IReadOnlyList<string> BuildGenreTags(string? rawGenre)
    {
        if (string.IsNullOrWhiteSpace(rawGenre))
            return Array.Empty<string>();

        return rawGenre
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();
    }

    private void NotifyDisplayProperties()
    {
        OnPropertyChanged(nameof(DisplayShortDescription));
        OnPropertyChanged(nameof(DisplayDeveloper));
        OnPropertyChanged(nameof(DisplayPublisher));
        OnPropertyChanged(nameof(DisplayReleaseDate));
        OnPropertyChanged(nameof(DisplayPrice));
        NotifyStatusProperties();
    }

    private void NotifyStatusProperties()
    {
        OnPropertyChanged(nameof(IsPremiumGame));
        OnPropertyChanged(nameof(ShowAktivasiOfflineBadge));
        OnPropertyChanged(nameof(ShowSteamSharingBadge));
        OnPropertyChanged(nameof(ShowThirdPartySection));
    }

    private async Task<FixEntry?> ResolveBypassEntryAsync(int appId, CancellationToken ct)
    {
        var steam = await _bypassGamesData.GetSteamGamesAsync(ct);
        var steamMatch = steam.FirstOrDefault(x => x.AppId == appId);
        if (steamMatch is not null)
            return steamMatch;

        var direct = await _bypassGamesData.GetFixAsync(appId, ct);
        if (direct is not null)
            return direct;

        var newer = await _bypassGamesData.GetNewFixesAsync(ct);
        return newer.FirstOrDefault(x => x.AppId == appId);
    }

    private static string? ReadFirstAssetUrl(string? rawMetadataJson, string assetKey)
    {
        if (string.IsNullOrWhiteSpace(rawMetadataJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawMetadataJson);
            JsonElement node;
            if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                assets.TryGetProperty(assetKey, out var nestedNode))
            {
                node = nestedNode;
            }
            else if (doc.RootElement.TryGetProperty(assetKey, out var rootNode))
            {
                node = rootNode;
            }
            else
            {
                return null;
            }

            if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("url", out var url) &&
                        url.ValueKind == JsonValueKind.String)
                        return url.GetString();
                }
            }
        }
        catch { }

        return null;
    }
}
