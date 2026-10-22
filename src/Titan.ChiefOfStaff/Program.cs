using Titan.ChiefOfStaff;
using Titan.ChiefOfStaff.Eleads;

if (args.Any(a => a.Contains("password", StringComparison.OrdinalIgnoreCase)
                  || a.Contains("passwd", StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine("Titan never stores or types the Eleads password. Remove that argument.");
    return 2;
}

var verb = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "status";
var paths = new HostPaths(ReadOverride(args, "--data"));
var kill = new KillSwitch(paths);

return verb switch
{
    "install" => Install(),
    "status" => Status(),
    "checkin" => await CheckInAsync(),
    "verify" => RejectVerifyWord(),
    "tick" => await TickAsync(),
    "loop" => await LoopAsync(),
    "report" => Report(),
    "stop" => Stop(),
    _ => Usage()
};

int Install()
{
    paths.EnsureDataTree();
    var state = HostStateMachine.AfterInstall(kill);
    using var store = new HostStore(paths);
    var et = EasternClock.Now();
    store.WriteHeartbeat(new Heartbeat(state.ToString(), null, null, null, null, Policy.SmsOpen(et)));
    store.Log("host", "install", "kill.switch armed — not Verify, not send authorization");
    WriteHeartbeatFile(paths, store.ReadHeartbeat());
    Console.WriteLine($"Installed data root: {paths.DataRoot}");
    Console.WriteLine("kill.switch=armed");
    Console.WriteLine("This is not Glenn Verify and not send authorization.");
    Console.WriteLine("Next: double-click CHECKIN.cmd. Do not type verify by itself — Command Prompt owns that word.");
    return 0;
}

int Status()
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

int RejectVerifyWord()
{
    Console.Error.WriteLine("Command Prompt owns that word. Double-click CHECKIN.cmd.");
    return 2;
}

async Task<int> CheckInAsync()
{
    if (CloudRefused(out var cloudCode))
    {
        return cloudCode;
    }

    return await VerifyAsync();
}

bool CloudRefused(out int code)
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

async Task<int> VerifyAsync()
{
    if (!kill.IsArmed())
    {
        VerifyMarker.Write(paths, HostState.KillTripped.ToString(), null, null, null, false);
        Console.Error.WriteLine("kill.switch is not armed. Run install first.");
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
        report = await eleads.AttachAsync(paths, allowNavigate: true, startChromeIfNeeded: true);
    }
    catch (Exception ex)
    {
        using var storeErr = new HostStore(paths);
        var failed = new Heartbeat(HostState.AwaitingVerify.ToString(), null, null, null, ex.Message, Policy.SmsOpen(EasternClock.Now()));
        storeErr.WriteHeartbeat(failed);
        storeErr.Log("host", "verify", "browser attach failed: " + ex.Message);
        WriteHeartbeatFile(paths, failed);
        VerifyMarker.Write(paths, failed.State, null, null, null, false);
        Console.Error.WriteLine("Chrome attach failed. Install Google Chrome on Alienware. Titan did not type a password.");
        Console.Error.WriteLine(ex.Message);
        return 5;
    }

    using var store = new HostStore(paths);
    var et = EasternClock.Now();
    var next = eleads.LoggedIn && eleads.DetectedRooftop is not null
        ? HostStateMachine.AfterVerify(eleads.DetectedRooftop)
        : HostState.AwaitingVerify;
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

async Task<int> TickAsync()
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
    var beat = store.ReadHeartbeat();
    var state = HostStateMachine.FromHeartbeat(kill, beat);
    if (state is not HostState.VerifiedIdle and not HostState.TickNewLeads and not HostState.TickPlanner
        and not HostState.TickOverdue and not HostState.TickDatabase)
    {
        Console.Error.WriteLine($"Cannot tick from {state}. Run CHECKIN.cmd after you sign in. Do not type verify by itself.");
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
        Console.Error.WriteLine("Lost Eleads session. Glenn must verify again. " + ex.Message);
        return 5;
    }

    if (!eleads.LoggedIn || eleads.Page is null)
    {
        store.WriteHeartbeat(beat with { State = HostState.AwaitingVerify.ToString(), LastError = "session not logged in" });
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
    var result = await new WorkTick().RunAsync(eleads.Page, store, eleads.DetectedRooftop!, et);
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

async Task<int> LoopAsync()
{
    // Local live loop: the Alienware program Glenn runs on the PC itself.
    // One tick per cycle against the already-verified Chrome (CDP 9222).
    // Never opens Eleads from scratch, never types a password, and stops
    // the moment Verify is lost or the kill switch trips.
    var maxCycles = ReadIntOverride(args, "--cycles") ?? 0; // 0 = until stopped
    var overrideMinutes = ReadIntOverride(args, "--minutes");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
        Console.WriteLine("Stopping Titan COS loop after this cycle (Ctrl+C)...");
    };

    Console.WriteLine("Titan Autonomous COS — local live CRM loop");
    Console.WriteLine($"Identity {Policy.Identity}. Rooftops 28206, 28546 only. Never 6220.");
    Console.WriteLine("Work order 0 first: inbound replies and Bordine new leads, then Daily Organizer Open/All.");
    Console.WriteLine("Stop any time with Ctrl+C, the app Stop button, or by tripping kill.switch.");

    var cycle = 0;
    while (!cts.IsCancellationRequested)
    {
        cycle++;
        var et = EasternClock.Now();
        Console.WriteLine($"--- Loop cycle {cycle} at {et:yyyy-MM-dd HH:mm:ss} ET, pace {LoopPace.Describe(et)} ---");

        var code = await TickAsync();
        if (code != 0)
        {
            Console.WriteLine($"Loop stopped: tick returned {code}. Sign in and run CHECKIN.cmd again if Verify was lost.");
            return code;
        }

        if (maxCycles > 0 && cycle >= maxCycles)
        {
            Console.WriteLine($"Loop finished {cycle} cycle(s). Chrome left open.");
            return 0;
        }

        var wait = overrideMinutes is > 0 ? TimeSpan.FromMinutes(overrideMinutes.Value) : LoopPace.Interval(EasternClock.Now());
        Console.WriteLine($"Next cycle in {(int)wait.TotalMinutes} min. Leave Chrome open.");
        try
        {
            await Task.Delay(wait, cts.Token);
        }
        catch (TaskCanceledException)
        {
            break;
        }
    }

    Console.WriteLine("Titan COS loop stopped. Chrome left open.");
    return 0;
}

int Report()
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

int Stop()
{
    using var store = new HostStore(paths);
    var beat = store.ReadHeartbeat();
    store.WriteHeartbeat(beat with { State = HostState.AwaitingVerify.ToString(), LastError = "stopped" });
    store.Log("host", "stop", "host stopped; session not closed by typing credentials");
    Console.WriteLine("Stopped. kill.switch unchanged.");
    return 0;
}

int Usage()
{
    Console.WriteLine("Titan.ChiefOfStaff — Alienware local host");
    Console.WriteLine("verbs: install | status | checkin | tick | loop | report | stop");
    Console.WriteLine("loop options: --cycles N (0 = until stopped), --minutes N (override pace)");
    Console.WriteLine("In Command Prompt, do not type verify by itself. Run CHECKIN.cmd");
    Console.WriteLine("Rooftops 28206 and 28546 only. Never 6220.");
    Console.WriteLine("Titan never stores or types the Eleads password.");
    return 1;
}

static int? ReadIntOverride(string[] args, string name)
{
    var raw = ReadOverride(args, name);
    return int.TryParse(raw, out var value) ? value : null;
}

static string? ReadOverride(string[] args, string name)
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

static void WriteHeartbeatFile(HostPaths paths, Heartbeat beat)
{
    File.WriteAllText(paths.Heartbeat, System.Text.Json.JsonSerializer.Serialize(beat));
}
