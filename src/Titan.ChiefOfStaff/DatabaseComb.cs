namespace Titan.ChiefOfStaff;

/// <summary>
/// Concrete "comb the database" job for P2, then P3 tailored upgrades.
/// Runs only after that rooftop's P1 overdue / Daily Organizer work is clear.
/// List-view qualification is forbidden. Cloud Linux never sends.
/// </summary>
public enum DatabaseCombAction
{
    Blocked,
    StudyProfile,
    SkipThinOrHeld,
    ComposeTailoredUpgrade
}

public sealed record DatabaseCombRow(
    string CustomerKey,
    string AssignedTo,
    string RooftopId,
    string HistoryText,
    bool EmailPermitted,
    DateTimeOffset? LastContact = null,
    bool TitanTouched = false,
    DateTimeOffset? OpenedAt = null,
    DateTimeOffset? PurchasedAt = null,
    bool RecentInquiry = false,
    int? ModelYear = null,
    CustomerProfileFacts? Profile = null);

public sealed record DatabaseCombDecision(
    DatabaseCombAction Action,
    DailyUpgradePhase Phase,
    string Reason,
    TailoredUpgradeDecision? Upgrade)
{
    public bool MaySendUpgrade =>
        Action == DatabaseCombAction.ComposeTailoredUpgrade
        && Upgrade?.MaySend == true;
}

public static class DatabaseComb
{
    public static DatabaseCombDecision Evaluate(
        DatabaseCombRow row,
        DailyUpgradeProgress progress,
        DateTimeOffset nowEt,
        DateTimeOffset firstChoiceEt,
        DateTimeOffset secondChoiceEt,
        bool includeComplimentaryAppraisalOffer = true,
        bool customerRequestedTuesday = false)
    {
        if (!Policy.IsAllowedRooftop(row.RooftopId) || Policy.IsForbiddenRooftop(row.HistoryText))
        {
            return Blocked(DailyUpgradePhase.CustomerDatabaseStudy, "Rooftop is not 28206 or 28546 (never 6220).");
        }

        var p2Gate = DailyUpgradeQueue.GateReasonOnRooftop(
            DailyUpgradePhase.CustomerDatabaseStudy, row.RooftopId, progress);
        if (p2Gate is not null)
        {
            return Blocked(DailyUpgradePhase.DailyOrganizerScheduledEmails, p2Gate);
        }

        var hold = WorkOrder.HoldReason(
            new OrganizerRow(
                row.CustomerKey,
                row.AssignedTo,
                WorkQueue.Database,
                row.LastContact,
                row.OpenedAt,
                row.TitanTouched,
                row.HistoryText,
                row.PurchasedAt,
                row.RecentInquiry,
                row.ModelYear),
            nowEt);
        if (hold is not null)
        {
            return new DatabaseCombDecision(
                DatabaseCombAction.SkipThinOrHeld,
                DailyUpgradePhase.CustomerDatabaseStudy,
                hold,
                null);
        }

        var p3Gate = DailyUpgradeQueue.GateReasonOnRooftop(
            DailyUpgradePhase.TailoredUpgradeEmails, row.RooftopId, progress);
        if (p3Gate is not null)
        {
            return new DatabaseCombDecision(
                DatabaseCombAction.StudyProfile,
                DailyUpgradePhase.CustomerDatabaseStudy,
                "P2 comb: open the full profile, every history tab, permissions, ownership, and exact timestamps. Do not send an upgrade email until this rooftop's database study is marked complete. No list-view send.",
                null);
        }

        if (row.Profile is null)
        {
            return new DatabaseCombDecision(
                DatabaseCombAction.SkipThinOrHeld,
                DailyUpgradePhase.TailoredUpgradeEmails,
                "P3: profile facts were not loaded. Manual verification required. Do not send a generic blast.",
                null);
        }

        var upgrade = TailoredUpgradeEmail.Evaluate(
            row.Profile,
            nowEt,
            includeComplimentaryAppraisalOffer,
            firstChoiceEt,
            secondChoiceEt,
            customerRequestedTuesday);

        if (!upgrade.MaySend)
        {
            return new DatabaseCombDecision(
                DatabaseCombAction.SkipThinOrHeld,
                DailyUpgradePhase.TailoredUpgradeEmails,
                string.Join(" ", upgrade.Reasons),
                upgrade);
        }

        return new DatabaseCombDecision(
            DatabaseCombAction.ComposeTailoredUpgrade,
            DailyUpgradePhase.TailoredUpgradeEmails,
            "P3: tailored upgrade email from proven profile facts only. Send under V1.1 after Verify. CRM history under Bordine, Glenn is proof.",
            upgrade);
    }

    private static DatabaseCombDecision Blocked(DailyUpgradePhase phase, string reason)
        => new(DatabaseCombAction.Blocked, phase, reason, null);
}
