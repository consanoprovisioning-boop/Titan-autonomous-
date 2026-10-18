using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class AppointmentCommitmentTests
{
    // Friday 2026-09-25 10:00 ET.
    private static readonly DateTimeOffset Fri10 = new(2026, 9, 25, 10, 0, 0, TimeSpan.FromHours(-4));
    private static readonly DateTimeOffset Sat10 = new(2026, 9, 26, 10, 0, 0, TimeSpan.FromHours(-4));

    private static AppointmentCommitmentDecision Eval(
        string text,
        ReplyChannel channel = ReplyChannel.Sms,
        DateTimeOffset? now = null,
        DateTimeOffset? proposed = null,
        LeadStatus status = LeadStatus.Other,
        DateTimeOffset? existing = null,
        string rooftop = "28206")
        => AppointmentCommitment.Evaluate(new AppointmentCommitmentRequest(
            channel, text, now ?? Fri10, rooftop, proposed, status, existing));

    [Fact]
    public void Clear_commitment_with_proposed_slot_books_and_sets_status()
    {
        var d = Eval("Yes, that works", proposed: Sat10);
        Assert.Equal(AppointmentCommitmentAction.ConfirmAndBook, d.Action);
        Assert.True(d.ShouldBook);
        Assert.True(d.ShouldReplyNow);
        Assert.True(d.PreemptsQueue);
        Assert.Equal(Sat10, d.SlotEt);
        Assert.Equal(LeadStatus.AppointmentSet, d.NewStatus);
        Assert.Equal("Appointment Set", d.NewStatusLabel);
        Assert.Contains("Saturday", d.ReplyText);
        Assert.Contains("Valdosta Nissan", d.ReplyText);
        Assert.NotNull(d.CrmNote);
    }

    [Fact]
    public void Commitment_proposing_new_slot_parses_and_books()
    {
        var d = Eval("See you Saturday at 10am");
        Assert.Equal(AppointmentCommitmentAction.ConfirmAndBook, d.Action);
        Assert.Equal(Sat10, d.SlotEt);
        Assert.Equal(LeadStatus.AppointmentSet, d.NewStatus);

        var proposal = Eval("How about Monday at 11am?");
        Assert.Equal(AppointmentCommitmentAction.ConfirmAndBook, proposal.Action);
        Assert.Equal(new DateTimeOffset(2026, 9, 28, 11, 0, 0, TimeSpan.FromHours(-4)), proposal.SlotEt);
    }

    [Fact]
    public void Ambiguous_commitment_asks_for_slot_without_booking()
    {
        var d = Eval("Yes, I'll be there");
        Assert.Equal(AppointmentCommitmentAction.ClarifySlot, d.Action);
        Assert.False(d.ShouldBook);
        Assert.Null(d.NewStatus);
        Assert.True(d.ShouldReplyNow);
        Assert.Contains("day and time", d.ReplyText, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("What time do you close on Saturday?")]
    [InlineData("I'm interested, just looking for now")]
    [InlineData("Maybe Saturday at 10")]
    public void Question_or_soft_interest_does_not_trigger(string text)
    {
        var d = Eval(text, proposed: Sat10);
        Assert.Equal(AppointmentCommitmentAction.NoTrigger, d.Action);
        Assert.False(d.ShouldBook);
        Assert.Null(d.NewStatus);
    }

    [Theory]
    [InlineData("STOP")]
    [InlineData("Please unsubscribe me")]
    [InlineData("Do not text me again")]
    public void Opt_out_never_triggers(string text)
    {
        var d = Eval(text, proposed: Sat10);
        Assert.Equal(AppointmentCommitmentAction.NoTrigger, d.Action);
        Assert.False(d.ShouldBook);
    }

    [Fact]
    public void Outside_window_slot_proposes_nearest_valid_without_booking()
    {
        // Saturday 7:00 PM is past the Sat 17:30 close -> next valid is Monday 09:00.
        var d = Eval("Yes, Saturday at 7pm");
        Assert.Equal(AppointmentCommitmentAction.ProposeNearestValidSlot, d.Action);
        Assert.False(d.ShouldBook);
        Assert.Null(d.NewStatus);
        Assert.Equal(new DateTimeOffset(2026, 9, 28, 9, 0, 0, TimeSpan.FromHours(-4)), d.ProposedNearestSlotEt);

        var sunday = Eval("I'll come Sunday at 10am", now: Fri10);
        Assert.Equal(AppointmentCommitmentAction.ProposeNearestValidSlot, sunday.Action);
        Assert.Equal(DayOfWeek.Monday, sunday.ProposedNearestSlotEt!.Value.DayOfWeek);
    }

    [Fact]
    public void Duplicate_commitment_is_idempotent()
    {
        var d = Eval("Yes, Saturday at 10am", status: LeadStatus.AppointmentSet, existing: Sat10);
        Assert.Equal(AppointmentCommitmentAction.AlreadyBooked, d.Action);
        Assert.False(d.ShouldBook);
        Assert.False(d.ShouldReplyNow);
        Assert.Null(d.ReplyText);
    }

    [Fact]
    public void Sms_outside_window_queues_reply_but_still_decides_booking()
    {
        var satNight = new DateTimeOffset(2026, 9, 26, 22, 0, 0, TimeSpan.FromHours(-4));
        var monday10 = new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.FromHours(-4));
        var d = Eval("Yes, Monday at 10am works", now: satNight, proposed: null);
        Assert.Equal(AppointmentCommitmentAction.ConfirmAndBook, d.Action);
        Assert.True(d.ShouldBook);
        Assert.False(d.ShouldReplyNow);
        Assert.Equal(new DateTimeOffset(2026, 9, 27, 8, 0, 0, TimeSpan.FromHours(-4)), d.ReplyAtEt);
        Assert.Equal(monday10, d.SlotEt);

        var email = Eval("Yes, Monday at 10am works", channel: ReplyChannel.Email, now: satNight);
        Assert.True(email.ShouldReplyNow);
    }
}
