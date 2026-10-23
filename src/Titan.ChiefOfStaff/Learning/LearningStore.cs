using Microsoft.Data.Sqlite;

namespace Titan.ChiefOfStaff;

public sealed class LearningStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public LearningStore(HostPaths paths)
    {
        paths.EnsureDataTree();
        _connection = new SqliteConnection($"Data Source={paths.LearningDatabase}");
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS experience (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                at_et TEXT NOT NULL,
                rooftop TEXT,
                queue TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                qualified INTEGER NOT NULL,
                sent INTEGER NOT NULL,
                held INTEGER NOT NULL,
                reason TEXT
            );
            CREATE TABLE IF NOT EXISTS selector_memory (
                selector TEXT PRIMARY KEY,
                success INTEGER NOT NULL,
                fail INTEGER NOT NULL,
                last_et TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS draft_memory (
                template_id TEXT NOT NULL,
                rooftop TEXT NOT NULL,
                sent INTEGER NOT NULL,
                replies INTEGER NOT NULL,
                last_et TEXT NOT NULL,
                PRIMARY KEY (template_id, rooftop)
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void RecordExperience(string rooftop, string queue, int durationMs, int qualified, int sent, int held, string? reason)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO experience (at_et, rooftop, queue, duration_ms, qualified, sent, held, reason)
            VALUES ($at, $rooftop, $queue, $ms, $q, $s, $h, $reason)
            """;
        cmd.Parameters.AddWithValue("$at", EasternClock.Now().ToString("o"));
        cmd.Parameters.AddWithValue("$rooftop", rooftop);
        cmd.Parameters.AddWithValue("$queue", queue);
        cmd.Parameters.AddWithValue("$ms", durationMs);
        cmd.Parameters.AddWithValue("$q", qualified);
        cmd.Parameters.AddWithValue("$s", sent);
        cmd.Parameters.AddWithValue("$h", held);
        cmd.Parameters.AddWithValue("$reason", (object?)reason ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void RememberSelector(string selector, bool success)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO selector_memory (selector, success, fail, last_et)
            VALUES ($sel, $ok, $fail, $at)
            ON CONFLICT(selector) DO UPDATE SET
                success = selector_memory.success + $ok,
                fail = selector_memory.fail + $fail,
                last_et = $at
            """;
        cmd.Parameters.AddWithValue("$sel", selector);
        cmd.Parameters.AddWithValue("$ok", success ? 1 : 0);
        cmd.Parameters.AddWithValue("$fail", success ? 0 : 1);
        cmd.Parameters.AddWithValue("$at", EasternClock.Now().ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void RememberDraft(string templateId, string rooftop, bool sent, bool reply)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO draft_memory (template_id, rooftop, sent, replies, last_et)
            VALUES ($id, $rooftop, $sent, $reply, $at)
            ON CONFLICT(template_id, rooftop) DO UPDATE SET
                sent = draft_memory.sent + $sent,
                replies = draft_memory.replies + $reply,
                last_et = $at
            """;
        cmd.Parameters.AddWithValue("$id", templateId);
        cmd.Parameters.AddWithValue("$rooftop", rooftop);
        cmd.Parameters.AddWithValue("$sent", sent ? 1 : 0);
        cmd.Parameters.AddWithValue("$reply", reply ? 1 : 0);
        cmd.Parameters.AddWithValue("$at", EasternClock.Now().ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<string> RankedSelectors(IEnumerable<string> candidates)
    {
        var scores = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var selector in candidates)
        {
            scores[selector] = ScoreSelector(selector);
        }

        return scores.OrderByDescending(pair => pair.Value).Select(pair => pair.Key).ToArray();
    }

    public int ScoreSelector(string selector)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT success, fail FROM selector_memory WHERE selector = $sel";
        cmd.Parameters.AddWithValue("$sel", selector);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return 0;
        }

        return reader.GetInt32(0) - reader.GetInt32(1);
    }

    public EfficiencySnapshot Snapshot()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*), COALESCE(SUM(qualified),0), COALESCE(SUM(sent),0), COALESCE(AVG(duration_ms),0)
            FROM experience
            """;
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return new EfficiencySnapshot(0, 0, 0, 0);
        }

        return new EfficiencySnapshot(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), Convert.ToInt32(reader.GetDouble(3)));
    }

    public void Dispose() => _connection.Dispose();
}

public sealed record EfficiencySnapshot(int Cycles, int Qualified, int Sent, int AverageDurationMs);
