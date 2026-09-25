using Microsoft.Data.Sqlite;

namespace Titan.ChiefOfStaff;

public sealed class HostStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public HostStore(HostPaths paths)
    {
        paths.EnsureDataTree();
        _connection = new SqliteConnection($"Data Source={paths.Database}");
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS heartbeat (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                state TEXT NOT NULL,
                rooftop TEXT,
                verified_at TEXT,
                last_tick_at TEXT,
                last_error TEXT,
                sms_open INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS work_log (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                at_et TEXT NOT NULL,
                rooftop TEXT,
                customer_key TEXT,
                queue TEXT NOT NULL,
                action TEXT NOT NULL,
                result TEXT NOT NULL
            );
            INSERT OR IGNORE INTO heartbeat (id, state, rooftop, verified_at, last_tick_at, last_error, sms_open)
            VALUES (1, 'AwaitingInstall', NULL, NULL, NULL, NULL, 0);
            """;
        cmd.ExecuteNonQuery();
    }

    public Heartbeat ReadHeartbeat()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT state, rooftop, verified_at, last_tick_at, last_error, sms_open FROM heartbeat WHERE id = 1";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return new Heartbeat("AwaitingInstall", null, null, null, null, false);
        }

        return new Heartbeat(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetInt32(5) != 0);
    }

    public void WriteHeartbeat(Heartbeat beat)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE heartbeat
            SET state = $state, rooftop = $rooftop, verified_at = $verified, last_tick_at = $tick, last_error = $error, sms_open = $sms
            WHERE id = 1
            """;
        cmd.Parameters.AddWithValue("$state", beat.State);
        cmd.Parameters.AddWithValue("$rooftop", (object?)beat.Rooftop ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$verified", (object?)beat.VerifiedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tick", (object?)beat.LastTickAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$error", (object?)beat.LastError ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sms", beat.SmsOpen ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void Log(string queue, string action, string result, string? rooftop = null, string? customerKey = null)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO work_log (at_et, rooftop, customer_key, queue, action, result)
            VALUES ($at, $rooftop, $customer, $queue, $action, $result)
            """;
        cmd.Parameters.AddWithValue("$at", EasternClock.Now().ToString("o"));
        cmd.Parameters.AddWithValue("$rooftop", (object?)rooftop ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$customer", (object?)customerKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$queue", queue);
        cmd.Parameters.AddWithValue("$action", action);
        cmd.Parameters.AddWithValue("$result", result);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}

public sealed record Heartbeat(
    string State,
    string? Rooftop,
    string? VerifiedAt,
    string? LastTickAt,
    string? LastError,
    bool SmsOpen);
