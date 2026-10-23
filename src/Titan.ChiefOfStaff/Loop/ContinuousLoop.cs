using Titan.ChiefOfStaff.Eleads;

namespace Titan.ChiefOfStaff;

public sealed class ContinuousLoop
{
    public async Task<int> RunAsync(
        HostPaths paths,
        KillSwitch kill,
        IOverlay overlay,
        Func<Task<CycleSnapshot>> cycle,
        Func<TimeSpan, CancellationToken, Task> delay,
        CancellationToken cancel)
    {
        var sentThisSession = false;
        while (!cancel.IsCancellationRequested && kill.IsArmed() && !overlay.Closed && !File.Exists(paths.StopFlag))
        {
            var et = EasternClock.Now();
            CycleSnapshot snap;
            try
            {
                snap = await cycle();
            }
            catch (Exception ex)
            {
                snap = CycleSnapshot.BlockedCycle(ex.Message);
            }

            sentThisSession = sentThisSession || snap.Sent > 0;
            var learning = snap.Learning;
            overlay.Publish(new OverlayState(
                "Titan COS",
                snap.Blocked ? "Blocked — retrying" : "Live loop",
                snap.Nissan,
                snap.Mitsubishi,
                snap.LastTick,
                learning,
                snap.Instruction,
                snap.Verified,
                Looping: !snap.Blocked));

            var wait = LoopPace.NextDelay(et, snap.Blocked, sentThisSession);
            try
            {
                await delay(wait, cancel);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        overlay.Publish(OverlayState.Waiting("COS loop stopped. kill.switch unchanged. Double-click TitanCOS.exe to start again."));
        return 0;
    }
}

public sealed record CycleSnapshot(
    bool Verified,
    bool Blocked,
    int Sent,
    string Nissan,
    string Mitsubishi,
    string LastTick,
    string Learning,
    string Instruction)
{
    public static CycleSnapshot BlockedCycle(string reason)
        => new(false, true, 0, "28206 idle", "28546 idle", "-", "local memory", reason);
}
