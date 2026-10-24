using System.Globalization;
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

    public void Log(string queue, string action, string result, string? rooftop = null, string? customerKey = null, DateTimeOffset? at = null)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO work_log (at_et, rooftop, customer_key, queue, action, result)
            VALUES ($at, $rooftop, $customer, $queue, $action, $result)
            """;
        cmd.Parameters.AddWithValue("$at", (at ?? EasternClock.Now()).ToString("o"));
        cmd.Parameters.AddWithValue("$rooftop", (object?)rooftop ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$customer", (object?)customerKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$queue", queue);
        cmd.Parameters.AddWithValue("$action", action);
        cmd.Parameters.AddWithValue("$result", result);
        cmd.ExecuteNonQuery();
    }

    public DateTimeOffset? LatestSendAttemptAt(string? rooftop, string? customerKey)
    {
        if (string.IsNullOrWhiteSpace(rooftop) || string.IsNullOrWhiteSpace(customerKey))
        {
            return null;
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT at_et FROM work_log
            WHERE rooftop = $rooftop AND customer_key = $customer
              AND (
                    action = 'send-attempt'
                    OR (action = 'send' AND result IN (
                        'SMS text completed via system.',
                        'Manual email completed via system.'))
                  )
            ORDER BY id DESC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$rooftop", rooftop);
        cmd.Parameters.AddWithValue("$customer", customerKey);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var text = reader.GetString(0);
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
            ? at
            : DateTimeOffset.MaxValue;
    }

    public IReadOnlyList<string> RecentLogs(int take)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT at_et, queue, action, result, customer_key FROM work_log ORDER BY id DESC LIMIT $take";
        cmd.Parameters.AddWithValue("$take", take);
        using var reader = cmd.ExecuteReader();
        var lines = new List<string>();
        while (reader.Read())
        {
            var customer = reader.IsDBNull(4) ? "" : reader.GetString(4);
            lines.Add($"{reader.GetString(0)} {reader.GetString(1)} {reader.GetString(2)} {reader.GetString(3)} {customer}".Trim());
        }

        return lines;
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
