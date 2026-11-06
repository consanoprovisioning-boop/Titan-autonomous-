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
    [InlineData("I can't come in Saturday at 10am")]
    [InlineData("I cannot make it Saturday at 10am")]
    [InlineData("I won't be there Saturday at 10am")]
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
    public void Ordinary_words_are_not_booked_as_a_calendar_month()
    {
        var decided = Eval("Yes I decided 2 pm works");
        Assert.Equal(AppointmentCommitmentAction.ClarifySlot, decided.Action);
        Assert.False(decided.ShouldBook);
        Assert.Null(decided.NewStatus);
        Assert.Null(decided.SlotEt);

        var mark = Eval("Yes, mark 10 am");
        Assert.Equal(AppointmentCommitmentAction.ClarifySlot, mark.Action);
        Assert.False(mark.ShouldBook);
        Assert.Null(mark.SlotEt);

        // "how about" plus a false month used to count as a day and book the proposed slot.
        var howAbout = Eval("How about I decided 2 pm?", proposed: Sat10);
        Assert.Equal(AppointmentCommitmentAction.NoTrigger, howAbout.Action);
        Assert.False(howAbout.ShouldBook);

        var markSaturday = Eval("Yes, mark Saturday at 10 am");
        Assert.Equal(AppointmentCommitmentAction.ConfirmAndBook, markSaturday.Action);
        Assert.Equal(Sat10, markSaturday.SlotEt);

        Assert.False(Policy.HasCustomerDayAndTime("I decided 2 pm works"));
        Assert.False(Policy.HasCustomerDayAndTime("Yes, mark 10 am"));
        Assert.True(Policy.HasCustomerDayAndTime("December 2 at 2 pm"));
        Assert.True(Policy.HasCustomerDayAndTime("Saturday at 10:30 AM"));
    }

    [Fact]
    public void Real_month_names_and_abbreviations_still_book()
    {
        var december = Eval("See you December 2 at 2 pm");
        Assert.Equal(AppointmentCommitmentAction.ConfirmAndBook, december.Action);
        Assert.Equal(new DateTimeOffset(2026, 12, 2, 14, 0, 0, TimeSpan.FromHours(-4)), december.SlotEt);
        Assert.Equal(LeadStatus.AppointmentSet, december.NewStatus);

        var abbreviated = Eval("See you Dec 2 at 2 pm");
        Assert.Equal(december.SlotEt, abbreviated.SlotEt);

        // March 10, 2026 is already past Sept 25, so the next March 10 is 2027.
        var march = Eval("I'll be there March 10 at 10am");
        Assert.Equal(AppointmentCommitmentAction.ConfirmAndBook, march.Action);
        Assert.Equal(new DateTimeOffset(2027, 3, 10, 10, 0, 0, TimeSpan.FromHours(-4)), march.SlotEt);
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
