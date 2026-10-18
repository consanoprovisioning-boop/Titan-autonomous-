using System.Globalization;
using System.Text.RegularExpressions;

namespace Titan.ChiefOfStaff;

public enum ReplyChannel
{
    Sms,
    Email
}

public enum LeadStatus
{
    Other,
    AppointmentSet
}

public enum AppointmentCommitmentAction
{
    NoTrigger,
    ClarifySlot,
    ConfirmAndBook,
    ProposeNearestValidSlot,
    AlreadyBooked
}

public sealed record AppointmentCommitmentRequest(
    ReplyChannel Channel,
    string? Text,
    DateTimeOffset NowEt,
    string? RooftopId,
    DateTimeOffset? ProposedSlotEt = null,
    LeadStatus CurrentStatus = LeadStatus.Other,
    DateTimeOffset? ExistingAppointmentSlotEt = null);

public sealed record AppointmentCommitmentDecision(
    AppointmentCommitmentAction Action,
    bool IsCommitment,
    bool ShouldBook,
    bool ShouldReplyNow,
    DateTimeOffset? ReplyAtEt,
    DateTimeOffset? SlotEt,
    DateTimeOffset? ProposedNearestSlotEt,
    LeadStatus? NewStatus,
    string? NewStatusLabel,
    string? ReplyText,
    string Reason)
{
    public bool PreemptsQueue => IsCommitment;
    public string? CrmNote => Action switch
    {
        AppointmentCommitmentAction.ConfirmAndBook when SlotEt is not null =>
            $"Appointment commitment fast path: booked {SlotEt.Value:yyyy-MM-dd HH:mm} ET, status {Policy.AppointmentSetStatusLabel}, reply on same channel. Proof requires CRM history under {Policy.Identity}.",
        AppointmentCommitmentAction.ProposeNearestValidSlot when ProposedNearestSlotEt is not null =>
            $"Appointment commitment fast path: committed time outside appointment windows; proposed nearest valid {ProposedNearestSlotEt.Value:yyyy-MM-dd HH:mm} ET. No booking, no Appointment Set.",
        AppointmentCommitmentAction.ClarifySlot =>
            "Appointment commitment fast path: commitment without a concrete day/time; asked for specific day+time. No booking, no Appointment Set.",
        AppointmentCommitmentAction.AlreadyBooked =>
            "Appointment commitment fast path: duplicate commitment; existing appointment kept, no double-book, no duplicate reply.",
        _ => null
    };
}

/// <summary>
/// Glenn directive (2026-10-18): when a customer responds with an appointment
/// commitment, reply immediately, schedule the committed day/time, and change
/// the customer status to Appointment Set. No per-draft approval (V1.1).
/// This type only decides; Eleads execution happens on the Glenn-verified
/// Alienware-18 host session. This cloud agent never books or sends.
/// </summary>
public static class AppointmentCommitment
{
    public static AppointmentCommitmentDecision Evaluate(AppointmentCommitmentRequest request)
    {
        var text = request.Text?.Trim() ?? "";

        if (IsOptOut(text))
        {
            return NoTrigger("Opt-out / no-contact reply. Never triggers scheduling.");
        }

        if (!IsCommitment(text, out var reason))
        {
            return NoTrigger(reason);
        }

        // Commitment from here. SMS gating follows Policy.SmsOpen (live
        // 08:00-21:00 ET). Outside the window an SMS reply is queued to the
        // next Policy.NextSmsOpen, but a valid booking/status change is still
        // decided now so the verified host session can execute it as soon as
        // detected; the SMS itself must not be sent outside the window.
        var (replyNow, replyAt) = ReplyTiming(request);

        var slot = TryParseSlot(text, request.NowEt) ?? request.ProposedSlotEt;

        if (slot is null)
        {
            return new AppointmentCommitmentDecision(
                AppointmentCommitmentAction.ClarifySlot,
                IsCommitment: true,
                ShouldBook: false,
                ShouldReplyNow: replyNow,
                ReplyAtEt: replyAt,
                SlotEt: null,
                ProposedNearestSlotEt: null,
                NewStatus: null,
                NewStatusLabel: null,
                ReplyText: "Thanks — I'd be glad to set that up. What day and time works best for you?",
                Reason: "Commitment without a concrete day/time and none proposed. Ask for the specific day+time; do not book or set Appointment Set.");
        }

        if (request.CurrentStatus == LeadStatus.AppointmentSet
            || (request.ExistingAppointmentSlotEt is not null && request.ExistingAppointmentSlotEt.Value == slot.Value))
        {
            return new AppointmentCommitmentDecision(
                AppointmentCommitmentAction.AlreadyBooked,
                IsCommitment: true,
                ShouldBook: false,
                ShouldReplyNow: false,
                ReplyAtEt: null,
                SlotEt: slot,
                ProposedNearestSlotEt: null,
                NewStatus: null,
                NewStatusLabel: null,
                ReplyText: null,
                Reason: "Duplicate commitment for an already-booked slot. Idempotent: no double-book, no duplicate text.");
        }

        var customerRequestedTuesday = slot.Value.DayOfWeek == DayOfWeek.Tuesday;
        if (!Policy.AppointmentWindowOpen(slot.Value, customerRequestedTuesday))
        {
            var nearest = NearestValidSlot(slot.Value, customerRequestedTuesday);
            return new AppointmentCommitmentDecision(
                AppointmentCommitmentAction.ProposeNearestValidSlot,
                IsCommitment: true,
                ShouldBook: false,
                ShouldReplyNow: replyNow,
                ReplyAtEt: replyAt,
                SlotEt: slot,
                ProposedNearestSlotEt: nearest,
                NewStatus: null,
                NewStatusLabel: null,
                ReplyText: nearest is null
                    ? "Thanks — that time is outside our appointment hours. What other day and time works for you?"
                    : $"Thanks — that time is outside our appointment hours. The nearest I can offer is {FormatSlot(nearest.Value)}. Does that work?",
                Reason: "Committed slot is outside appointment windows (or Sunday/closed). Propose nearest valid slot; do not book the invalid time.");
        }

        var rooftop = Policy.RooftopDisplayName(request.RooftopId);
        return new AppointmentCommitmentDecision(
            AppointmentCommitmentAction.ConfirmAndBook,
            IsCommitment: true,
            ShouldBook: true,
            ShouldReplyNow: replyNow,
            ReplyAtEt: replyAt,
            SlotEt: slot,
            ProposedNearestSlotEt: null,
            NewStatus: LeadStatus.AppointmentSet,
            NewStatusLabel: Policy.AppointmentSetStatusLabel,
            ReplyText: $"You're set for {FormatSlot(slot.Value)} at {rooftop}. Please ask for {Policy.Identity} when you arrive. Reply here if anything changes.",
            Reason: "Clear appointment commitment with a valid day/time. Reply immediately on the same channel, book in Eleads, set Appointment Set — no approval pause (V1.1). Preempts normal queue order.");
    }

    public static string FormatSlot(DateTimeOffset slot)
        => slot.ToString("dddd, MMMM d 'at' h:mm tt", CultureInfo.InvariantCulture) + " ET";

    private static (bool Now, DateTimeOffset At) ReplyTiming(AppointmentCommitmentRequest request)
    {
        if (request.Channel == ReplyChannel.Email)
        {
            return (true, request.NowEt);
        }

        return Policy.SmsOpen(request.NowEt)
            ? (true, request.NowEt)
            : (false, Policy.NextSmsOpen(request.NowEt));
    }

    private static AppointmentCommitmentDecision NoTrigger(string reason) => new(
        AppointmentCommitmentAction.NoTrigger,
        IsCommitment: false,
        ShouldBook: false,
        ShouldReplyNow: false,
        ReplyAtEt: null,
        SlotEt: null,
        ProposedNearestSlotEt: null,
        NewStatus: null,
        NewStatusLabel: null,
        ReplyText: null,
        Reason: reason);

    private static bool IsOptOut(string text)
        => Regex.IsMatch(text, @"\bstop\b|\bunsubscribe\b|opt[\s-]?out|do not (text|contact|call)|no further contact|leave me alone", RegexOptions.IgnoreCase);

    private static bool IsCommitment(string text, out string reason)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            reason = "Empty reply.";
            return false;
        }

        if (Regex.IsMatch(text, @"\bmaybe\b|\bperhaps\b|not sure|let me think|i'?ll let you know|thinking about it|just looking|just browsing", RegexOptions.IgnoreCase))
        {
            reason = "Soft interest / tentative reply (e.g. maybe without a firm commitment). Follow-up only.";
            return false;
        }

        var hasSlot = Policy.HasCustomerDayAndTime(text);

        // A proposal of a specific slot counts as commitment even phrased as a question.
        if (Regex.IsMatch(text, @"\b(how about|what about|can we do|could we do|can i come|can i do|let'?s do)\b", RegexOptions.IgnoreCase) && hasSlot)
        {
            reason = "Customer proposes a specific day/time.";
            return true;
        }

        var commitment = Regex.IsMatch(text,
            @"\b(yes|yeah|yep|yup|ok|okay|sounds good|that works|works for me|perfect|confirmed?|i'?ll be there|i will be there|see you|book it|schedule it|i can (come|make it)|i'?ll come|come in|be there|count me in|sure)\b",
            RegexOptions.IgnoreCase);

        if (commitment)
        {
            reason = "Customer commitment language detected.";
            return true;
        }

        if (text.Contains('?'))
        {
            reason = "Question without commitment language. Answer normally; no scheduling trigger.";
            return false;
        }

        reason = "No appointment commitment language. Soft interest does not trigger scheduling.";
        return false;
    }

    public static DateTimeOffset? TryParseSlot(string text, DateTimeOffset nowEt)
    {
        var time = TryParseClockTime(text);
        if (time is null)
        {
            return null;
        }

        DateTime date;
        var lower = text.ToLowerInvariant();

        var monthDay = Regex.Match(lower, @"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\s+(\d{1,2})(?:st|nd|rd|th)?\b");
        var numeric = Regex.Match(lower, @"\b(\d{1,2})/(\d{1,2})(?:/(\d{2,4}))?\b");

        if (monthDay.Success)
        {
            var month = MonthNumber(monthDay.Groups[1].Value);
            var day = int.Parse(monthDay.Groups[2].Value, CultureInfo.InvariantCulture);
            date = ResolveDated(nowEt.Date, month, day);
        }
        else if (numeric.Success)
        {
            var month = int.Parse(numeric.Groups[1].Value, CultureInfo.InvariantCulture);
            var day = int.Parse(numeric.Groups[2].Value, CultureInfo.InvariantCulture);
            date = ResolveDated(nowEt.Date, month, day);
        }
        else if (lower.Contains("tomorrow"))
        {
            date = nowEt.Date.AddDays(1);
        }
        else if (lower.Contains("today") || lower.Contains("tonight"))
        {
            date = nowEt.Date;
        }
        else
        {
            var weekday = WeekdayMatch(lower);
            if (weekday is null)
            {
                return null;
            }

            var daysAhead = ((int)weekday.Value - (int)nowEt.DayOfWeek + 7) % 7;
            date = nowEt.Date.AddDays(daysAhead);
            // Same weekday but the clock time has already passed -> next week.
            if (daysAhead == 0 && date.Add(time.Value) <= nowEt.DateTime)
            {
                date = date.AddDays(7);
            }
        }

        return new DateTimeOffset(date.Add(time.Value), nowEt.Offset);
    }

    private static DateTime ResolveDated(DateTime today, int month, int day)
    {
        var candidate = new DateTime(today.Year, month, day);
        return candidate < today ? candidate.AddYears(1) : candidate;
    }

    private static int MonthNumber(string prefix) => prefix switch
    {
        "jan" => 1, "feb" => 2, "mar" => 3, "apr" => 4, "may" => 5, "jun" => 6,
        "jul" => 7, "aug" => 8, "sep" or "sept" => 9, "oct" => 10, "nov" => 11, "dec" => 12,
        _ => 1
    };

    private static DayOfWeek? WeekdayMatch(string lower)
    {
        (string, DayOfWeek)[] days =
        [
            ("monday", DayOfWeek.Monday), ("mon", DayOfWeek.Monday),
            ("tuesday", DayOfWeek.Tuesday), ("tues", DayOfWeek.Tuesday), ("tue", DayOfWeek.Tuesday),
            ("wednesday", DayOfWeek.Wednesday), ("wed", DayOfWeek.Wednesday),
            ("thursday", DayOfWeek.Thursday), ("thurs", DayOfWeek.Thursday), ("thur", DayOfWeek.Thursday), ("thu", DayOfWeek.Thursday),
            ("friday", DayOfWeek.Friday), ("fri", DayOfWeek.Friday),
            ("saturday", DayOfWeek.Saturday), ("sat", DayOfWeek.Saturday),
            ("sunday", DayOfWeek.Sunday), ("sun", DayOfWeek.Sunday)
        ];
        foreach (var (name, day) in days)
        {
            if (Regex.IsMatch(lower, $@"\b{name}\b"))
            {
                return day;
            }
        }

        return null;
    }

    private static TimeSpan? TryParseClockTime(string text)
    {
        var m = Regex.Match(text, @"\b(1[0-2]|0?[1-9])(?::([0-5]\d))?\s*(a\.?m\.?|p\.?m\.?)\b", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var hour = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) % 12;
            if (m.Groups[3].Value.StartsWith("p", StringComparison.OrdinalIgnoreCase))
            {
                hour += 12;
            }

            var minute = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
            return new TimeSpan(hour, minute, 0);
        }

        m = Regex.Match(text, @"\b([01]?\d|2[0-3]):([0-5]\d)\b");
        if (m.Success)
        {
            return new TimeSpan(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), 0);
        }

        m = Regex.Match(text, @"\bat\s+(1[0-2]|0?[1-9])\b", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            // Bare "at 10" with no am/pm: appointment windows are daytime; 7-11
            // reads as AM, 12 as noon, 1-6 as PM inside the windows.
            var hour = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            if (hour >= 1 && hour <= 6)
            {
                hour += 12;
            }

            return new TimeSpan(hour, 0, 0);
        }

        return null;
    }

    public static DateTimeOffset? NearestValidSlot(DateTimeOffset slot, bool customerRequestedTuesday)
    {
        for (var d = 0; d < 14; d++)
        {
            var day = slot.Date.AddDays(d);
            var tod = d == 0 ? slot.TimeOfDay : TimeSpan.FromHours(9);
            if (tod < TimeSpan.FromHours(9))
            {
                tod = TimeSpan.FromHours(9);
            }

            var candidate = new DateTimeOffset(day.Add(tod), slot.Offset);
            if (Policy.AppointmentWindowOpen(candidate, customerRequestedTuesday && candidate.DayOfWeek == DayOfWeek.Tuesday))
            {
                return candidate;
            }
        }

        return null;
    }
}
