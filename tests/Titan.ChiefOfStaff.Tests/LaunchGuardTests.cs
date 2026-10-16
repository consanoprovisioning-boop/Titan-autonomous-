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
}
