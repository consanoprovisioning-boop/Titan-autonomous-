using Titan.ChiefOfStaff.Eleads;

namespace Titan.ChiefOfStaff;

public static class LaunchHost
{
    public static async Task<int> RunAsync(HostPaths paths, KillSwitch kill)
    {
        if (CommandRouter.CloudRefused(out var cloudCode))
        {
            return cloudCode;
        }

        Prepare(paths, kill, AppContext.BaseDirectory);
        if (File.Exists(paths.StopFlag))
        {
            File.Delete(paths.StopFlag);
        }

        using var overlay = OverlayHud.Start(paths);
        overlay.Publish(OverlayState.Waiting("Opening Chrome. Sign in as Bordine, Glenn on 28206 or 28546. COS never types the password."));

        using var sessionLock = new SessionLock(paths);
        if (!sessionLock.Acquired)
        {
            overlay.Publish(OverlayState.Waiting("Another Titan COS session is already live."));
            return 4;
        }

        using var store = new HostStore(paths);
        using var learning = new LearningStore(paths);
        var verifiedAt = store.ReadHeartbeat().VerifiedAt;

        overlay.Publish(OverlayState.Waiting("Waiting for a live Bordine, Glenn session on 28206 or 28546."));
        var signedIn = await WaitForVerifyAsync(paths, kill, store, overlay, TimeSpan.FromHours(12));
        if (!signedIn)
        {
            return 6;
        }

        verifiedAt = store.ReadHeartbeat().VerifiedAt ?? EasternClock.Now().ToString("o");
        var loop = new ContinuousLoop();
        using var cts = new CancellationTokenSource();
        return await loop.RunAsync(
            paths,
            kill,
            overlay,
            async () =>
            {
                if (!kill.IsArmed() || File.Exists(paths.StopFlag) || overlay.Closed)
                {
                    return CycleSnapshot.BlockedCycle("stopped");
                }

                await using var eleads = new EleadsSession();
                try
                {
                    await eleads.AttachAsync(paths, allowNavigate: false, startChromeIfNeeded: false);
                    await eleads.InspectAsync();
                }
                catch (Exception ex)
                {
                    store.WriteHeartbeat(store.ReadHeartbeat() with
                    {
                        State = HostState.AwaitingSignIn.ToString(),
                        LastError = ex.Message
                    });
                    return CycleSnapshot.BlockedCycle("Lost Eleads session. Sign in again as Bordine, Glenn. " + ex.Message);
                }

                if (eleads.DetectedRooftop == Policy.ForbiddenTkoId || Policy.IsForbiddenRooftop(eleads.DetectedRooftop))
                {
                    store.WriteHeartbeat(store.ReadHeartbeat() with
                    {
                        State = HostState.BlockedForbiddenRooftop.ToString(),
                        LastError = "forbidden rooftop"
                    });
                    return CycleSnapshot.BlockedCycle("TKO 6220 blocked.");
                }

                if (!eleads.LoggedIn || eleads.Page is null || !Policy.IsAllowedRooftop(eleads.DetectedRooftop))
                {
                    return CycleSnapshot.BlockedCycle("Session is not verified. Sign in as Bordine, Glenn. Titan will not type the password.");
                }

                var et = EasternClock.Now();
                var first = eleads.DetectedRooftop!;
                var firstTick = await new WorkTick().RunAsync(eleads.Page, store, learning, first, et);
                var other = DualRooftop.OtherAllowed(first);
                var nissan = first == Policy.NissanId ? firstTick : "pending switch";
                var mitsubishi = first == Policy.MitsubishiId ? firstTick : "pending switch";
                var cycleSent = firstTick.Contains("CRM-proven send", StringComparison.Ordinal) && !firstTick.Contains("0 CRM-proven", StringComparison.Ordinal) ? 1 : 0;
                if (other is not null)
                {
                    var switched = await DualRooftop.SwitchAsync(eleads.Page, learning, first, other);
                    await eleads.InspectAsync();
                    if (eleads.LoggedIn && eleads.Page is not null && eleads.DetectedRooftop == other)
                    {
                        var secondTick = await new WorkTick().RunAsync(eleads.Page, store, learning, other, EasternClock.Now());
                        if (other == Policy.NissanId)
                        {
                            nissan = secondTick;
                        }
                        else
                        {
                            mitsubishi = secondTick;
                        }

                        if (secondTick.Contains("CRM-proven send", StringComparison.Ordinal) && !secondTick.Contains("0 CRM-proven", StringComparison.Ordinal))
                        {
                            cycleSent++;
                        }

                        await DualRooftop.SwitchAsync(eleads.Page, learning, other, first);
                    }
                    else if (switched == first)
                    {
                        if (other == Policy.NissanId)
                        {
                            nissan = "store switch not available this cycle";
                        }
                        else
                        {
                            mitsubishi = "store switch not available this cycle";
                        }
                    }
                }

                var snap = learning.Snapshot();
                store.WriteHeartbeat(new Heartbeat(
                    HostState.Looping.ToString(),
                    eleads.DetectedRooftop,
                    verifiedAt,
                    EasternClock.Now().ToString("o"),
                    null,
                    Policy.SmsOpen(EasternClock.Now())));
                CommandRouter.WriteHeartbeatFile(paths, store.ReadHeartbeat());
                return new CycleSnapshot(
                    true,
                    false,
                    cycleSent,
                    nissan,
                    mitsubishi,
                    EasternClock.Now().ToString("t"),
                    $"{snap.Cycles} cycles · {snap.Qualified} qualified · {snap.Sent} proven sends · {snap.AverageDurationMs}ms avg",
                    cycleSent > 0
                        ? "Working both rooftops. CRM history is the only send count."
                        : "Monitoring both rooftops. Eligible work executes when a composer and CRM proof exist.");
            },
            DelayAsync,
            cts.Token);
    }

    public static SelfInstallResult Prepare(HostPaths paths, KillSwitch kill, string sourceDir)
    {
        var installed = SelfInstall.Ensure(paths, sourceDir);
        _ = kill;
        using var store = new HostStore(paths);
        if (store.ReadHeartbeat().State == "AwaitingInstall")
        {
            store.WriteHeartbeat(new Heartbeat(HostState.AwaitingSignIn.ToString(), null, null, null, null, Policy.SmsOpen(EasternClock.Now())));
        }

        store.Log("host", "launch", installed.Summary);
        OverlayState.Waiting("Titan COS prepared locally. No cloud tokens.").Write(paths);
        return installed;
    }

    public static async Task<bool> WaitForVerifyAsync(
        HostPaths paths,
        KillSwitch kill,
        HostStore store,
        IOverlay overlay,
        TimeSpan limit)
    {
        var deadline = DateTime.UtcNow + limit;
        while (DateTime.UtcNow < deadline && kill.IsArmed() && !overlay.Closed && !File.Exists(paths.StopFlag))
        {
            await using var eleads = new EleadsSession();
            try
            {
                await eleads.AttachAsync(paths, allowNavigate: true, startChromeIfNeeded: true);
                await eleads.InspectAsync();
            }
            catch (Exception ex)
            {
                overlay.Publish(OverlayState.Waiting("Chrome is not ready. " + ex.Message + " Titan will not type the password."));
                await Task.Delay(2000);
                continue;
            }

            if (eleads.DetectedRooftop == Policy.ForbiddenTkoId)
            {
                store.WriteHeartbeat(new Heartbeat(HostState.BlockedForbiddenRooftop.ToString(), Policy.ForbiddenTkoId, null, null, "TKO 6220 blocked", Policy.SmsOpen(EasternClock.Now())));
                overlay.Publish(OverlayState.Waiting("TKO 6220 is blocked. Switch to 28206 or 28546."));
                await Task.Delay(3000);
                continue;
            }

            if (eleads.LoggedIn && Policy.IsAllowedRooftop(eleads.DetectedRooftop))
            {
                var et = EasternClock.Now();
                var beat = new Heartbeat(HostState.VerifiedIdle.ToString(), eleads.DetectedRooftop, et.ToString("o"), null, null, Policy.SmsOpen(et));
                store.WriteHeartbeat(beat);
                CommandRouter.WriteHeartbeatFile(paths, beat);
                VerifyMarker.Write(paths, beat.State, beat.Rooftop, Policy.Identity, beat.VerifiedAt, true);
                store.Log("host", "verify", $"Verified rooftop {eleads.DetectedRooftop} as {Policy.Identity}.", eleads.DetectedRooftop);
                overlay.Publish(new OverlayState(
                    "Titan COS",
                    "Verified — starting live loop",
                    eleads.DetectedRooftop == Policy.NissanId ? "live" : "standby",
                    eleads.DetectedRooftop == Policy.MitsubishiId ? "live" : "standby",
                    "-",
                    "local memory",
                    "Working both rooftops. No cloud tokens.",
                    true,
                    true));
                return true;
            }

            overlay.Publish(OverlayState.Waiting("Sign in as Bordine, Glenn on 28206 or 28546. COS starts the moment the session is live."));
            await Task.Delay(2000);
        }

        return false;
    }

    public static Task DelayAsync(TimeSpan delay, CancellationToken cancel)
        => Task.Delay(delay, cancel);
}
