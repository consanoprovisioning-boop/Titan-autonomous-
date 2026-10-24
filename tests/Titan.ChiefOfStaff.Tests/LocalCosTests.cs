using Titan.ChiefOfStaff;
using Titan.ChiefOfStaff.Eleads;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class LocalCosTests
{
    [Fact]
    public void Self_install_copies_and_arms()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-self-" + Guid.NewGuid().ToString("n"));
        var source = Path.Combine(root, "src");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "TitanCOS.exe"), "exe");
        var paths = new HostPaths(Path.Combine(root, "data"));
        var result = SelfInstall.Ensure(paths, source);
        Assert.True(result.Armed);
        Assert.True(result.Copied);
        Assert.True(File.Exists(Path.Combine(paths.AppDir, "TitanCOS.exe")));
        Assert.Equal("armed", File.ReadAllText(paths.KillSwitch).Trim());
        Assert.Contains("not Verify", result.Summary, StringComparison.OrdinalIgnoreCase);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Launch_prepare_does_not_rearm_a_tripped_kill_switch()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-trip-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        paths.EnsureDataTree();
        File.WriteAllText(paths.KillSwitch, "tripped" + Environment.NewLine);
        using (var store = new HostStore(paths))
        {
            store.WriteHeartbeat(new Heartbeat(nameof(HostState.VerifiedIdle), "28206", null, null, null, false));
        }

        var kill = new KillSwitch(paths);
        var result = LaunchHost.Prepare(paths, kill, root);
        Assert.False(result.Armed);
        Assert.Equal("tripped", kill.Read());
        Assert.Contains("tripped", result.Summary, StringComparison.OrdinalIgnoreCase);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Launch_prepare_does_not_recreate_a_deleted_kill_switch()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-delkill-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        using (var store = new HostStore(paths))
        {
            store.WriteHeartbeat(new Heartbeat(nameof(HostState.AwaitingVerify), null, null, null, "stopped", false));
        }

        var kill = new KillSwitch(paths);
        var result = LaunchHost.Prepare(paths, kill, root);
        Assert.False(File.Exists(paths.KillSwitch));
        Assert.False(result.Armed);
        Assert.Equal("missing", kill.Read());
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Explicit_install_still_rearms_a_tripped_switch()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-rearm-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        paths.EnsureDataTree();
        File.WriteAllText(paths.KillSwitch, "tripped" + Environment.NewLine);
        using (var store = new HostStore(paths))
        {
            store.WriteHeartbeat(new Heartbeat(nameof(HostState.VerifiedIdle), "28206", null, null, null, false));
        }

        var kill = new KillSwitch(paths);
        Assert.False(SelfInstall.ArmIfFirstInstall(paths, kill));
        Assert.Equal(HostState.AwaitingVerify, HostStateMachine.AfterInstall(kill));
        Assert.Equal("armed", kill.Read());
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Launch_prepare_writes_overlay_without_chrome()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-prep-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        var kill = new KillSwitch(paths);
        var result = LaunchHost.Prepare(paths, kill, root);
        Assert.True(result.Armed);
        Assert.True(kill.IsArmed());
        Assert.True(File.Exists(paths.OverlayJson));
        var state = OverlayState.Read(paths);
        Assert.Contains("Titan COS", state.Title, StringComparison.Ordinal);
        Assert.False(state.Verified);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Loop_pace_matches_live_windows()
    {
        var sms = EasternClock.AtLocal(2026, 10, 16, 10, 0);
        var evening = EasternClock.AtLocal(2026, 10, 16, 20, 0);
        var overnight = EasternClock.AtLocal(2026, 10, 17, 2, 0);
        Assert.Equal(TimeSpan.FromMinutes(15), LoopPace.NextDelay(sms, hostBlocked: false, sentThisSession: true));
        Assert.Equal(TimeSpan.FromMinutes(30), LoopPace.NextDelay(evening, hostBlocked: false, sentThisSession: true));
        Assert.Equal(TimeSpan.FromMinutes(45), LoopPace.NextDelay(overnight, hostBlocked: false, sentThisSession: true));
        Assert.Equal(TimeSpan.FromMinutes(30), LoopPace.NextDelay(sms, hostBlocked: true, sentThisSession: true));
    }

    [Fact]
    public async Task Continuous_loop_runs_until_stop_flag()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-loop-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        var kill = new KillSwitch(paths);
        kill.Arm();
        using var overlay = new FileOverlay(paths);
        var cycles = 0;
        var loop = new ContinuousLoop();
        var result = await loop.RunAsync(
            paths,
            kill,
            overlay,
            () =>
            {
                cycles++;
                File.WriteAllText(paths.StopFlag, "stop");
                return Task.FromResult(new CycleSnapshot(true, false, 0, "ok", "ok", "now", "mem", "working"));
            },
            (_, _) => Task.CompletedTask,
            CancellationToken.None);
        Assert.Equal(0, result);
        Assert.Equal(1, cycles);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Fact_ledger_refuses_invented_money()
    {
        var ledger = FactLedger.FromHistory("Customer: Pat asked about a Sentra. Saturday at 10:30 AM?", "28206", "Bordine, Glenn");
        Assert.Equal("Pat", ledger.Get("firstName"));
        Assert.Equal("Sentra", ledger.Get("vehicle"));
        Assert.Equal("28206", ledger.Get("rooftop"));
        Assert.False(ledger.Allows("I can get you $299/mo at 1.9 APR"));
        Assert.True(FactLedger.ContainsInventedMoney("1.9%"));
        Assert.Null(FactLedger.FirstName("Hello there"));
        Assert.Null(FactLedger.VehicleMention("Customer owns a vehicle."));
    }

    [Fact]
    public void Grounded_draft_blocks_missing_history_and_forbidden_copy()
    {
        var now = EasternClock.AtLocal(2026, 10, 16, 10, 0);
        var empty = GroundedDraft.Email(new FactLedger(), now, "");
        Assert.False(empty.Ok);
        Assert.Contains("history", empty.Block ?? "", StringComparison.OrdinalIgnoreCase);

        var history = "Customer: Pat asked about a Sentra. Interested in coming in?";
        var ledger = FactLedger.FromHistory(history, "28206", "Bordine, Glenn");
        var email = GroundedDraft.Email(ledger, now, history);
        Assert.True(email.Ok);
        Assert.Contains("Pat", email.Body);
        Assert.Contains("Sentra", email.Body);
        Assert.DoesNotContain("$", email.Body);
        Assert.DoesNotContain("APR", email.Body, StringComparison.OrdinalIgnoreCase);

        var night = EasternClock.AtLocal(2026, 10, 16, 20, 0);
        var sms = GroundedDraft.Sms(ledger, night, history);
        Assert.False(sms.Ok);
        Assert.Contains("SMS window", sms.Block ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void Learning_store_ranks_successful_selectors()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-learn-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        using (var store = new LearningStore(paths))
        {
            store.RememberSelector("Valdosta Nissan", true);
            store.RememberSelector("Valdosta Nissan", true);
            store.RememberSelector("Nissan", false);
            store.RecordExperience("28206", "NewLeads", 1200, 2, 1, 0, "ok");
            store.RememberDraft("email", "28206", sent: true, reply: true);
            var ranked = store.RankedSelectors(["Nissan", "Valdosta Nissan"]);
            Assert.Equal("Valdosta Nissan", ranked[0]);
            var snap = store.Snapshot();
            Assert.Equal(1, snap.Cycles);
            Assert.Equal(2, snap.Qualified);
            Assert.Equal(1, snap.Sent);
        }

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Dual_rooftop_never_switches_to_6220()
    {
        Assert.Equal("28546", DualRooftop.OtherAllowed("28206"));
        Assert.Equal("28206", DualRooftop.OtherAllowed("28546"));
        Assert.Null(DualRooftop.OtherAllowed("6220"));
        Assert.Empty(DualRooftop.SwitchCandidates("6220"));
        Assert.True(DualRooftop.ForbiddenSwitch("The TKO Autogroup (6220)"));
        Assert.False(DualRooftop.ForbiddenSwitch("Valdosta Nissan"));
        Assert.Contains("28206", DualRooftop.SwitchCandidates("28206"));
        Assert.Contains("28546", DualRooftop.SwitchCandidates("28546"));
    }

    [Fact]
    public void Send_proof_requires_identity_and_channel()
    {
        Assert.False(AutonomousWork.LooksLikeSendProof("preview only", "email"));
        Assert.False(AutonomousWork.LooksLikeSendProof("Email queued", "email"));
        Assert.False(AutonomousWork.LooksLikeSendProof("Bordine, Glenn\nSend Email", "email"));
        Assert.False(AutonomousWork.LooksLikeSendProof("Glenn\nSend SMS", "SMS"));
        Assert.True(AutonomousWork.LooksLikeSendProof("Bordine, Glenn\nManual email completed via system.", "email"));
        Assert.True(AutonomousWork.LooksLikeSendProof("Glenn\nSMS text completed via system.", "SMS"));
        Assert.False(AutonomousWork.LooksLikeSendProof("The TKO Autogroup (6220) email", "email"));

        var prior = "Bordine, Glenn\nManual email completed via system.\nSend Email";
        Assert.False(AutonomousWork.LooksLikeNewSendProof(prior, prior, "email"));
        Assert.True(AutonomousWork.LooksLikeNewSendProof(prior, prior + "\nManual email completed via system.", "email"));
        var priorSms = "Glenn\nSMS text completed via system.";
        Assert.False(AutonomousWork.LooksLikeNewSendProof(priorSms, priorSms + "\nSend SMS", "SMS"));
        Assert.True(AutonomousWork.LooksLikeNewSendProof(priorSms, priorSms + "\nSMS text completed via system.", "SMS"));
    }

    [Fact]
    public void Specialists_are_seven_not_fifty_one()
    {
        Assert.Equal(7, SpecialistCore.SpecialistNames.Count);
        Assert.Equal(51, SpecialistCore.CatalogSize);
        var now = EasternClock.AtLocal(2026, 10, 16, 10, 0);
        var row = new OrganizerRow("Pat new lead", "Bordine, Glenn", WorkQueue.NewLeads, now, now, true, "Customer: Pat asked about a Sentra.");
        var results = SpecialistCore.Run("28206", [row], now, [], []);
        Assert.Equal(7, results.Count);
        Assert.DoesNotContain(results, item => item.Rooftop == "6220");
        var blocked = SpecialistCore.Run("6220", [row], now, [], []);
        Assert.All(blocked, item => Assert.Equal("blocked", item.Status));
    }

    [Fact]
    public void Appointment_slots_stay_inside_windows()
    {
        var friday = EasternClock.AtLocal(2026, 10, 16, 10, 0);
        var slots = AppointmentSlots.NextTwo(friday);
        Assert.Equal(2, slots.Count);
        Assert.DoesNotContain(slots, slot => slot.Contains("Sunday", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cloud_launch_verb_is_refused()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var root = Path.Combine(Path.GetTempPath(), "titan-cloud-" + Guid.NewGuid().ToString("n"));
        var code = await CommandRouter.RunAsync(["launch", "--data", root], defaultLaunch: true);
        Assert.Equal(2, code);
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Recent_send_attempt_blocks_another_click_for_12_hours()
    {
        var now = EasternClock.AtLocal(2026, 10, 16, 10, 0);
        Assert.Null(AutonomousWork.RecentSendSkip(null, now));
        Assert.Null(AutonomousWork.RecentSendSkip(now.AddHours(-12), now));
        Assert.NotNull(AutonomousWork.RecentSendSkip(now.AddHours(-12).AddMinutes(1), now));
        Assert.NotNull(AutonomousWork.RecentSendSkip(now.AddMinutes(-15), now));

        var root = Path.Combine(Path.GetTempPath(), "titan-spacing-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        using (var store = new HostStore(paths))
        {
            store.Log("NewLeads", "send", "Send control not found — no send counted", "28206", "Pat new lead", now.AddMinutes(-10));
            store.Log("NewLeads", "hold", "12h spacing — this customer already has a send attempt. Not clicking Send again.", "28206", "Pat new lead", now.AddMinutes(-5));
            Assert.Null(store.LatestSendAttemptAt("28206", "Pat new lead"));

            store.Log("NewLeads", "send", "Manual email completed via system.", "28206", "Pat new lead", now.AddHours(-13));
            Assert.Null(AutonomousWork.RecentSendSkip(store.LatestSendAttemptAt("28206", "Pat new lead"), now));

            store.Log("NewLeads", "send-attempt", "Send clicked; CRM proof still required", "28206", "Pat new lead", now.AddMinutes(-15));
            Assert.NotNull(AutonomousWork.RecentSendSkip(store.LatestSendAttemptAt("28206", "Pat new lead"), now));

            store.Log("NewLeads", "send", "SMS text completed via system.", "28206", "Other customer", now.AddMinutes(-5));
            Assert.Null(store.LatestSendAttemptAt("28546", "Pat new lead"));
            Assert.NotNull(AutonomousWork.RecentSendSkip(store.LatestSendAttemptAt("28206", "Other customer"), now));
            Assert.NotNull(AutonomousWork.RecentSendSkip(store.LatestSendAttemptAt("28206", "Pat new lead"), now));
        }

        Directory.Delete(root, recursive: true);
    }
}
