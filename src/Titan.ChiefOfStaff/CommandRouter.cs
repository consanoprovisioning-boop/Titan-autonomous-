using Titan.ChiefOfStaff.Eleads;

namespace Titan.ChiefOfStaff;

public static class CommandRouter
{
    public static async Task<int> RunAsync(string[] args, bool defaultLaunch)
    {
        if (args.Any(a => a.Contains("password", StringComparison.OrdinalIgnoreCase)
                          || a.Contains("passwd", StringComparison.OrdinalIgnoreCase)))
        {
            Console.Error.WriteLine("Titan never stores or types the Eleads password. Remove that argument.");
            return 2;
        }

        var verb = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : (defaultLaunch ? "launch" : "status");
        var paths = new HostPaths(ReadOverride(args, "--data"));
        var kill = new KillSwitch(paths);

        return verb switch
        {
            "install" => Install(paths, kill),
            "status" => Status(paths, kill),
            "checkin" => await CheckInAsync(paths, kill),
            "verify" => RejectVerifyWord(),
            "tick" => await TickAsync(paths, kill),
            "launch" or "cos" or "overlay" or "run" => await LaunchHost.RunAsync(paths, kill),
            "report" => Report(paths),
            "stop" => Stop(paths),
            _ => Usage()
        };
    }

    public static int Install(HostPaths paths, KillSwitch kill)
    {
        using var sessionLock = SessionLock.Acquire(paths, TimeSpan.FromMinutes(2));
        if (!sessionLock.Acquired)
        {
            Console.Error.WriteLine("Another Titan session lock is held. Install was not saved.");
            return 4;
        }

        var result = SelfInstall.Ensure(paths, AppContext.BaseDirectory);
        var state = HostStateMachine.AfterInstall(kill);
        using var store = new HostStore(paths);
        var et = EasternClock.Now();
        var beat = new Heartbeat(state.ToString(), null, null, null, null, Policy.SmsOpen(et));
        SessionRecord.Save(paths, store, beat, glennVerified: false);
        store.Log("host", "install", result.Summary);
        Console.WriteLine($"Installed data root: {paths.DataRoot}");
        Console.WriteLine($"app={result.AppDir}");
        Console.WriteLine("kill.switch=armed");
        Console.WriteLine("This is not Glenn Verify and not send authorization.");
        Console.WriteLine("Double-click TitanCOS.exe to put COS to work. Titan will not type the password.");
        return 0;
    }

    public static int Status(HostPaths paths, KillSwitch kill)
    {
        paths.EnsureDataTree();
        using var store = new HostStore(paths);
        var beat = store.ReadHeartbeat();
        var et = EasternClock.Now();
        Console.WriteLine($"state={HostStateMachine.FromHeartbeat(kill, beat)}");
        Console.WriteLine($"kill={kill.Read()}");
        Console.WriteLine($"rooftop={beat.Rooftop ?? "-"}");
        Console.WriteLine($"verified_at={beat.VerifiedAt ?? "-"}");
        Console.WriteLine($"last_tick_at={beat.LastTickAt ?? "-"}");
        Console.WriteLine($"last_error={beat.LastError ?? "-"}");
        Console.WriteLine($"et={et:o}");
        Console.WriteLine($"sms_open={Policy.SmsOpen(et)}");
        Console.WriteLine($"allowed=28206,28546 forbidden=6220");
        Console.WriteLine($"data={paths.DataRoot}");
        return kill.IsArmed() ? 0 : 3;
    }

    public static int RejectVerifyWord()
    {
        Console.Error.WriteLine("Command Prompt owns that word. Double-click TitanCOS.exe or CHECKIN.cmd.");
        return 2;
    }

    public static async Task<int> CheckInAsync(HostPaths paths, KillSwitch kill)
    {
        if (CloudRefused(out var cloudCode))
        {
            return cloudCode;
        }

        return await VerifyOnceAsync(paths, kill, startChromeIfNeeded: true);
    }

    public static async Task<int> TickAsync(HostPaths paths, KillSwitch kill)
    {
        if (CloudRefused(out var cloudCode))
        {
            return cloudCode;
        }

        using var sessionLock = new SessionLock(paths);
        if (!sessionLock.Acquired)
        {
            Console.Error.WriteLine("Another Titan session lock is held. One live Eleads session only.");
            return 4;
        }

        using var store = new HostStore(paths);
        using var learning = new LearningStore(paths);
        var beat = store.ReadHeartbeat();
        if (!kill.IsArmed())
        {
            var tripped = beat with { State = HostState.KillTripped.ToString(), LastError = "kill.switch is not armed." };
            SessionRecord.Save(paths, store, tripped, glennVerified: false);
            Console.Error.WriteLine("kill.switch is not armed.");
            return 3;
        }

        var state = HostStateMachine.FromHeartbeat(kill, beat);
        if (state is not HostState.VerifiedIdle and not HostState.TickNewLeads and not HostState.TickPlanner
            and not HostState.TickOverdue and not HostState.TickDatabase and not HostState.Looping
            and not HostState.AwaitingSignIn)
        {
            SessionRecord.Save(paths, store, beat.State == state.ToString() ? beat : beat with { State = state.ToString() }, glennVerified: false);
            Console.Error.WriteLine($"Cannot tick from {state}. Double-click TitanCOS.exe after you sign in.");
            return 6;
        }

        if (beat.Rooftop is null || !Policy.IsAllowedRooftop(beat.Rooftop))
        {
            var rejected = beat with { State = HostState.BlockedForbiddenRooftop.ToString(), LastError = "No allowed rooftop on the verified session." };
            SessionRecord.Save(paths, store, rejected, glennVerified: false);
            Console.Error.WriteLine("No allowed rooftop on the verified session.");
            return 6;
        }

        await using var eleads = new EleadsSession();
        try
        {
            await eleads.AttachAsync(paths, allowNavigate: false, startChromeIfNeeded: false);
            await eleads.InspectAsync();
        }
        catch (Exception ex)
        {
            var lost = beat with { LastError = ex.Message, State = HostState.AwaitingVerify.ToString() };
            SessionRecord.Save(paths, store, lost, glennVerified: false);
            Console.Error.WriteLine("Lost Eleads session. Sign in again as Bordine, Glenn. " + ex.Message);
            return 5;
        }

        var admission = TickAdmission.Inspect(eleads.LoggedIn, eleads.Page is not null, eleads.DetectedRooftop);
        if (admission.ExitCode != 0)
        {
            if (admission.ClearVerifiedSession)
            {
                var rejected = beat with { State = admission.State.ToString(), LastError = admission.Error };
                SessionRecord.Save(paths, store, rejected, glennVerified: false);
            }

            Console.Error.WriteLine(admission.Error);
            return admission.ExitCode;
        }

        var et = EasternClock.Now();
        var result = await new WorkTick().RunAsync(eleads.Page!, store, learning, eleads.DetectedRooftop!, et);
        var worked = TickAdmission.AfterWork(result);
        if (worked.ClearVerifiedSession)
        {
            var blocked = beat with
            {
                State = worked.State.ToString(),
                Rooftop = eleads.DetectedRooftop,
                LastError = worked.Error
            };
            SessionRecord.Save(paths, store, blocked, glennVerified: false);
            Console.Error.WriteLine(result);
            return worked.ExitCode;
        }

        var completed = new Heartbeat(
            HostState.VerifiedIdle.ToString(),
            eleads.DetectedRooftop,
            beat.VerifiedAt,
            et.ToString("o"),
            null,
            Policy.SmsOpen(et));
        SessionRecord.Save(paths, store, completed, glennVerified: true);
        Console.WriteLine(result);
        return 0;
    }

    public static int Report(HostPaths paths)
    {
        paths.EnsureDataTree();
        using var store = new HostStore(paths);
        var beat = store.ReadHeartbeat();
        Console.WriteLine($"state={beat.State} rooftop={beat.Rooftop ?? "-"} sms={Policy.SmsOpen(EasternClock.Now())}");
        foreach (var line in store.RecentLogs(20))
        {
            Console.WriteLine(line);
        }

        return 0;
    }

    public static int Stop(HostPaths paths)
    {
        paths.EnsureDataTree();
        File.WriteAllText(paths.StopFlag, EasternClock.Now().ToString("o"));
        using var sessionLock = SessionLock.Acquire(paths, TimeSpan.FromMinutes(2));
        using var store = new HostStore(paths);
        var beat = store.ReadHeartbeat();
        var stopped = beat with { State = HostState.AwaitingVerify.ToString(), LastError = "stopped" };
        SessionRecord.Save(paths, store, stopped, glennVerified: false);
        store.Log("host", "stop", "host stopped; session not closed by typing credentials");
        if (!sessionLock.Acquired)
        {
            Console.Error.WriteLine("Stop is recorded and Verify was cleared. A tick was still finishing.");
            return 4;
        }

        Console.WriteLine("Stopped. kill.switch unchanged.");
        return 0;
    }

    public static int Usage()
    {
        Console.WriteLine("Titan COS — local Windows overlay for 28206 and 28546");
        Console.WriteLine("Double-click TitanCOS.exe to launch. verbs: launch | install | status | checkin | tick | report | stop");
        Console.WriteLine("In Command Prompt, do not type verify by itself.");
        Console.WriteLine("Rooftops 28206 and 28546 only. Never 6220.");
        Console.WriteLine("Titan never stores or types the Eleads password. No cloud tokens.");
        return 1;
    }

    public static async Task<int> VerifyOnceAsync(HostPaths paths, KillSwitch kill, bool startChromeIfNeeded)
    {
        if (!kill.IsArmed())
        {
            VerifyMarker.Write(paths, HostState.KillTripped.ToString(), null, null, null, false);
            Console.Error.WriteLine("kill.switch is not armed. Run install or double-click TitanCOS.exe first.");
            return 3;
        }

        using var sessionLock = new SessionLock(paths);
        if (!sessionLock.Acquired)
        {
            Console.Error.WriteLine("Another Titan session lock is held. One live Eleads session only.");
            return 4;
        }

        await using var eleads = new EleadsSession();
        string report;
        try
        {
            report = await eleads.AttachAsync(paths, allowNavigate: true, startChromeIfNeeded: startChromeIfNeeded);
        }
        catch (Exception ex)
        {
            using var storeErr = new HostStore(paths);
            var failed = new Heartbeat(HostState.AwaitingSignIn.ToString(), null, null, null, ex.Message, Policy.SmsOpen(EasternClock.Now()));
            SessionRecord.Save(paths, storeErr, failed, glennVerified: false);
            storeErr.Log("host", "verify", "browser attach failed: " + ex.Message);
            Console.Error.WriteLine("Chrome attach failed. Install Google Chrome. Titan did not type a password.");
            Console.Error.WriteLine(ex.Message);
            return 5;
        }

        using var store = new HostStore(paths);
        var et = EasternClock.Now();
        var next = eleads.LoggedIn && eleads.DetectedRooftop is not null
            ? HostStateMachine.AfterVerify(eleads.DetectedRooftop)
            : HostState.AwaitingSignIn;
        var verified = next == HostState.VerifiedIdle;
        var error = next == HostState.BlockedForbiddenRooftop ? "TKO 6220 blocked" : eleads.LoggedIn ? null : report;
        var beat = new Heartbeat(
            next.ToString(),
            eleads.DetectedRooftop,
            verified ? et.ToString("o") : null,
            store.ReadHeartbeat().LastTickAt,
            error,
            Policy.SmsOpen(et));
        SessionRecord.Save(paths, store, beat, verified);
        store.Log("host", "verify", report, eleads.DetectedRooftop);
        Console.WriteLine(report);
        return verified ? 0 : 6;
    }

    public static bool CloudRefused(out int code)
    {
        try
        {
            EleadsOpenGuard.RefuseCloudOpen(EleadsOpenGuard.IsCloudLinuxHost, null);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            code = 2;
            return true;
        }

        code = 0;
        return false;
    }

    public static string? ReadOverride(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    public static void WriteHeartbeatFile(HostPaths paths, Heartbeat beat)
        => File.WriteAllText(paths.Heartbeat, System.Text.Json.JsonSerializer.Serialize(beat));
}
