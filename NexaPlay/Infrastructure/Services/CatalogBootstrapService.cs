using Microsoft.Data.Sqlite;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Constants;

namespace NexaPlay.Infrastructure.Services;

public sealed class CatalogBootstrapService : ICatalogBootstrapService
{
    private static readonly string[] RequiredColumns =
    [
        "appid", "name", "normalized_name", "library_capsule", "genre",
        "price_normalized", "price_display", "protection", "tier", "pc_requirements"
    ];

    private readonly IAppLogService _log;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _ready;

    public string BundledSeedPath { get; }
    public string RuntimeDatabasePath { get; }

    public CatalogBootstrapService(IAppLogService log)
        : this(
            log,
            Path.Combine(AppContext.BaseDirectory, AppConstants.CatalogSeedRelativePath),
            AppConstants.CatalogDatabasePath)
    {
    }

    public CatalogBootstrapService(IAppLogService log, string bundledSeedPath, string runtimeDatabasePath)
    {
        _log = log;
        BundledSeedPath = bundledSeedPath;
        RuntimeDatabasePath = runtimeDatabasePath;
    }

    public async Task EnsureReadyAsync(CancellationToken ct = default)
    {
        if (_ready)
            return;

        await _gate.WaitAsync(ct);
        try
        {
            if (_ready)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(RuntimeDatabasePath)!);

            if (File.Exists(RuntimeDatabasePath))
            {
                try
                {
                    await ValidateAsync(RuntimeDatabasePath, ct);
                    await EnsureRuntimeSchemaAsync(ct);
                    DeleteLegacyFilterCache();
                    _ready = true;
                    return;
                }
                catch (Exception ex) when (ex is SqliteException or InvalidDataException)
                {
                    var corruptPath = Path.Combine(
                        Path.GetDirectoryName(RuntimeDatabasePath)!,
                        $"catalog.corrupt.{DateTime.UtcNow:yyyyMMddHHmmssfff}.{Guid.NewGuid():N}.sqlite");
                    File.Move(RuntimeDatabasePath, corruptPath);
                    _log.Log("Catalog", $"Invalid runtime catalog preserved at {corruptPath}: {ex.Message}");
                }
            }

            if (!File.Exists(BundledSeedPath))
                throw new FileNotFoundException("Bundled catalog seed was not found.", BundledSeedPath);

            var bootstrapPath = RuntimeDatabasePath + ".bootstrap";
            File.Delete(bootstrapPath);

            try
            {
                File.Copy(BundledSeedPath, bootstrapPath);
                await ValidateAsync(bootstrapPath, ct);
                File.Move(bootstrapPath, RuntimeDatabasePath);
            }
            finally
            {
                File.Delete(bootstrapPath);
            }

            await EnsureRuntimeSchemaAsync(ct);
            DeleteLegacyFilterCache();
            _ready = true;
            _log.Log("Catalog", $"Runtime catalog bootstrapped from bundled seed: {RuntimeDatabasePath}");
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task ValidateAsync(string path, CancellationToken ct)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        await using var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(ct);

        await using (var quickCheck = connection.CreateCommand())
        {
            quickCheck.CommandText = "PRAGMA quick_check;";
            var result = Convert.ToString(await quickCheck.ExecuteScalarAsync(ct));
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SQLite quick_check failed: {result}");
        }

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var schema = connection.CreateCommand())
        {
            schema.CommandText = "PRAGMA table_info(games);";
            await using var reader = await schema.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                columns.Add(reader.GetString(1));
        }

        if (RequiredColumns.Any(column => !columns.Contains(column)))
            throw new InvalidDataException("SQLite games table is missing required columns.");

        await using (var count = connection.CreateCommand())
        {
            count.CommandText = "SELECT COUNT(*) FROM games;";
            if (Convert.ToInt64(await count.ExecuteScalarAsync(ct)) <= 0)
                throw new InvalidDataException("SQLite games table is empty.");
        }

        await using var version = connection.CreateCommand();
        version.CommandText = "SELECT value FROM catalog_meta WHERE key = 'catalog_version';";
        if (!int.TryParse(Convert.ToString(await version.ExecuteScalarAsync(ct)), out var catalogVersion) || catalogVersion <= 0)
            throw new InvalidDataException("SQLite catalog_version metadata is missing or invalid.");
    }

    private async Task EnsureRuntimeSchemaAsync(CancellationToken ct)
    {
        await using var connection = new SqliteConnection($"Data Source={RuntimeDatabasePath};Mode=ReadWrite");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS catalog_runtime_overrides (
                appid INTEGER PRIMARY KEY,
                name TEXT NULL,
                normalized_name TEXT NULL,
                library_capsule TEXT NULL,
                genre TEXT NULL,
                price_normalized INTEGER NULL,
                price_display TEXT NULL,
                protection INTEGER NULL CHECK(protection IS NULL OR protection IN (0,1)),
                tier TEXT NULL CHECK(tier IS NULL OR tier IN ('standard','premium'))
            );
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void DeleteLegacyFilterCache()
    {
        try
        {
            File.Delete(Path.Combine(
                AppConstants.AppDataPath,
                "runtime_catalog_sources",
                AppConstants.LegacyGamesFilterCacheFileName));
        }
        catch
        {
        }
    }
}
