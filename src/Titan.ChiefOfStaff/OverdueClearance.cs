namespace Titan.ChiefOfStaff;

/// <summary>
/// High-velocity P1 job: complete overdue emails and reschedule overdue
/// calls first. This is the legal path that moves the 60 actions/hour
/// needle. Emails need no approval after a Glenn-verified session.
/// This type only decides. Cloud Linux never sends.
/// </summary>
public enum OverdueTaskKind
{
    Email,
    Call,
    Sms,
    Other
}

public enum OverdueClearanceAction
{
    Skip,
    CompleteOverdueEmail,
    RescheduleOverdueCall,
    SendEligibleSmsInWindow,
    InspectOnly
}

public sealed record OverdueTask(
    string CustomerKey,
    string AssignedTo,
    string RooftopId,
    OverdueTaskKind Kind,
    string HistoryText,
    bool EmailPermitted,
    bool SmsPermitted,
    DateTimeOffset? LastContact = null,
    bool TitanTouched = false,
    DateTimeOffset? OpenedAt = null,
    DateTimeOffset? PurchasedAt = null,
    bool RecentInquiry = false,
    int? ModelYear = null);

public sealed record OverdueClearanceDecision(
    OverdueClearanceAction Action,
    string Reason,
    DateTimeOffset? RescheduleToEt)
{
    public bool CountsTowardHourFloor =>
        Action is OverdueClearanceAction.CompleteOverdueEmail
            or OverdueClearanceAction.RescheduleOverdueCall
            or OverdueClearanceAction.SendEligibleSmsInWindow;
}

public static class OverdueClearance
{
    public static OverdueClearanceDecision Evaluate(OverdueTask task, DateTimeOffset nowEt)
    {
        if (!Policy.IsAllowedRooftop(task.RooftopId) || Policy.IsForbiddenRooftop(task.HistoryText))
        {
            return Skip("Rooftop is not 28206 or 28546 (never 6220).");
        }

        var hold = WorkOrder.HoldReason(
            new OrganizerRow(
                task.CustomerKey,
                task.AssignedTo,
                WorkQueue.Overdue,
                task.LastContact,
                task.OpenedAt,
                task.TitanTouched,
                task.HistoryText,
                task.PurchasedAt,
                task.RecentInquiry,
                task.ModelYear),
            nowEt);
        if (hold is not null)
        {
            return Skip(hold);
        }

        return task.Kind switch
        {
            OverdueTaskKind.Email => EmailDecision(task),
            OverdueTaskKind.Call => CallDecision(task, nowEt),
            OverdueTaskKind.Sms => SmsDecision(task, nowEt),
            _ => new OverdueClearanceDecision(
                OverdueClearanceAction.InspectOnly,
                "Not an overdue email or call. Stay on P1 organizer; do not jump to database comb.",
                null)
        };
    }

    public static DateTimeOffset NextFollowUpSlot(DateTimeOffset nowEt)
    {
        var candidate = nowEt.AddDays(1);
        if (candidate.DayOfWeek == DayOfWeek.Sunday)
        {
            candidate = candidate.AddDays(1);
        }

        var start = TimeSpan.FromHours(9);
        var end = candidate.DayOfWeek == DayOfWeek.Saturday
            ? TimeSpan.FromHours(18)
            : TimeSpan.FromHours(19);
        var tod = candidate.TimeOfDay < start ? start
            : candidate.TimeOfDay > end ? end
            : candidate.TimeOfDay;
        return new DateTimeOffset(candidate.Date.Add(tod), nowEt.Offset);
    }

    private static OverdueClearanceDecision EmailDecision(OverdueTask task)
    {
        if (!task.EmailPermitted)
        {
            return Skip("Email channel is not permitted (opt-out, hard bounce, or no valid permitted email).");
        }

        return new OverdueClearanceDecision(
            OverdueClearanceAction.CompleteOverdueEmail,
            "P1 high-velocity: complete the overdue email under V1.1 after Verify. CRM history under Bordine, Glenn is the only send count.",
            null);
    }

    private static OverdueClearanceDecision CallDecision(OverdueTask task, DateTimeOffset nowEt)
    {
        return new OverdueClearanceDecision(
            OverdueClearanceAction.RescheduleOverdueCall,
            "P1 high-velocity: reschedule the overdue call. Never mark a call completed if no call occurred. Autonomous calls stay closed without telephony.",
            NextFollowUpSlot(nowEt));
    }

    private static OverdueClearanceDecision SmsDecision(OverdueTask task, DateTimeOffset nowEt)
    {
        if (!task.SmsPermitted)
        {
            return Skip("SMS channel is not permitted (DNT, Stop, or no valid SMS phone).");
        }

        if (!Policy.SmsOpen(nowEt))
        {
            return Skip($"SMS window closed until {Policy.NextSmsOpen(nowEt):HH:mm} ET. Emails may still run after Verify.");
        }

        return new OverdueClearanceDecision(
            OverdueClearanceAction.SendEligibleSmsInWindow,
            "P1: eligible SMS inside 08:00-21:00 ET after Verify. CRM history is proof.",
            null);
    }

    private static OverdueClearanceDecision Skip(string reason)
        => new(OverdueClearanceAction.Skip, reason, null);
}
