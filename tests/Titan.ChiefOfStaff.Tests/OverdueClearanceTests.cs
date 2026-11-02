using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class OverdueClearanceTests
{
    private static readonly DateTimeOffset Monday10 = new(2026, 11, 2, 10, 0, 0, TimeSpan.FromHours(-5));

    private static OverdueTask Task(OverdueTaskKind kind, string rooftop = "28206", bool email = true, bool sms = true, string history = "Open overdue task. Email permission on file.")
        => new("Pat Lee", "Bordine, Glenn", rooftop, kind, history, email, sms, RecentInquiry: true);

    [Fact]
    public void Overdue_email_is_the_high_velocity_complete_action()
    {
        var d = OverdueClearance.Evaluate(Task(OverdueTaskKind.Email), Monday10);
        Assert.Equal(OverdueClearanceAction.CompleteOverdueEmail, d.Action);
        Assert.True(d.CountsTowardHourFloor);
    }

    [Fact]
    public void Overdue_call_reschedules_and_never_marks_complete()
    {
        var d = OverdueClearance.Evaluate(Task(OverdueTaskKind.Call), Monday10);
        Assert.Equal(OverdueClearanceAction.RescheduleOverdueCall, d.Action);
        Assert.NotNull(d.RescheduleToEt);
        Assert.Contains("Never mark a call completed", d.Reason);
    }

    [Fact]
    public void Sms_respects_the_8_to_21_window()
    {
        var open = OverdueClearance.Evaluate(Task(OverdueTaskKind.Sms), Monday10);
        Assert.Equal(OverdueClearanceAction.SendEligibleSmsInWindow, open.Action);

        var closed = OverdueClearance.Evaluate(Task(OverdueTaskKind.Sms), new DateTimeOffset(2026, 11, 2, 21, 0, 0, TimeSpan.FromHours(-5)));
        Assert.Equal(OverdueClearanceAction.Skip, closed.Action);
        Assert.Contains("SMS window closed", closed.Reason);
    }

    [Fact]
    public void Tko_and_consent_blocks_never_count()
    {
        Assert.Equal(OverdueClearanceAction.Skip,
            OverdueClearance.Evaluate(Task(OverdueTaskKind.Email, rooftop: "6220"), Monday10).Action);
        Assert.Equal(OverdueClearanceAction.Skip,
            OverdueClearance.Evaluate(Task(OverdueTaskKind.Email, history: "Customer replied STOP"), Monday10).Action);
        Assert.False(OverdueClearance.Evaluate(Task(OverdueTaskKind.Email, email: false), Monday10).CountsTowardHourFloor);
    }
}
