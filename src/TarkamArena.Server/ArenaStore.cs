using System.Data.Common;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace TarkamArena.Server;

public sealed record PlayerRecord(string Name, int Rating, int Wins, int Losses, string? Clan);

public sealed record ClanRecord(string Name, string Tag, int Members, int WarPoints);

/// <summary>Persistent online data: ratings, clans and clan war points.</summary>
public interface IArenaStore
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<PlayerRecord> GetOrCreatePlayerAsync(string name, CancellationToken ct = default);
    Task SavePlayerAsync(PlayerRecord player, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerRecord>> TopPlayersAsync(int count, CancellationToken ct = default);
    Task<ClanRecord?> GetClanAsync(string name, CancellationToken ct = default);
    Task<ClanRecord> CreateClanAsync(string name, string tag, CancellationToken ct = default);
    Task AddClanPointsAsync(string clan, int points, CancellationToken ct = default);
    Task<IReadOnlyList<ClanRecord>> ClanStandingsAsync(CancellationToken ct = default);
}

/// <summary>
/// ADO.NET store that runs on PostgreSQL in production and SQLite for local development. The SQL is
/// kept to the common subset of both dialects; only the connection type differs.
/// </summary>
public sealed class SqlArenaStore(Func<DbConnection> connect, bool postgres) : IArenaStore
{
    public static SqlArenaStore Postgres(string connectionString) => new(() => new NpgsqlConnection(connectionString), true);

    public static SqlArenaStore Sqlite(string path) => new(() => new SqliteConnection($"Data Source={path}"), false);

    private async Task<DbConnection> OpenAsync(CancellationToken ct)
    {
        var c = connect();
        await c.OpenAsync(ct);
        return c;
    }

    private static DbCommand Command(DbConnection c, string sql, params (string Name, object? Value)[] args)
    {
        var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        return cmd;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var c = await OpenAsync(ct);
        string sql = """
            CREATE TABLE IF NOT EXISTS players (
                name VARCHAR(40) PRIMARY KEY,
                rating INTEGER NOT NULL,
                wins INTEGER NOT NULL,
                losses INTEGER NOT NULL,
                clan VARCHAR(40) NULL);
            CREATE TABLE IF NOT EXISTS clans (
                name VARCHAR(40) PRIMARY KEY,
                tag VARCHAR(8) NOT NULL,
                war_points INTEGER NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_players_rating ON players(rating DESC);
            """;
        await using var cmd = Command(c, sql);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<PlayerRecord> GetOrCreatePlayerAsync(string name, CancellationToken ct = default)
    {
        await using var c = await OpenAsync(ct);
        await using (var cmd = Command(c, "SELECT name, rating, wins, losses, clan FROM players WHERE name = @n", ("@n", name)))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            if (await r.ReadAsync(ct))
                return new PlayerRecord(r.GetString(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.IsDBNull(4) ? null : r.GetString(4));
        }
        var player = new PlayerRecord(name, Core.Progression.Rating.Initial, 0, 0, null);
        await SavePlayerAsync(player, ct);
        return player;
    }

    public async Task SavePlayerAsync(PlayerRecord p, CancellationToken ct = default)
    {
        await using var c = await OpenAsync(ct);
        string sql = postgres
            ? "INSERT INTO players(name, rating, wins, losses, clan) VALUES(@n, @r, @w, @l, @c) ON CONFLICT (name) DO UPDATE SET rating = EXCLUDED.rating, wins = EXCLUDED.wins, losses = EXCLUDED.losses, clan = EXCLUDED.clan"
            : "INSERT INTO players(name, rating, wins, losses, clan) VALUES(@n, @r, @w, @l, @c) ON CONFLICT(name) DO UPDATE SET rating = excluded.rating, wins = excluded.wins, losses = excluded.losses, clan = excluded.clan";
        await using var cmd = Command(c, sql, ("@n", p.Name), ("@r", p.Rating), ("@w", p.Wins), ("@l", p.Losses), ("@c", p.Clan));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerRecord>> TopPlayersAsync(int count, CancellationToken ct = default)
    {
        await using var c = await OpenAsync(ct);
        await using var cmd = Command(c, "SELECT name, rating, wins, losses, clan FROM players ORDER BY rating DESC LIMIT @k", ("@k", count));
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<PlayerRecord>();
        while (await r.ReadAsync(ct))
            list.Add(new PlayerRecord(r.GetString(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.IsDBNull(4) ? null : r.GetString(4)));
        return list;
    }

    public async Task<ClanRecord?> GetClanAsync(string name, CancellationToken ct = default)
    {
        await using var c = await OpenAsync(ct);
        await using var cmd = Command(c, "SELECT c.name, c.tag, c.war_points, (SELECT COUNT(*) FROM players p WHERE p.clan = c.name) FROM clans c WHERE c.name = @n", ("@n", name));
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new ClanRecord(r.GetString(0), r.GetString(1), Convert.ToInt32(r.GetValue(3), System.Globalization.CultureInfo.InvariantCulture), r.GetInt32(2)) : null;
    }

    public async Task<ClanRecord> CreateClanAsync(string name, string tag, CancellationToken ct = default)
    {
        await using (var c = await OpenAsync(ct))
        await using (var cmd = Command(c, "INSERT INTO clans(name, tag, war_points) VALUES(@n, @t, 0)", ("@n", name), ("@t", tag)))
            await cmd.ExecuteNonQueryAsync(ct);
        return (await GetClanAsync(name, ct))!;
    }

    public async Task AddClanPointsAsync(string clan, int points, CancellationToken ct = default)
    {
        await using var c = await OpenAsync(ct);
        await using var cmd = Command(c, "UPDATE clans SET war_points = war_points + @p WHERE name = @n", ("@p", points), ("@n", clan));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ClanRecord>> ClanStandingsAsync(CancellationToken ct = default)
    {
        await using var c = await OpenAsync(ct);
        await using var cmd = Command(c, "SELECT c.name, c.tag, c.war_points, (SELECT COUNT(*) FROM players p WHERE p.clan = c.name) FROM clans c ORDER BY c.war_points DESC LIMIT 50");
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ClanRecord>();
        while (await r.ReadAsync(ct))
            list.Add(new ClanRecord(r.GetString(0), r.GetString(1), Convert.ToInt32(r.GetValue(3), System.Globalization.CultureInfo.InvariantCulture), r.GetInt32(2)));
        return list;
    }
}
