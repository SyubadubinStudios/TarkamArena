using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using TarkamArena.Core.Progression;
using TarkamArena.Core.Settings;

namespace TarkamArena.Persistence;

public sealed record MatchRecord(DateTimeOffset PlayedAt, string Mode, string CharacterId, string OpponentId, bool Won, int MaxCombo, string? Finisher);

/// <summary>
/// Local save data in a single SQLite file: the player profile, settings and match history.
/// Documents are stored as JSON so the schema survives new profile fields without migrations.
/// </summary>
public sealed class GameDatabase : IDisposable
{
    private const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions s_json = new()
    {
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
    };

    private readonly SqliteConnection _connection;

    public string Path { get; }

    public GameDatabase(string path)
    {
        Path = path;
        if (path != ":memory:")
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        _connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        _connection.Open();
        Execute("PRAGMA journal_mode=WAL;");
        Migrate();
    }

    /// <summary>Default location: %APPDATA%/SyubadubinStudios/TarkamArena/save.db (or the XDG equivalent).</summary>
    public static string DefaultPath() =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyubadubinStudios", "TarkamArena", "save.db");

    private void Migrate()
    {
        Execute("""
            CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS documents (name TEXT PRIMARY KEY, json TEXT NOT NULL, updated_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS match_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                played_at TEXT NOT NULL,
                mode TEXT NOT NULL,
                character_id TEXT NOT NULL,
                opponent_id TEXT NOT NULL,
                won INTEGER NOT NULL,
                max_combo INTEGER NOT NULL,
                finisher TEXT NULL);
            CREATE INDEX IF NOT EXISTS ix_match_history_character ON match_history(character_id);
            """);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO meta(key, value) VALUES('schema', $v) ON CONFLICT(key) DO UPDATE SET value = excluded.value;";
        cmd.Parameters.AddWithValue("$v", SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    private void Execute(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private void SaveDocument<T>(string name, T value)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO documents(name, json, updated_at) VALUES($n, $j, $t) ON CONFLICT(name) DO UPDATE SET json = excluded.json, updated_at = excluded.updated_at;";
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(value, s_json));
        cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private T? LoadDocument<T>(string name) where T : class
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT json FROM documents WHERE name = $n;";
        cmd.Parameters.AddWithValue("$n", name);
        return cmd.ExecuteScalar() is string json ? JsonSerializer.Deserialize<T>(json, s_json) : null;
    }

    public PlayerProfile LoadProfile()
    {
        var profile = LoadDocument<PlayerProfile>("profile");
        if (profile is not null) return profile;
        profile = PlayerProfile.CreateNew();
        SaveProfile(profile);
        return profile;
    }

    public void SaveProfile(PlayerProfile profile) => SaveDocument("profile", profile);

    public GameSettings LoadSettings() => LoadDocument<GameSettings>("settings") ?? new GameSettings();

    public void SaveSettings(GameSettings settings) => SaveDocument("settings", settings);

    public void RecordMatch(MatchRecord record)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO match_history(played_at, mode, character_id, opponent_id, won, max_combo, finisher)
            VALUES($p, $m, $c, $o, $w, $x, $f);
            """;
        cmd.Parameters.AddWithValue("$p", record.PlayedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$m", record.Mode);
        cmd.Parameters.AddWithValue("$c", record.CharacterId);
        cmd.Parameters.AddWithValue("$o", record.OpponentId);
        cmd.Parameters.AddWithValue("$w", record.Won ? 1 : 0);
        cmd.Parameters.AddWithValue("$x", record.MaxCombo);
        cmd.Parameters.AddWithValue("$f", (object?)record.Finisher ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<MatchRecord> RecentMatches(int count = 20)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT played_at, mode, character_id, opponent_id, won, max_combo, finisher FROM match_history ORDER BY id DESC LIMIT $n;";
        cmd.Parameters.AddWithValue("$n", count);
        using var reader = cmd.ExecuteReader();
        var list = new List<MatchRecord>();
        while (reader.Read())
        {
            list.Add(new MatchRecord(
                DateTimeOffset.Parse(reader.GetString(0), System.Globalization.CultureInfo.InvariantCulture),
                reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4) == 1, reader.GetInt32(5),
                reader.IsDBNull(6) ? null : reader.GetString(6)));
        }
        return list;
    }

    /// <summary>Win rate per fighter, for the collection screen.</summary>
    public IReadOnlyDictionary<string, (int Played, int Won)> CharacterStats()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT character_id, COUNT(*), SUM(won) FROM match_history GROUP BY character_id;";
        using var reader = cmd.ExecuteReader();
        var result = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        while (reader.Read()) result[reader.GetString(0)] = (reader.GetInt32(1), reader.GetInt32(2));
        return result;
    }

    public void Dispose() => _connection.Dispose();
}
