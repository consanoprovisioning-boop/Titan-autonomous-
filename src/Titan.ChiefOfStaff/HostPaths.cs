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
    public string OverlayJson { get; }
    public string OverlayText { get; }
    public string LearningDatabase { get; }
    public string StopFlag { get; }

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
        OverlayJson = Path.Combine(DataRoot, "overlay.json");
        OverlayText = Path.Combine(DataRoot, "overlay.txt");
        LearningDatabase = Path.Combine(DataRoot, "learning.sqlite");
        StopFlag = Path.Combine(DataRoot, "stop.flag");
    }

    public void EnsureDataTree()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(AppDir);
    }
}
