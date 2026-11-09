using System.Text.Json;
using Titan.ChiefOfStaff;
using Titan.ChiefOfStaff.Eleads;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class LaunchGuardTests
{
    [Fact]
    public void Rooftop_guard_rejects_6220()
    {
        Assert.False(Policy.IsAllowedRooftop("6220"));
        Assert.False(Policy.IsAllowedRooftop(" 6220 "));
        Assert.True(Policy.IsForbiddenRooftop("The TKO Autogroup (6220)"));
        Assert.True(Policy.IsAllowedRooftop("28206"));
        Assert.True(Policy.IsAllowedRooftop("28546"));
        Assert.Equal(HostState.BlockedForbiddenRooftop, HostStateMachine.AfterVerify("6220"));
        Assert.Equal(HostState.VerifiedIdle, HostStateMachine.AfterVerify("28206"));
        Assert.Equal(HostState.VerifiedIdle, HostStateMachine.AfterVerify("28546"));
    }

    [Fact]
    public void Ownership_screen_is_180_days()
    {
        var now = EasternClock.AtLocal(2026, 10, 16, 12, 0);
        Assert.Equal(180, Policy.OwnershipScreenDays);
        Assert.Null(Policy.OwnershipScreenMonths);
        Assert.True(Policy.PassesOwnershipScreen(now, now.AddDays(-180), recentInquiry: false, modelYear: null));
        Assert.False(Policy.PassesOwnershipScreen(now, now.AddDays(-181), recentInquiry: false, modelYear: null));
        Assert.True(Policy.PassesOwnershipScreen(now, now.AddDays(-400), recentInquiry: true, modelYear: 2014));
        Assert.False(Policy.PassesOwnershipScreen(now, purchasedAt: null, recentInquiry: false, modelYear: 2024));
        Assert.Contains("180", Policy.OwnershipBlock(now, now.AddDays(-181), recentInquiry: false, modelYear: null) ?? "");
        Assert.Contains("Model year", Policy.OwnershipBlock(now, null, recentInquiry: false, modelYear: 2024) ?? "");
        Assert.Null(Policy.OwnershipBlock(now, now.AddDays(-400), recentInquiry: true, modelYear: 2014));

        var held = new OrganizerRow(
            "past-owner",
            "Bordine, Glenn",
            WorkQueue.Database,
            null,
            null,
            false,
            "Customer owns a vehicle.",
            PurchasedAt: now.AddDays(-200),
            RecentInquiry: false,
            ModelYear: 2019);
        Assert.Contains("180-day", WorkOrder.HoldReason(held, now) ?? "");
        Assert.Null(WorkOrder.HoldReason(held with { RecentInquiry = true }, now));

        var unproven = new OrganizerRow(
            "equity Jane",
            "Bordine, Glenn",
            WorkQueue.Database,
            null,
            null,
            true,
            "Customer asked about equity.",
            PurchasedAt: null,
            RecentInquiry: false);
        Assert.Contains("unproven", WorkOrder.HoldReason(unproven, now) ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Null(WorkOrder.HoldReason(unproven with { RecentInquiry = true }, now));
        Assert.Null(WorkOrder.HoldReason(unproven with { PurchasedAt = now.AddDays(-30) }, now));
    }

    [Fact]
    public void Sms_window_is_9_to_19_america_new_york()
    {
        var zone = EasternClock.Zone();
        Assert.Equal("America/New_York", zone.Id);

        var open = EasternClock.AtLocal(2026, 10, 16, 9, 0);
        var lastOpen = EasternClock.AtLocal(2026, 10, 16, 18, 59);
        var closes = EasternClock.AtLocal(2026, 10, 16, 19, 0);
        var eightPm = EasternClock.AtLocal(2026, 10, 16, 20, 0);
        Assert.True(Policy.SmsOpenEastern(open));
        Assert.True(Policy.SmsOpenEastern(lastOpen));
        Assert.False(Policy.SmsOpenEastern(closes));
        Assert.False(Policy.SmsOpenEastern(eightPm));

        var nineUtc = DateTimeOffset.Parse("2026-10-16T13:00:00Z");
        var nineteenUtc = DateTimeOffset.Parse("2026-10-16T23:00:00Z");
        var eightPmUtc = DateTimeOffset.Parse("2026-10-17T00:00:00Z");
        Assert.True(Policy.SmsOpenEastern(nineUtc));
        Assert.False(Policy.SmsOpenEastern(nineteenUtc));
        Assert.False(Policy.SmsOpenEastern(eightPmUtc));
    }

    [Fact]
    public void Appointment_set_requires_day_and_time()
    {
        Assert.NotNull(Policy.AppointmentSetBlock("Maybe Saturday"));
        Assert.NotNull(Policy.AppointmentSetBlock("Saturday"));
        Assert.NotNull(Policy.AppointmentSetBlock("10:30 AM"));
        Assert.NotNull(Policy.AppointmentSetBlock(""));
        Assert.NotNull(Policy.AppointmentSetBlock(null));
        Assert.Null(Policy.AppointmentSetBlock("Saturday at 10:30 AM"));
        Assert.Null(Policy.AppointmentSetBlock("Wednesday at 2 pm"));
        Assert.False(Policy.HasCustomerDayAndTime("Maybe Saturday"));
        Assert.True(Policy.HasCustomerDayAndTime("Saturday at 10:30 AM"));
        Assert.NotNull(Policy.AppointmentSetBlock("I sat at 10:30 AM and went over the numbers."));
        Assert.False(Policy.HasCustomerDayAndTime("I sat at 10:30 AM and went over the numbers."));
        Assert.False(Policy.HasCustomerDayAndTime("Customer sat at 2:00 PM."));
        Assert.False(Policy.HasCustomerDayAndTime("in the sun at 4:00 PM"));
        Assert.Null(Policy.AppointmentSetBlock("Sat at 10:30 AM"));
        Assert.True(Policy.HasCustomerDayAndTime("Sat at 10:30 AM"));
        Assert.Null(Policy.AppointmentSetBlock("Sunday at 4:00 PM"));
        Assert.NotNull(Policy.AppointmentSetBlock("Called on Monday at 2:30 PM, no answer."));
        Assert.False(Policy.HasCustomerDayAndTime("Called on Monday at 2:30 PM, no answer."));
        Assert.False(Policy.HasCustomerDayAndTime("Emailed on 10/26 at 2:30 PM about the Sentra."));
        Assert.Null(Policy.AppointmentSetBlock("I can come on Monday at 2:30 PM"));
        Assert.True(Policy.HasCustomerDayAndTime("I can come on Monday at 2:30 PM"));
        Assert.NotNull(Policy.AppointmentSetBlock("Customer asked about Saturday. We open at 9:00 AM."));
        Assert.False(Policy.HasCustomerDayAndTime("Are you open Saturday at 9:00 AM?"));
        Assert.False(Policy.HasCustomerDayAndTime("Are you open on Saturday at 9:00 AM?"));
        Assert.False(Policy.HasCustomerDayAndTime("What time do you close on Saturday at 5:00 PM?"));
        Assert.False(Policy.HasCustomerDayAndTime("Looking at a 1/2 ton. Call me sometime."));
        Assert.False(Policy.HasCustomerDayAndTime("maybe 12 at 2 pm"));
        Assert.True(Policy.HasCustomerDayAndTime("I can come May 5 at 2:00 PM"));
        Assert.Null(FactLedger.QuoteDayAndTime("Customer asked about Saturday. We open at 9:00 AM."));
        Assert.Equal("May 5 2:00 PM", FactLedger.QuoteDayAndTime("I can come May 5 at 2:00 PM"));
        Assert.Null(FactLedger.QuoteDayAndTime("The 1/2 ton at 10:30 AM is the one I want."));
        Assert.Equal("Saturday 10:30 AM", FactLedger.QuoteDayAndTime("The 1/2 ton is nice. I can come Saturday at 10:30 AM."));
    }

    [Fact]
    public void Cloud_host_refuses_to_open_eleads()
    {
        Assert.Null(EleadsOpenGuard.NavigationTarget(isCloudLinux: true, allowNavigate: true, currentUrl: null));
        Assert.Null(EleadsOpenGuard.NavigationTarget(isCloudLinux: true, allowNavigate: true, currentUrl: "about:blank"));
        var refused = Assert.Throws<InvalidOperationException>(
            () => EleadsOpenGuard.RefuseCloudOpen(isCloudLinux: true, EleadsOpenGuard.LoginUrl));
        Assert.Contains("refuses to open Eleads", refused.Message, StringComparison.Ordinal);

        if (!OperatingSystem.IsWindows())
        {
            Assert.True(EleadsOpenGuard.IsCloudLinuxHost);
            Assert.Null(EleadsOpenGuard.NavigationTarget(
                EleadsOpenGuard.IsCloudLinuxHost,
                allowNavigate: true,
                currentUrl: null));
        }

        EleadsOpenGuard.RefuseCloudOpen(isCloudLinux: false, EleadsOpenGuard.LoginUrl);
        Assert.Equal(
            EleadsOpenGuard.LoginUrl,
            EleadsOpenGuard.NavigationTarget(isCloudLinux: false, allowNavigate: true, currentUrl: "about:blank"));
        Assert.Null(EleadsOpenGuard.NavigationTarget(isCloudLinux: false, allowNavigate: false, currentUrl: "about:blank"));
    }

    [Fact]
    public void Tick_keeps_verify_when_the_logged_in_tab_has_no_page_handle()
    {
        var unbound = TickAdmission.Inspect(loggedIn: true, pageBound: false, rooftop: "28206");
        Assert.False(unbound.ClearVerifiedSession);
        Assert.Equal(6, unbound.ExitCode);
        Assert.Equal(HostState.VerifiedIdle, unbound.State);

        var ready = TickAdmission.Inspect(loggedIn: true, pageBound: true, rooftop: "28546");
        Assert.Equal(0, ready.ExitCode);
        Assert.False(ready.ClearVerifiedSession);
    }

    [Fact]
    public void Tick_clears_verify_when_the_session_or_rooftop_is_rejected()
    {
        var loggedOut = TickAdmission.Inspect(loggedIn: false, pageBound: true, rooftop: "28206");
        Assert.True(loggedOut.ClearVerifiedSession);
        Assert.Equal(HostState.AwaitingSignIn, loggedOut.State);
        Assert.Equal(6, loggedOut.ExitCode);

        var forbidden = TickAdmission.Inspect(loggedIn: true, pageBound: true, rooftop: "6220");
        Assert.True(forbidden.ClearVerifiedSession);
        Assert.Equal(HostState.BlockedForbiddenRooftop, forbidden.State);
        Assert.Equal(7, forbidden.ExitCode);

        var blocked = TickAdmission.AfterWork("Blocked: TKO Autogroup detected.");
        Assert.True(blocked.ClearVerifiedSession);
        Assert.Equal(7, blocked.ExitCode);
        Assert.Equal(0, TickAdmission.AfterWork("Tick 28206: 0 visible rows, 0 qualified, 0 held.").ExitCode);
    }

    [Fact]
    public void Saving_a_rejected_session_clears_the_verify_receipt()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-record-" + Guid.NewGuid().ToString("n"));
        try
        {
            var paths = new HostPaths(root);
            using var store = new HostStore(paths);
            SessionRecord.Save(paths, store, new Heartbeat("VerifiedIdle", "28206", "2026-10-16T12:00:00-04:00", null, null, true), true);
            using (var verified = JsonDocument.Parse(File.ReadAllText(paths.VerifyMarker)))
            {
                Assert.True(verified.RootElement.GetProperty("glennVerified").GetBoolean());
            }

            SessionRecord.Save(
                paths,
                store,
                new Heartbeat("BlockedForbiddenRooftop", "6220", null, null, "Forbidden rooftop. TKO 6220 is blocked.", false),
                false);
            using var doc = JsonDocument.Parse(File.ReadAllText(paths.VerifyMarker));
            Assert.False(doc.RootElement.GetProperty("glennVerified").GetBoolean());
            Assert.Equal("BlockedForbiddenRooftop", doc.RootElement.GetProperty("state").GetString());
            Assert.Equal("6220", doc.RootElement.GetProperty("rooftop").GetString());
            Assert.Equal("BlockedForbiddenRooftop", store.ReadHeartbeat().State);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Looping_heartbeat_keeps_a_verified_idle_receipt()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-loop-receipt-" + Guid.NewGuid().ToString("n"));
        try
        {
            var paths = new HostPaths(root);
            using var store = new HostStore(paths);
            SessionRecord.Save(
                paths,
                store,
                new Heartbeat("Looping", "28206", "2026-10-16T12:00:00-04:00", "2026-10-16T12:15:00-04:00", null, true),
                true);
            Assert.Equal("Looping", store.ReadHeartbeat().State);
            using var doc = JsonDocument.Parse(File.ReadAllText(paths.VerifyMarker));
            Assert.True(doc.RootElement.GetProperty("glennVerified").GetBoolean());
            Assert.Equal("VerifiedIdle", doc.RootElement.GetProperty("state").GetString());
            Assert.Equal("28206", doc.RootElement.GetProperty("rooftop").GetString());
            Assert.Equal(Policy.Identity, doc.RootElement.GetProperty("identity").GetString());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Stop_and_kill_clear_the_verify_receipt_after_a_live_cycle()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-exit-receipt-" + Guid.NewGuid().ToString("n"));
        try
        {
            var paths = new HostPaths(root);
            var kill = new KillSwitch(paths);
            kill.Arm();
            using var store = new HostStore(paths);
            SessionRecord.Save(
                paths,
                store,
                new Heartbeat("Looping", "28546", "2026-10-16T12:00:00-04:00", "2026-10-16T12:15:00-04:00", null, true),
                true);

            File.WriteAllText(paths.StopFlag, "stop");
            LaunchHost.WriteExitReceipt(paths, store, kill);
            using (var stopped = JsonDocument.Parse(File.ReadAllText(paths.VerifyMarker)))
            {
                Assert.False(stopped.RootElement.GetProperty("glennVerified").GetBoolean());
                Assert.Equal("AwaitingVerify", stopped.RootElement.GetProperty("state").GetString());
            }

            Assert.Equal(0, CommandRouter.Stop(paths));
            using (var afterStop = JsonDocument.Parse(File.ReadAllText(paths.VerifyMarker)))
            {
                Assert.False(afterStop.RootElement.GetProperty("glennVerified").GetBoolean());
            }

            SessionRecord.Save(
                paths,
                store,
                new Heartbeat("Looping", "28546", "2026-10-16T12:00:00-04:00", "2026-10-16T12:20:00-04:00", null, true),
                true);
            File.WriteAllText(paths.KillSwitch, "tripped" + Environment.NewLine);
            LaunchHost.WriteExitReceipt(paths, store, kill);
            using var tripped = JsonDocument.Parse(File.ReadAllText(paths.VerifyMarker));
            Assert.False(tripped.RootElement.GetProperty("glennVerified").GetBoolean());
            Assert.Equal("KillTripped", tripped.RootElement.GetProperty("state").GetString());
            Assert.Equal("KillTripped", store.ReadHeartbeat().State);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
