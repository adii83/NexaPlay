namespace NexaPlay.Core.Constants;

public static class AppConstants
{
    public const string AppName = "NexaPlay";
    public const string AppVersion = "1.0.4";
    public const string AppDataFolder = "NexaPlay";
    public const string AppUpdateManifestUrl = "https://raw.githubusercontent.com/adii83/NexaPlay/main/NexaPlay/release/update-stable.json";
    public const string AppUpdateInstallerArguments = "";

    public const string MetadataRepositoryRawUrl = "https://raw.githubusercontent.com/adii83/Nexaplay-Metadata-Override/main";
    public const string CatalogManifestUrl = $"{MetadataRepositoryRawUrl}/catalog_manifest.json";
    public const string SteamDataUrl = $"{MetadataRepositoryRawUrl}/steam_data.json.gz";
    public const string SteamDataJsonUrl = $"{MetadataRepositoryRawUrl}/steam_data.json";
    public const string BypassGamesUrl = $"{MetadataRepositoryRawUrl}/fix_games.json";
    public const string NewFixGamesUrl = $"{MetadataRepositoryRawUrl}/new_fix_games.json";
    public const string SteamGamesUrl = $"{MetadataRepositoryRawUrl}/steam_games/steam_games.json";
    public const string PopularGamesUrl = $"{MetadataRepositoryRawUrl}/appid_populer.json";
    public const string OverrideDataUrl = $"{MetadataRepositoryRawUrl}/override_data.json";
    public const string NexaPlayOverrideUrl = $"{MetadataRepositoryRawUrl}/nexaplay_override.json";
    public const string LibraryCapsuleIndexUrl = $"{MetadataRepositoryRawUrl}/library_capsule.json";
    public const string LibraryCapsuleIndexGzipUrl = $"{MetadataRepositoryRawUrl}/library_capsule.json.gz";
    public const string YoutubeTutorialUrl = $"{MetadataRepositoryRawUrl}/youtube.json";
    public const string NewGamesUrl = $"{MetadataRepositoryRawUrl}/new_games.json";
    public const string R2MetadataBaseUrl = "https://meta.nexaplaymetadata.online/Metadata";

    // Online fix base URL
    public const string OnlineFixBaseUrl = "https://files.luatools.work/OnlineFix1/";
    public const string OnlineFixFallbackUrl = "https://github.com/madoiscool/lt_api_links/releases/download/unsteam/Win64.zip";

    // Supabase
    public const string SupabaseUrl     = "https://ghmzmvrjazvqiaufjrjt.supabase.co";
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImdobXptdnJqYXp2cWlhdWZqcmp0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ2MDI4NTUsImV4cCI6MjA4MDE3ODg1NX0.FyOhPNk9Yvc0g0Ki0W3ZEeJC1d3N5gv1WWXgyXsl0xg";

    // Cache TTLs
    public static readonly TimeSpan SafetyNetTtl = TimeSpan.FromHours(24);

    // Timeouts
    public static readonly TimeSpan LicenseOnlineTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan HttpDefaultTimeout   = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan HttpHeadTimeout      = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan AppUpdateCheckCooldown = TimeSpan.FromHours(6);

    // File/folder names
    public const string LicenseFileName        = "license.dat";
    public const string SteamDataCacheFileName = "github_raw_full.json";
    public const string BypassGamesCacheFileName  = "fix_games.json";
    public const string NewFixGamesCacheFileName = "new_fix_games.json";
    public const string SteamGamesCacheFileName   = "steam_games.json";
    public const string AppliedStateFileName   = "applied_state.json";
    public const string LogFileName            = "nexaplay.log";
    public const string AppUpdateStateFileName = "app_update_state.json";
    public const string AccountReportLimitFileName = "account_report_limits.json";
    public const string FixLogPrefix           = "nexaplay-fix-log-";
    public const string OverrideDataCacheFileName = "override_data.json";
    public const string UserOverrideDataCacheFileName = "user_override_data.json";
    public const string NexaPlayOverrideCacheFileName = "nexaplay_override.json";
    public const string LibraryCapsuleIndexCacheFileName = "library_capsule.json";
    public const string LibraryCapsuleIndexGzipCacheFileName = "library_capsule.json.gz";
    public const string YoutubeTutorialCacheFileName = "youtube_tutorial.json";
    public const string YoutubeTutorialEtagFileName = "youtube_tutorial.etag";
    public const string NewGamesCacheFileName = "new_games.json";
    public const string NewGamesEtagFileName = "new_games.etag";
    public const string NewGamesCatalogFileName = "new_games_catalog.json";
    public const string CatalogManifestCacheFileName = "catalog_manifest.json";
    public const string CatalogManifestEtagFileName = "catalog_manifest.etag";
    public const string AppUpdateDownloadsFolderName = "updates";
}
