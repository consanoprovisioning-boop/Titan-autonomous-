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
        var result = SelfInstall.Ensure(paths, AppContext.BaseDirectory);
        var state = HostStateMachine.AfterInstall(kill);
        using var store = new HostStore(paths);
        var et = EasternClock.Now();
        store.WriteHeartbeat(new Heartbeat(state.ToString(), null, null, null, null, Policy.SmsOpen(et)));
        store.Log("host", "install", result.Summary);
        WriteHeartbeatFile(paths, store.ReadHeartbeat());
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

        if (!kill.IsArmed())
        {
            Console.Error.WriteLine("kill.switch is not armed.");
            return 3;
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
        var state = HostStateMachine.FromHeartbeat(kill, beat);
        if (state is not HostState.VerifiedIdle and not HostState.TickNewLeads and not HostState.TickPlanner
            and not HostState.TickOverdue and not HostState.TickDatabase and not HostState.Looping
            and not HostState.AwaitingSignIn)
        {
            Console.Error.WriteLine($"Cannot tick from {state}. Double-click TitanCOS.exe after you sign in.");
            return 6;
        }

        if (beat.Rooftop is null || !Policy.IsAllowedRooftop(beat.Rooftop))
        {
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
            store.WriteHeartbeat(beat with { LastError = ex.Message, State = HostState.AwaitingVerify.ToString() });
            Console.Error.WriteLine("Lost Eleads session. Sign in again as Bordine, Glenn. " + ex.Message);
            return 5;
        }

        if (!eleads.LoggedIn || eleads.Page is null)
        {
            store.WriteHeartbeat(beat with { State = HostState.AwaitingSignIn.ToString(), LastError = "session not logged in" });
            Console.Error.WriteLine("Eleads session is not verified. Titan will not type the password.");
            return 6;
        }

        if (eleads.DetectedRooftop == Policy.ForbiddenTkoId || !Policy.IsAllowedRooftop(eleads.DetectedRooftop))
        {
            store.WriteHeartbeat(beat with { State = HostState.BlockedForbiddenRooftop.ToString(), LastError = "forbidden rooftop" });
            Console.Error.WriteLine("Forbidden rooftop. TKO 6220 is blocked.");
            return 7;
        }

        var et = EasternClock.Now();
        var result = await new WorkTick().RunAsync(eleads.Page, store, learning, eleads.DetectedRooftop!, et);
        store.WriteHeartbeat(new Heartbeat(
            HostState.VerifiedIdle.ToString(),
            eleads.DetectedRooftop,
            beat.VerifiedAt,
            et.ToString("o"),
            null,
            Policy.SmsOpen(et)));
        WriteHeartbeatFile(paths, store.ReadHeartbeat());
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
        using var store = new HostStore(paths);
        var beat = store.ReadHeartbeat();
        File.WriteAllText(paths.StopFlag, EasternClock.Now().ToString("o"));
        store.WriteHeartbeat(beat with { State = HostState.AwaitingVerify.ToString(), LastError = "stopped" });
        store.Log("host", "stop", "host stopped; session not closed by typing credentials");
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
            storeErr.WriteHeartbeat(failed);
            storeErr.Log("host", "verify", "browser attach failed: " + ex.Message);
            WriteHeartbeatFile(paths, failed);
            VerifyMarker.Write(paths, failed.State, null, null, null, false);
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
        store.WriteHeartbeat(beat);
        store.Log("host", "verify", report, eleads.DetectedRooftop);
        WriteHeartbeatFile(paths, beat);
        VerifyMarker.Write(paths, beat.State, beat.Rooftop, verified ? Policy.Identity : null, verified ? beat.VerifiedAt : null, verified);
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
