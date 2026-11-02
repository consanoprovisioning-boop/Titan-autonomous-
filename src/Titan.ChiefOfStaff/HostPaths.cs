namespace Titan.ChiefOfStaff;

public sealed class HostPaths
{
    public string DataRoot { get; }
    public string AppDir { get; }
    public string KillSwitch { get; }
    public string BrowserProfile { get; }
    public string Database { get; }
    public string Heartbeat { get; }
    public string VerifyMarker { get; }
    public string SessionLock { get; }
    public string NodeMarker { get; }
    public string ClusterLeases { get; }
    public string ClusterMode { get; }

    public HostPaths(string? dataRoot = null)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
        {
            local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        DataRoot = dataRoot ?? Path.Combine(local, "TitanChief");
        AppDir = Path.Combine(DataRoot, "app");
        KillSwitch = Path.Combine(DataRoot, "kill.switch");
        BrowserProfile = Path.Combine(DataRoot, "browser");
        Database = Path.Combine(DataRoot, "host.sqlite");
        Heartbeat = Path.Combine(DataRoot, "heartbeat.json");
        VerifyMarker = Path.Combine(DataRoot, "verify.json");
        SessionLock = Path.Combine(DataRoot, "session.lock");
        NodeMarker = Path.Combine(DataRoot, "node.json");
        ClusterLeases = Path.Combine(DataRoot, "cluster-leases.json");
        ClusterMode = Path.Combine(DataRoot, "cluster-mode.json");
    }

    public void EnsureDataTree()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(AppDir);
    }

    public string? ReadEnrolledNodeId()
        => ReadJsonString(NodeMarker, "nodeId");

    public bool ReadScaleOutMode()
    {
        var shared = Environment.GetEnvironmentVariable("TITAN_LEASE_DIR");
        if (!string.IsNullOrWhiteSpace(shared))
        {
            var sharedMode = Path.Combine(shared.Trim(), "cluster-mode.json");
            if (ReadJsonBool(sharedMode, "scaleOut") == true)
            {
                return true;
            }
        }

        return ReadJsonBool(ClusterMode, "scaleOut") == true
               || ClusterPartition.IsOverflowNode(ReadEnrolledNodeId());
    }

    private static string? ReadJsonString(string path, string property)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty(property, out var value) ? value.GetString() : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static bool? ReadJsonBool(string path, string property)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty(property, out var value) && value.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                ? value.GetBoolean()
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
