using System.Reflection;

namespace Titan.ChiefOfStaff;

public sealed record SelfInstallResult(string AppDir, bool Copied, bool Armed, string? Shortcut, string Summary);

public static class SelfInstall
{
    public static SelfInstallResult Ensure(HostPaths paths, string sourceDir)
    {
        paths.EnsureDataTree();
        var copied = SyncAppFiles(sourceDir, paths.AppDir);
        var kill = new KillSwitch(paths);
        if (!kill.IsArmed())
        {
            kill.Arm();
        }

        var shortcut = TryWriteDesktopShortcut(paths.AppDir);
        var summary = copied
            ? "self-install copied files and armed kill.switch — not Verify, not send authorization"
            : "kill.switch armed — app already in place. not Verify, not send authorization";
        return new SelfInstallResult(paths.AppDir, copied, kill.IsArmed(), shortcut, summary);
    }

    public static bool AlreadyInstalledHere(HostPaths paths, string sourceDir)
        => PathsEqual(Path.GetFullPath(sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), Path.GetFullPath(paths.AppDir));

    public static bool PathsEqual(string left, string right)
        => string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string path)
        => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static bool SyncAppFiles(string sourceDir, string appDir)
    {
        Directory.CreateDirectory(appDir);
        if (!Directory.Exists(sourceDir) || PathsEqual(sourceDir, appDir))
        {
            return false;
        }

        var copied = false;
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)
                || name.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relative = Path.GetRelativePath(sourceDir, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}.playwright{Path.DirectorySeparatorChar}driver{Path.DirectorySeparatorChar}browser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dest = Path.Combine(appDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var needCopy = !File.Exists(dest)
                           || new FileInfo(file).Length != new FileInfo(dest).Length
                           || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(dest);
            if (!needCopy)
            {
                continue;
            }

            File.Copy(file, dest, overwrite: true);
            copied = true;
        }

        return copied;
    }

    public static string? TryWriteDesktopShortcut(string appDir)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var exe = Path.Combine(appDir, "TitanCOS.exe");
        if (!File.Exists(exe))
        {
            exe = Path.Combine(appDir, "Titan.ChiefOfStaff.exe");
        }

        if (!File.Exists(exe))
        {
            return null;
        }

        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop))
            {
                return null;
            }

            var link = Path.Combine(desktop, "Titan COS.lnk");
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return null;
            }

            var shell = Activator.CreateInstance(shellType);
            if (shell is null)
            {
                return null;
            }

            var shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                [link]);
            if (shortcut is null)
            {
                return null;
            }

            Set(shortcut, "TargetPath", exe);
            Set(shortcut, "WorkingDirectory", appDir);
            Set(shortcut, "WindowStyle", 1);
            Set(shortcut, "Description", "Titan Chief of Staff — local overlay for 28206 and 28546");
            shortcut.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            return link;
        }
        catch
        {
            return null;
        }
    }

    private static void Set(object shortcut, string property, object value)
        => shortcut.GetType().InvokeMember(property, BindingFlags.SetProperty, null, shortcut, [value]);
}
