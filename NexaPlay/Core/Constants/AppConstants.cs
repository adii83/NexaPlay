namespace NexaPlay.Core.Constants;

public static class AppConstants
{
    public const string AppName = "NexaPlay";
    public const string AppVersion = "1.0.0";
    public const string AppDataFolder = "NexaPlay";

    // GitHub data sources
    public const string SteamDataUrl = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/steam_data.json.gz";
    public const string BypassGamesUrl  = "https://raw.githubusercontent.com/adii83/steam-metadata-archive/main/fix_games.json";

    // Online fix base URL
    public const string OnlineFixBaseUrl = "https://files.luatools.work/OnlineFix1/";

    // Supabase
    public const string SupabaseUrl     = "https://ghmzmvrjazvqiaufjrjt.supabase.co";
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImdobXptdnJqYXp2cWlhdWZqcmp0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ2MDI4NTUsImV4cCI6MjA4MDE3ODg1NX0.FyOhPNk9Yvc0g0Ki0W3ZEeJC1d3N5gv1WWXgyXsl0xg";

    // Cache TTLs
    public static readonly TimeSpan SteamDataCacheTtl  = TimeSpan.FromHours(12);
    public static readonly TimeSpan BypassGamesCacheTtl   = TimeSpan.FromHours(24);

    // Timeouts
    public static readonly TimeSpan LicenseOnlineTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan HttpDefaultTimeout   = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan HttpHeadTimeout      = TimeSpan.FromSeconds(15);

    // File/folder names
    public const string LicenseFileName        = "license.dat";
    public const string SteamDataCacheFileName = "github_raw_full.json";
    public const string BypassGamesCacheFileName  = "fix_games.json";
    public const string AppliedStateFileName   = "applied_state.json";
    public const string LogFileName            = "nexaplay.log";
    public const string FixLogPrefix           = "nexaplay-fix-log-";
}
