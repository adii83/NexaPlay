namespace NexaPlay.Core.Constants;

public static class AppConstants
{
    public const string AppName = "NexaPlay";
    public const string AppVersion = "1.0.6";
    public const string AppDataFolder = "NexaPlay";
    public const string AppUpdateManifestUrl = "https://raw.githubusercontent.com/adii83/NexaPlay/main/NexaPlay/release/update-stable.json";
    public const string AppUpdateInstallerArguments = "";

    public const string MetadataRepositoryRawUrl = "https://raw.githubusercontent.com/adii83/Nexaplay-Metadata-Override/main";
    public const string CatalogManifestUrl = "https://meta.nexaplaymetadata.online/catalog/catalog_manifest.json";
    public const string CatalogBaseUrl = "https://meta.nexaplaymetadata.online/catalog/";
    public const string BypassGamesUrl = $"{MetadataRepositoryRawUrl}/fix_games.json";
    public const string NewFixGamesUrl = $"{MetadataRepositoryRawUrl}/new_fix_games.json";
    public const string SteamGamesUrl = $"{MetadataRepositoryRawUrl}/steam_games/steam_games.json";
    public const string PopularGamesUrl = $"{MetadataRepositoryRawUrl}/appid_populer.json";
    public const string NexaPlayOverrideUrl = $"{MetadataRepositoryRawUrl}/nexaplay_override.json";
    public const string YoutubeTutorialUrl = $"{MetadataRepositoryRawUrl}/youtube.json";
    public const string R2MetadataBaseUrl = "https://meta.nexaplaymetadata.online/Metadata";

    // The only authority for Online-Fix availability and package mapping.
    public const string OnlineFixCatalogUrl = "https://api.github.com/repos/adii83/Nexaplay-Metadata-Override/contents/online_fix.json?ref=main";

    // Supabase
    public const string SupabaseUrl     = "https://ghmzmvrjazvqiaufjrjt.supabase.co";
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImdobXptdnJqYXp2cWlhdWZqcmp0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ2MDI4NTUsImV4cCI6MjA4MDE3ODg1NX0.FyOhPNk9Yvc0g0Ki0W3ZEeJC1d3N5gv1WWXgyXsl0xg";

    // Timeouts
    public static readonly TimeSpan LicenseOnlineTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan HttpDefaultTimeout   = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan AppUpdateCheckCooldown = TimeSpan.FromHours(6);

    // File/folder names
    public const string LicenseFileName        = "license.dat";
    public const string BypassGamesCacheFileName  = "fix_games.json";
    public const string NewFixGamesCacheFileName = "new_fix_games.json";
    public const string SteamGamesCacheFileName   = "steam_games.json";
    public const string AppliedStateFileName   = "applied_state.json";
    public const string LogFileName            = "nexaplay.log";
    public const string AppUpdateStateFileName = "app_update_state.json";
    public const string AccountReportLimitFileName = "account_report_limits.json";
    public const string FixLogPrefix           = "nexaplay-fix-log-";
    public const string NexaPlayOverrideCacheFileName = "nexaplay_override.json";
    public const string RetiredLibraryCapsuleCacheFileName = "library_capsule.json";
    public const string RetiredLibraryCapsuleGzipCacheFileName = "library_capsule.json.gz";
    public const string YoutubeTutorialCacheFileName = "youtube_tutorial.json";
    public const string YoutubeTutorialEtagFileName = "youtube_tutorial.etag";
    public const string AppUpdateDownloadsFolderName = "updates";
    public const string CatalogDataFolderName = "Data";
    public const string CatalogDatabaseFileName = "catalog.sqlite";
    public const string CatalogSeedRelativePath = "data\\catalog\\catalog_seed.sqlite";
    public const string LegacyGamesFilterCacheFileName = "games_filter_index_cache_v3.json";

    public static string LocalAppDataPath { get; } = ResolvePhysicalLocalAppDataPath();

    public static string AppDataPath { get; } = Path.Combine(LocalAppDataPath, AppDataFolder);

    public static string CatalogDatabasePath => Path.Combine(
        AppDataPath,
        CatalogDataFolderName,
        CatalogDatabaseFileName);

    private static string ResolvePhysicalLocalAppDataPath()
    {
        var path = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(path))
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Local AppData path is unavailable.");

        path = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        if (!OperatingSystem.IsWindows() || path.StartsWith(@"\\?\", StringComparison.Ordinal))
            return path;
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
            return @"\\?\UNC\" + path[2..];
        return @"\\?\" + path;
    }
}
