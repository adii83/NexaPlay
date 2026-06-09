namespace NexaPlay.Core.Constants;

public static class AppConstants
{
    public const string AppName = "NexaPlay";
    public const string AppVersion = "1.0.1";
    public const string AppDataFolder = "NexaPlay";
    public const string AppUpdateManifestUrl = "https://raw.githubusercontent.com/adii83/NexaPlay/main/NexaPlay/release/update-stable.json";
    public const string AppUpdateInstallerArguments = "";

    // Legacy GitHub data sources
    public const string SteamDataUrl = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/steam_data.json.gz";
    public const string SteamDataJsonUrl = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/steam_data.json";
    public const string BypassGamesUrl  = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/fix_games.json";
    public const string NewFixGamesUrl = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/new_fix_games.json";
    public const string SteamGamesUrl = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/steam_games/steam_games.json";
    public const string PopularGamesUrl = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/appid_populer.json";
    public const string OverrideDataUrl = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/override_data.json";

    // NexaPlay-specific override (separate repo)
    public const string NexaPlayOverrideUrl = "https://raw.githubusercontent.com/adii83/Nexaplay-Metadata-Override/main/nexaplay_override.json";
    public const string YoutubeTutorialUrl = "https://raw.githubusercontent.com/adii83/Nexaplay-Metadata-Override/main/youtube.json";

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
    public const string FixLogPrefix           = "nexaplay-fix-log-";
    public const string OverrideDataCacheFileName = "override_data.json";
    public const string UserOverrideDataCacheFileName = "user_override_data.json";
    public const string NexaPlayOverrideCacheFileName = "nexaplay_override.json";
    public const string YoutubeTutorialCacheFileName = "youtube_tutorial.json";
    public const string YoutubeTutorialEtagFileName = "youtube_tutorial.etag";
    public const string AppUpdateDownloadsFolderName = "updates";
}
