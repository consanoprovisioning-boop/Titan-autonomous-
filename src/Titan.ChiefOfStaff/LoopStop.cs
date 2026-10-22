using System.Diagnostics;

namespace Titan.ChiefOfStaff;

/// <summary>
/// Stop the live-loop host without closing Chrome. Check-in starts Chrome
/// with UseShellExecute false, so that window is a child of the autostart
/// process. Killing the descendant tree closes the signed-in Eleads session.
/// </summary>
public static class LoopStop
{
    public const bool KillDescendantProcesses = false;

    public static void StopHost(Process host)
    {
        if (host.HasExited)
        {
            return;
        }

        host.Kill(entireProcessTree: KillDescendantProcesses);
    }
}
