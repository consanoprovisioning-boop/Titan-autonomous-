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
    public string HostIdentity { get; }
    public string HostSecret { get; }

    public HostPaths(string? dataRoot = null)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
        {
            local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        if (dataRoot is not null && !IsAllowedOverride(dataRoot))
        {
            throw new InvalidOperationException(
                "Refusing a data folder outside this user's TitanChief directory or a temp test folder.");
        }

        DataRoot = dataRoot ?? Path.Combine(local, "TitanChief");
        AppDir = Path.Combine(DataRoot, "app");
        KillSwitch = Path.Combine(DataRoot, "kill.switch");
        BrowserProfile = Path.Combine(DataRoot, "browser");
        Database = Path.Combine(DataRoot, "host.sqlite");
        Heartbeat = Path.Combine(DataRoot, "heartbeat.json");
        VerifyMarker = Path.Combine(DataRoot, "verify.json");
        SessionLock = Path.Combine(DataRoot, "session.lock");
        HostIdentity = Path.Combine(DataRoot, "host.json");
        HostSecret = Path.Combine(DataRoot, "host.secret");
    }

    public static bool IsAllowedOverride(string dataRoot)
    {
        var full = Path.GetFullPath(dataRoot);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
        {
            local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        var titan = Path.GetFullPath(Path.Combine(local, "TitanChief"));
        var temp = Path.GetFullPath(Path.GetTempPath());
        return IsUnder(full, titan) || IsUnder(full, temp);
    }

    private static bool IsUnder(string path, string parent)
    {
        var prefix = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;
        var candidate = path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, parent, StringComparison.OrdinalIgnoreCase);
    }

    public void EnsureDataTree()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(AppDir);
    }
}
