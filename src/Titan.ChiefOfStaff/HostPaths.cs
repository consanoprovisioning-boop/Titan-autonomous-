namespace Titan.ChiefOfStaff;

public sealed class HostPaths
{
    public string DataRoot { get; }
    public string AppDir { get; }
    public string KillSwitch { get; }
    public string BrowserProfile { get; }
    public string Database { get; }
    public string Heartbeat { get; }
    public string SessionLock { get; }

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
        SessionLock = Path.Combine(DataRoot, "session.lock");
    }

    public void EnsureDataTree()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(AppDir);
    }
}
