using Microsoft.Data.Sqlite;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Helpers;
using NexaPlay.Core.Models;
using System.Text.Json;

namespace NexaPlay.Infrastructure.Services;

public sealed class SqliteCatalogRepository : ICatalogRepository
{
    private readonly record struct BrowseCandidate(int AppId, int PriceNormalized);
    private sealed record BrowseBlock(IReadOnlyList<BrowseCandidate> Items, int TotalCount);
    private const int ShuffleBlockPages = 3;

    private const string EffectiveCatalogSql = """
        WITH effective AS (
            SELECT g.appid,
                   COALESCE(o.name, g.name) AS name,
                   COALESCE(o.normalized_name, g.normalized_name) AS normalized_name,
                   COALESCE(o.library_capsule, g.library_capsule) AS library_capsule,
                   COALESCE(o.genre, g.genre) AS genre,
                   COALESCE(o.price_normalized, g.price_normalized) AS price_normalized,
                   COALESCE(o.price_display, g.price_display) AS price_display,
                   COALESCE(o.protection, g.protection) AS protection,
                   COALESCE(o.tier, g.tier) AS tier,
                   g.pc_requirements
            FROM games g
            LEFT JOIN catalog_runtime_overrides o ON o.appid = g.appid
        )
        """;
    private static readonly string EffectiveBrowseCatalogSql = EffectiveCatalogSql.Replace(
        "FROM games g",
        "FROM games g INDEXED BY idx_games_price",
        StringComparison.Ordinal);

    private const string SelectColumns = """
        e.appid, e.name, e.normalized_name, e.library_capsule, e.genre,
        e.price_normalized, e.price_display, e.protection, e.tier, e.pc_requirements
        """;

    private readonly ICatalogBootstrapService _bootstrap;

    public SqliteCatalogRepository(ICatalogBootstrapService bootstrap)
    {
        _bootstrap = bootstrap;
    }

    public async Task<GameEntry?> GetByAppIdAsync(int appId, CancellationToken ct = default)
    {
        await _bootstrap.EnsureReadyAsync(ct);
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"{EffectiveCatalogSql} SELECT {SelectColumns} FROM effective e WHERE e.appid = @appid;";
        command.Parameters.AddWithValue("@appid", appId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadGame(reader) : null;
    }

    public async Task<IReadOnlyList<GameEntry>> GetByAppIdsAsync(IReadOnlyList<int> appIds, CancellationToken ct = default)
    {
        if (appIds.Count == 0)
            return Array.Empty<GameEntry>();

        await _bootstrap.EnsureReadyAsync(ct);
        var found = new Dictionary<int, GameEntry>();
        await using var connection = await OpenAsync(ct);

        foreach (var chunk in appIds.Distinct().Chunk(800))
        {
            await using var command = connection.CreateCommand();
            var names = new string[chunk.Length];
            for (var index = 0; index < chunk.Length; index++)
            {
                names[index] = $"@id{index}";
                command.Parameters.AddWithValue(names[index], chunk[index]);
            }

            command.CommandText = $"{EffectiveCatalogSql} SELECT {SelectColumns} FROM effective e WHERE e.appid IN ({string.Join(',', names)});";
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var game = ReadGame(reader);
                found[game.AppId] = game;
            }
        }

        return appIds.Where(found.ContainsKey).Select(appId => found[appId]).ToList();
    }

    public async Task<CatalogPage> QueryAsync(CatalogQuery query, CancellationToken ct = default)
    {
        await _bootstrap.EnsureReadyAsync(ct);
        await using var connection = await OpenAsync(ct);
        var where = BuildWhere(query);

        if (string.IsNullOrWhiteSpace(query.Search) && query.ApplyDefaultPriceFloor)
            return await QueryDailyBrowseAsync(connection, query, where, ct);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"{EffectiveCatalogSql} SELECT COUNT(*) FROM effective e {where.Sql};";
        AddParameters(countCommand, where.Parameters);
        var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(ct));

        await using var dataCommand = connection.CreateCommand();
        dataCommand.CommandText = $"""
            {EffectiveCatalogSql}
            SELECT {SelectColumns}
            FROM effective e
            {where.Sql}
            ORDER BY e.normalized_name, e.appid
            LIMIT @limit OFFSET @offset;
            """;
        AddParameters(dataCommand, where.Parameters);
        dataCommand.Parameters.AddWithValue("@limit", Math.Clamp(query.Limit, 1, 500));
        dataCommand.Parameters.AddWithValue("@offset", Math.Max(0, query.Offset));

        var items = new List<GameEntry>();
        await using var reader = await dataCommand.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(ReadGame(reader));

        return new CatalogPage(items, total);
    }

    private static async Task<CatalogPage> QueryDailyBrowseAsync(
        SqliteConnection connection,
        CatalogQuery query,
        (string Sql, Dictionary<string, object> Parameters) where,
        CancellationToken ct)
    {
        var limit = Math.Clamp(query.Limit, 1, 500);
        var offset = Math.Max(0, query.Offset);
        var blockSize = limit * ShuffleBlockPages;
        var blockIndex = offset / blockSize;
        var offsetInBlock = offset % blockSize;
        var block = await LoadPriceBlockAsync(connection, where, blockIndex, blockSize, ct);
        var ordered = OrderBlock(block.Items, query.OrderSeed, blockIndex);
        var selectedAppIds = ordered.Skip(offsetInBlock).Take(limit).Select(item => item.AppId).ToList();
        var items = await LoadByAppIdsAsync(connection, selectedAppIds, ct);
        var total = block.TotalCount;
        if (block.Items.Count == 0 && offset > 0)
            total = await CountAsync(connection, where, ct);
        return new CatalogPage(items, total);
    }

    private static async Task<BrowseBlock> LoadPriceBlockAsync(
        SqliteConnection connection,
        (string Sql, Dictionary<string, object> Parameters) where,
        int blockIndex,
        int blockSize,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            {EffectiveBrowseCatalogSql}
            SELECT e.appid, e.price_normalized, COUNT(*) OVER()
            FROM effective e
            {where.Sql}
            ORDER BY e.price_normalized DESC, e.appid ASC
            LIMIT @blockSize OFFSET @blockOffset;
            """;
        AddParameters(command, where.Parameters);
        command.Parameters.AddWithValue("@blockSize", blockSize);
        command.Parameters.AddWithValue("@blockOffset", blockIndex * blockSize);

        var items = new List<BrowseCandidate>(blockSize);
        var total = 0;
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new BrowseCandidate(reader.GetInt32(0), reader.GetInt32(1)));
            total = reader.GetInt32(2);
        }
        return new BrowseBlock(items, total);
    }

    private static async Task<int> CountAsync(
        SqliteConnection connection,
        (string Sql, Dictionary<string, object> Parameters) where,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"{EffectiveBrowseCatalogSql} SELECT COUNT(*) FROM effective e {where.Sql};";
        AddParameters(command, where.Parameters);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    private static IReadOnlyList<BrowseCandidate> OrderBlock(
        IReadOnlyList<BrowseCandidate> block,
        int daySeed,
        int blockIndex)
    {
        var ordered = new List<BrowseCandidate>(block.Count);
        foreach (var band in block.GroupBy(item => PriceBand(item.PriceNormalized)).OrderBy(group => group.Key))
        {
            var salt = ((ulong)(uint)daySeed << 32) ^ ((ulong)(uint)band.Key << 16) ^ (uint)blockIndex;
            ordered.AddRange(band.OrderBy(item => Mix64((uint)item.AppId ^ salt)).ThenBy(item => item.AppId));
        }
        return ordered;
    }

    private static async Task<IReadOnlyList<GameEntry>> LoadByAppIdsAsync(
        SqliteConnection connection,
        IReadOnlyList<int> appIds,
        CancellationToken ct)
    {
        if (appIds.Count == 0)
            return Array.Empty<GameEntry>();

        await using var command = connection.CreateCommand();
        var names = new string[appIds.Count];
        for (var index = 0; index < appIds.Count; index++)
        {
            names[index] = $"@selected{index}";
            command.Parameters.AddWithValue(names[index], appIds[index]);
        }
        command.CommandText = $"{EffectiveCatalogSql} SELECT {SelectColumns} FROM effective e WHERE e.appid IN ({string.Join(',', names)});";

        var found = new Dictionary<int, GameEntry>(appIds.Count);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var game = ReadGame(reader);
            found[game.AppId] = game;
        }
        return appIds.Where(found.ContainsKey).Select(appId => found[appId]).ToList();
    }

    private static int PriceBand(int price) => price switch
    {
        >= 350_000 => 0,
        >= 250_000 => 1,
        >= 180_000 => 2,
        >= 135_000 => 3,
        _ => 4
    };

    private static ulong Mix64(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    public async Task<int> GetVersionAsync(CancellationToken ct = default)
    {
        await _bootstrap.EnsureReadyAsync(ct);
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM catalog_meta WHERE key = 'catalog_version';";
        return int.TryParse(Convert.ToString(await command.ExecuteScalarAsync(ct)), out var version) ? version : 0;
    }

    public async Task ReplaceRuntimeOverridesAsync(
        IReadOnlyDictionary<int, CatalogRuntimeOverride> overrides,
        CancellationToken ct = default)
    {
        await _bootstrap.EnsureReadyAsync(ct);
        await using var connection = await OpenAsync(ct);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);

        await using (var clear = connection.CreateCommand())
        {
            clear.Transaction = transaction;
            clear.CommandText = "DELETE FROM catalog_runtime_overrides;";
            await clear.ExecuteNonQueryAsync(ct);
        }

        foreach (var pair in overrides)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO catalog_runtime_overrides
                    (appid, name, normalized_name, library_capsule, genre, price_normalized, price_display, protection, tier)
                VALUES
                    (@appid, @name, @normalizedName, @libraryCapsule, @genre, @priceNormalized, @priceDisplay, @protection, @tier);
                """;
            insert.Parameters.AddWithValue("@appid", pair.Key);
            AddNullable(insert, "@name", pair.Value.Name);
            AddNullable(insert, "@normalizedName", pair.Value.NormalizedName);
            AddNullable(insert, "@libraryCapsule", pair.Value.LibraryCapsule);
            AddNullable(insert, "@genre", pair.Value.GenreJson);
            AddNullable(insert, "@priceNormalized", pair.Value.PriceNormalized);
            AddNullable(insert, "@priceDisplay", pair.Value.PriceDisplay);
            AddNullable(insert, "@protection", pair.Value.Protection is null ? null : pair.Value.Protection.Value ? 1 : 0);
            AddNullable(insert, "@tier", pair.Value.Tier);
            await insert.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    public async Task ApplyPatchAsync(CatalogPatch patch, CancellationToken ct = default)
    {
        await _bootstrap.EnsureReadyAsync(ct);
        await using var connection = await OpenAsync(ct);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);

        var currentVersion = await ReadVersionAsync(connection, transaction, ct);
        if (patch.SchemaVersion != 1 || patch.FromVersion != currentVersion || patch.ToVersion != currentVersion + 1)
            throw new InvalidDataException($"Patch version mismatch. local={currentVersion}, from={patch.FromVersion}, to={patch.ToVersion}");

        foreach (var game in patch.Upsert)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO games
                    (appid, name, normalized_name, library_capsule, genre, price_normalized, price_display, protection, tier, pc_requirements)
                VALUES
                    (@appid, @name, @normalizedName, @libraryCapsule, @genre, @priceNormalized, @priceDisplay, @protection, @tier, @pcRequirements)
                ON CONFLICT(appid) DO UPDATE SET
                    name = excluded.name,
                    normalized_name = excluded.normalized_name,
                    library_capsule = excluded.library_capsule,
                    genre = excluded.genre,
                    price_normalized = excluded.price_normalized,
                    price_display = excluded.price_display,
                    protection = excluded.protection,
                    tier = excluded.tier,
                    pc_requirements = excluded.pc_requirements;
                """;
            command.Parameters.AddWithValue("@appid", game.AppId);
            command.Parameters.AddWithValue("@name", game.Name);
            command.Parameters.AddWithValue("@normalizedName", game.NormalizedName);
            AddNullable(command, "@libraryCapsule", game.LibraryCapsule);
            command.Parameters.AddWithValue("@genre", game.GenreJson);
            command.Parameters.AddWithValue("@priceNormalized", game.PriceNormalized);
            command.Parameters.AddWithValue("@priceDisplay", game.PriceDisplay);
            command.Parameters.AddWithValue("@protection", game.Protection ? 1 : 0);
            command.Parameters.AddWithValue("@tier", game.Tier);
            command.Parameters.AddWithValue("@pcRequirements", game.PcRequirementsJson);
            await command.ExecuteNonQueryAsync(ct);
        }

        foreach (var appId in patch.Delete.Distinct())
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM games WHERE appid = @appid;";
            command.Parameters.AddWithValue("@appid", appId);
            await command.ExecuteNonQueryAsync(ct);
        }

        await using (var version = connection.CreateCommand())
        {
            version.Transaction = transaction;
            version.CommandText = """
                INSERT INTO catalog_meta(key, value) VALUES('catalog_version', @version)
                ON CONFLICT(key) DO UPDATE SET value = excluded.value;
                """;
            version.Parameters.AddWithValue("@version", patch.ToVersion.ToString());
            await version.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection($"Data Source={_bootstrap.RuntimeDatabasePath};Mode=ReadWrite;Cache=Shared;Pooling=True");
        await connection.OpenAsync(ct);
        return connection;
    }

    private static (string Sql, Dictionary<string, object> Parameters) BuildWhere(CatalogQuery query)
    {
        var clauses = new List<string>();
        var parameters = new Dictionary<string, object>();
        var search = query.Search.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (int.TryParse(search, out var appId))
            {
                clauses.Add("e.appid = @searchAppId");
                parameters["@searchAppId"] = appId;
            }
            else
            {
                clauses.Add("e.normalized_name LIKE '%' || @searchName || '%'");
                parameters["@searchName"] = search.NormalizeForSearch();
            }
        }
        else if (query.ApplyDefaultPriceFloor)
        {
            clauses.Add("e.price_normalized >= 100000");
            clauses.Add("NULLIF(TRIM(e.library_capsule), '') IS NOT NULL");
        }

        if (query.Premium is not null)
        {
            clauses.Add("e.tier = @tier");
            parameters["@tier"] = query.Premium.Value ? "premium" : "standard";
        }

        if (query.Protection is not null)
        {
            clauses.Add("e.protection = @protection");
            parameters["@protection"] = query.Protection.Value ? 1 : 0;
        }

        if (query.Genres.Count > 0)
        {
            var genreClauses = new List<string>();
            foreach (var genre in query.Genres.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var parameterName = $"@genre{genreClauses.Count}";
                genreClauses.Add($"e.genre LIKE {parameterName}");
                parameters[parameterName] = $"%\"{genre.Replace("\"", "\"\"")}\"%";
            }
            clauses.Add($"({string.Join(" OR ", genreClauses)})");
        }

        return (clauses.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", clauses), parameters);
    }

    private static void AddParameters(SqliteCommand command, IReadOnlyDictionary<string, object> parameters)
    {
        foreach (var pair in parameters)
            command.Parameters.AddWithValue(pair.Key, pair.Value);
    }

    private static void AddNullable(SqliteCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static async Task<int> ReadVersionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT value FROM catalog_meta WHERE key = 'catalog_version';";
        return int.TryParse(Convert.ToString(await command.ExecuteScalarAsync(ct)), out var version) ? version : 0;
    }

    private static GameEntry ReadGame(SqliteDataReader reader)
    {
        var genreJson = reader.GetString(4);
        string genre;
        try
        {
            genre = string.Join(", ", JsonSerializer.Deserialize<string[]>(genreJson) ?? []);
        }
        catch (JsonException)
        {
            genre = genreJson;
        }

        return new GameEntry
        {
            AppId = reader.GetInt32(0),
            Name = reader.GetString(1),
            LibraryCapsuleUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
            Genre = genre,
            PriceNormalized = reader.GetInt32(5),
            PriceDisplay = reader.GetString(6),
            Protection = reader.GetInt32(7) != 0,
            CatalogTier = reader.GetString(8)
        };
    }
}
