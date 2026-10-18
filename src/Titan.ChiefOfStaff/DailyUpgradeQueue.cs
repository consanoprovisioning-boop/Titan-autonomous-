namespace Titan.ChiefOfStaff;

/// <summary>
/// Glenn directive 2026-10-18 06:10/06:12 UTC — strict order for the
/// Daily Organizer / database / upgrade work, both rooftops:
/// (P1) complete the scheduled emails in the Daily Organizer first,
/// (P2) only then study the customer database, (P3) only then send
/// tailored upgrade emails profile-by-profile (never a generic blast).
/// The appointment-commitment fast path (AppointmentCommitment) preempts
/// this queue whenever a commitment reply lands for a lead.
/// Execution happens only on the Glenn-verified Alienware-18 host session
/// (CHECKIN.cmd + Verify, per rooftop); this type only decides. A send
/// counts only with CRM history proof under Bordine, Glenn.
/// </summary>
public enum DailyUpgradePhase
{
    DailyOrganizerScheduledEmails = 1,
    CustomerDatabaseStudy = 2,
    TailoredUpgradeEmails = 3
}

public sealed record DailyUpgradeProgress(
    IReadOnlyCollection<string> DailyOrganizerScheduledEmailsCompleteRooftops,
    IReadOnlyCollection<string> CustomerDatabaseStudyCompleteRooftops)
{
    public static DailyUpgradeProgress Empty { get; } = new([], []);
}

public static class DailyUpgradeQueue
{
    public static IReadOnlyList<DailyUpgradePhase> OrderedPhases { get; } =
    [
        DailyUpgradePhase.DailyOrganizerScheduledEmails,
        DailyUpgradePhase.CustomerDatabaseStudy,
        DailyUpgradePhase.TailoredUpgradeEmails
    ];

    /// <summary>First phase not yet complete for BOTH allowed rooftops.</summary>
    public static DailyUpgradePhase NextPhase(DailyUpgradeProgress progress)
    {
        if (!IsCompleteForBothRooftops(progress.DailyOrganizerScheduledEmailsCompleteRooftops))
        {
            return DailyUpgradePhase.DailyOrganizerScheduledEmails;
        }

        if (!IsCompleteForBothRooftops(progress.CustomerDatabaseStudyCompleteRooftops))
        {
            return DailyUpgradePhase.CustomerDatabaseStudy;
        }

        return DailyUpgradePhase.TailoredUpgradeEmails;
    }

    /// <summary>
    /// Strict order: a phase may start only when every earlier phase is
    /// complete for both 28206 and 28546. Database prospecting (P2/P3)
    /// never starts before P1 scheduled emails are complete.
    /// </summary>
    public static bool CanStart(DailyUpgradePhase phase, DailyUpgradeProgress progress)
        => GateReason(phase, progress) is null;

    public static string? GateReason(DailyUpgradePhase phase, DailyUpgradeProgress progress)
    {
        if (phase == DailyUpgradePhase.DailyOrganizerScheduledEmails)
        {
            return null;
        }

        if (!IsCompleteForBothRooftops(progress.DailyOrganizerScheduledEmailsCompleteRooftops))
        {
            return $"P1 first: scheduled emails in the Daily Organizer are not complete for {Missing(progress.DailyOrganizerScheduledEmailsCompleteRooftops)}. Database prospecting stays closed until P1 is complete on both rooftops.";
        }

        if (phase == DailyUpgradePhase.CustomerDatabaseStudy)
        {
            return null;
        }

        if (!IsCompleteForBothRooftops(progress.CustomerDatabaseStudyCompleteRooftops))
        {
            return $"P2 first: customer database study is not complete for {Missing(progress.CustomerDatabaseStudyCompleteRooftops)}. Tailored upgrade emails stay closed until the database has been studied on both rooftops.";
        }

        return null;
    }

    private static bool IsCompleteForBothRooftops(IReadOnlyCollection<string> completedRooftops)
        => Policy.AllowedRooftops.All(r => completedRooftops.Any(c => string.Equals(c.Trim(), r, StringComparison.Ordinal)));

    private static string Missing(IReadOnlyCollection<string> completedRooftops)
        => string.Join(" and ", Policy.AllowedRooftops.Where(r => !completedRooftops.Any(c => string.Equals(c.Trim(), r, StringComparison.Ordinal))));
}

/// <summary>
/// Facts proven in one customer's CRM record — nothing else may be used.
/// Every field here must come from that customer's profile/history tabs;
/// absent facts stay null and are never invented.
/// </summary>
public sealed record CustomerProfileFacts(
    string? RooftopId,
    int? CurrentVehicleYear,
    string? CurrentVehicleMake,
    string? CurrentVehicleModel,
    string? CurrentVehicleTrim,
    string? VehicleOfInterest,
    bool RecentInquiry,
    string? ServiceContext,
    DateTimeOffset? PurchasedAt,
    bool EmailPermitted,
    string? HistoryText,
    string? FirstName = null)
{
    public string? CurrentVehicle => CurrentVehicleYear is not null
        && !string.IsNullOrWhiteSpace(CurrentVehicleMake)
        && !string.IsNullOrWhiteSpace(CurrentVehicleModel)
        ? string.Join(" ", new[] { CurrentVehicleYear.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), CurrentVehicleMake!.Trim(), CurrentVehicleModel!.Trim(), CurrentVehicleTrim?.Trim() }.Where(p => !string.IsNullOrEmpty(p)))
        : null;

    /// <summary>Upgrade angle grounded only in a proven profile fact.</summary>
    public string? SpecificUpgradeAngle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(VehicleOfInterest))
            {
                return $"your record shows interest in the {VehicleOfInterest.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(ServiceContext))
            {
                return $"your {ServiceContext.Trim()}";
            }

            if (RecentInquiry)
            {
                return "your recent inquiry with us";
            }

            return null;
        }
    }
}

public enum TailoredUpgradeDisposition
{
    Eligible,
    SkipThinProfile,
    Blocked
}

public sealed record TailoredUpgradeDecision(
    TailoredUpgradeDisposition Disposition,
    IReadOnlyList<string> Reasons,
    string? Subject,
    string? Body)
{
    public bool MaySend => Disposition == TailoredUpgradeDisposition.Eligible && Body is not null;
}

/// <summary>
/// Per-profile eligibility + composition for P3 tailored upgrade emails.
/// Tailored means: the customer's actual current vehicle is named and a
/// specific upgrade angle from that customer's own record is used.
/// Equity, payment, trade value, APR, and any profile detail not in the
/// record are never fabricated. A thin profile is skipped/deferred —
/// a generic mail-merge blast to everyone is never the fallback.
/// </summary>
public static class TailoredUpgradeEmail
{
    public static TailoredUpgradeDecision Evaluate(
        CustomerProfileFacts profile,
        DateTimeOffset nowEt,
        bool includeComplimentaryAppraisalOffer,
        DateTimeOffset firstChoiceEt,
        DateTimeOffset secondChoiceEt,
        bool customerRequestedTuesday = false)
    {
        if (!Policy.IsAllowedRooftop(profile.RooftopId))
        {
            return Blocked("Rooftop is not 28206 or 28546 (never 6220). No tailored upgrade email.");
        }

        var consent = Consent.BlockReason(profile.HistoryText);
        if (consent is not null)
        {
            return Blocked(consent);
        }

        if (!profile.EmailPermitted)
        {
            return Blocked("Email channel is not permitted for this customer (opt-out, hard bounce, or no valid permitted email).");
        }

        var ownership = Policy.OwnershipBlock(nowEt, profile.PurchasedAt, profile.RecentInquiry, profile.CurrentVehicleYear);
        if (ownership is not null)
        {
            return Blocked(ownership);
        }

        if (!Policy.AppointmentWindowOpen(firstChoiceEt, customerRequestedTuesday)
            || !Policy.AppointmentWindowOpen(secondChoiceEt, customerRequestedTuesday))
        {
            return Blocked("Appointment choices must be inside the appointment windows (Mon/Wed/Thu/Fri 09:00-18:30, Sat 09:00-17:30 ET, never Sunday; Tuesday only if the customer asks).");
        }

        var missing = new List<string>();
        if (profile.CurrentVehicle is null)
        {
            missing.Add("proven current vehicle year/make/model in the CRM record");
        }

        if (profile.SpecificUpgradeAngle is null)
        {
            missing.Add("a specific upgrade angle proven in the record (vehicle of interest, service context, or recent inquiry)");
        }

        if (missing.Count > 0)
        {
            return new TailoredUpgradeDecision(
                TailoredUpgradeDisposition.SkipThinProfile,
                [$"Thin profile — skip/defer, do not send generic copy. Missing: {string.Join("; ", missing)}."],
                null,
                null);
        }

        var vehicle = profile.CurrentVehicle!;
        var angle = profile.SpecificUpgradeAngle!;
        var rooftop = Policy.RooftopDisplayName(profile.RooftopId);
        var greeting = string.IsNullOrWhiteSpace(profile.FirstName) ? "Hello," : $"Hi {profile.FirstName.Trim()},";
        var appraisal = includeComplimentaryAppraisalOffer
            ? $" While you're here, we can include a complimentary appraisal of your current {vehicle} — we'll review it with you in person."
            : "";
        var body =
            $"{greeting} I'm following up on your {vehicle}. Based on {angle}, it's worth a look at how an upgrade could fit you next.{appraisal} " +
            $"Would {AppointmentCommitment.FormatSlot(firstChoiceEt)} or {AppointmentCommitment.FormatSlot(secondChoiceEt)} at {rooftop} work for you? " +
            $"Please ask for {Policy.Identity} when you arrive, and just reply here with the time that suits you.";

        if (Consent.PromisesForbiddenTerms(body))
        {
            return Blocked("Composed copy failed the no-promised-terms gate. No send.");
        }

        return new TailoredUpgradeDecision(
            TailoredUpgradeDisposition.Eligible,
            ["Eligible: current vehicle and specific upgrade angle are both proven in this customer's record. Send profile-by-profile under V1.1, CRM history is proof."],
            $"Your {vehicle} — a quick upgrade look at {rooftop}",
            body);
    }

    private static TailoredUpgradeDecision Blocked(string reason)
        => new(TailoredUpgradeDisposition.Blocked, [reason], null, null);
}
