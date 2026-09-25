using Titan.ChiefOfStaff;
using Titan.ChiefOfStaff.Eleads;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class PolicyTests
{
    [Theory]
    [InlineData("28206", true)]
    [InlineData("28546", true)]
    [InlineData("6220", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Rooftop_lock(string? id, bool allowed) => Assert.Equal(allowed, Policy.IsAllowedRooftop(id));

    [Fact]
    public void Tko_text_is_forbidden()
    {
        Assert.True(Policy.IsForbiddenRooftop("The TKO Autogroup (6220)"));
        Assert.False(Policy.IsForbiddenRooftop("Valdosta Nissan 28206"));
    }

    [Fact]
    public void Sms_window_is_9_to_7_et_and_never_sunday()
    {
        var fridayOpen = new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.FromHours(-4));
        var fridayClosed = new DateTimeOffset(2026, 9, 25, 19, 0, 0, TimeSpan.FromHours(-4));
        var sunday = new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.FromHours(-4));
        Assert.True(Policy.SmsOpen(fridayOpen));
        Assert.False(Policy.SmsOpen(fridayClosed));
        Assert.False(Policy.SmsOpen(sunday));
    }

    [Fact]
    public void Tuesday_appointments_require_customer_request()
    {
        var tue = new DateTimeOffset(2026, 9, 22, 11, 0, 0, TimeSpan.FromHours(-4));
        Assert.False(Policy.AppointmentWindowOpen(tue, customerRequestedTuesday: false));
        Assert.True(Policy.AppointmentWindowOpen(tue, customerRequestedTuesday: true));
    }

    [Fact]
    public void Other_consultant_needs_96h_silence_unless_titan_touched()
    {
        var now = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(-4));
        Assert.False(Policy.OtherConsultantEligible(now, now.AddHours(-10), titanTouched: false));
        Assert.False(Policy.OtherConsultantEligible(now, null, titanTouched: false));
        Assert.True(Policy.OtherConsultantEligible(now, now.AddHours(-97), titanTouched: false));
        Assert.True(Policy.OtherConsultantEligible(now, now.AddHours(-1), titanTouched: true));
    }

    [Fact]
    public void Seventy_two_hour_hold()
    {
        var now = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(-4));
        Assert.True(Policy.OtherConsultantHeldByRecentContact(now, now.AddHours(-10)));
        Assert.False(Policy.OtherConsultantHeldByRecentContact(now, now.AddHours(-73)));
    }

    [Fact]
    public void Kill_switch_and_install_state()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-test-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        var kill = new KillSwitch(paths);
        Assert.False(kill.IsArmed());
        var state = HostStateMachine.AfterInstall(kill);
        Assert.True(kill.IsArmed());
        Assert.Equal(HostState.AwaitingVerify, state);
        Assert.Equal(HostState.BlockedForbiddenRooftop, HostStateMachine.AfterVerify("6220"));
        Assert.Equal(HostState.VerifiedIdle, HostStateMachine.AfterVerify("28206"));
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Detects_allowed_rooftops_from_page_text()
    {
        Assert.Equal("28206", EleadsSession.DetectRooftop("Valdosta Nissan store 28206", "https://www.eleadcrm.com/"));
        Assert.Equal("28546", EleadsSession.DetectRooftop("Valdosta Mitsubishi", "https://www.eleadcrm.com/"));
        Assert.Null(EleadsSession.DetectRooftop("pick a store", "https://www.eleadcrm.com/evo2/fresh/login.asp"));
    }

    [Fact]
    public void Host_store_round_trip()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-db-" + Guid.NewGuid().ToString("n"));
        var paths = new HostPaths(root);
        using (var store = new HostStore(paths))
        {
            store.WriteHeartbeat(new Heartbeat("VerifiedIdle", "28206", "t", null, null, true));
            store.Log("new-leads", "inspect", "ok", "28206");
            var beat = store.ReadHeartbeat();
            Assert.Equal("VerifiedIdle", beat.State);
            Assert.Equal("28206", beat.Rooftop);
        }

        Directory.Delete(root, recursive: true);
    }
}
