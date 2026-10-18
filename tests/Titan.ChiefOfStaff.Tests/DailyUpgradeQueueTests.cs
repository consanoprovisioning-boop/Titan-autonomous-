using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class DailyUpgradeQueueTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 0, 0, TimeSpan.FromHours(-4)); // Monday
    private static readonly DateTimeOffset Choice1 = new(2026, 10, 21, 10, 0, 0, TimeSpan.FromHours(-4)); // Wed
    private static readonly DateTimeOffset Choice2 = new(2026, 10, 24, 11, 0, 0, TimeSpan.FromHours(-4)); // Sat

    private static CustomerProfileFacts EligibleProfile() => new(
        RooftopId: "28206",
        CurrentVehicleYear: 2022,
        CurrentVehicleMake: "Nissan",
        CurrentVehicleModel: "Rogue",
        CurrentVehicleTrim: "SV",
        VehicleOfInterest: "2026 Nissan Rogue",
        RecentInquiry: true,
        ServiceContext: null,
        PurchasedAt: null,
        EmailPermitted: true,
        HistoryText: "Customer emailed last week about an upgrade. Email permission on file.",
        FirstName: null);

    [Fact]
    public void Strict_order_is_daily_emails_then_database_study_then_tailored_upgrades()
    {
        Assert.Equal(
            [DailyUpgradePhase.DailyOrganizerScheduledEmails, DailyUpgradePhase.CustomerDatabaseStudy, DailyUpgradePhase.TailoredUpgradeEmails],
            DailyUpgradeQueue.OrderedPhases);
    }

    [Fact]
    public void Database_study_is_locked_until_daily_emails_complete_on_both_rooftops()
    {
        var none = DailyUpgradeProgress.Empty;
        Assert.Equal(DailyUpgradePhase.DailyOrganizerScheduledEmails, DailyUpgradeQueue.NextPhase(none));
        Assert.False(DailyUpgradeQueue.CanStart(DailyUpgradePhase.CustomerDatabaseStudy, none));
        Assert.Contains("28546", DailyUpgradeQueue.GateReason(DailyUpgradePhase.CustomerDatabaseStudy, new DailyUpgradeProgress(["28206"], [])) ?? "");

        var dailyDoneOneRooftopOnly = new DailyUpgradeProgress(["28206"], []);
        Assert.Equal(DailyUpgradePhase.DailyOrganizerScheduledEmails, DailyUpgradeQueue.NextPhase(dailyDoneOneRooftopOnly));

        var dailyDoneBoth = new DailyUpgradeProgress(["28206", "28546"], []);
        Assert.Equal(DailyUpgradePhase.CustomerDatabaseStudy, DailyUpgradeQueue.NextPhase(dailyDoneBoth));
        Assert.True(DailyUpgradeQueue.CanStart(DailyUpgradePhase.CustomerDatabaseStudy, dailyDoneBoth));
    }

    [Fact]
    public void Tailored_upgrades_are_locked_until_database_study_complete_on_both_rooftops()
    {
        var studyOneOnly = new DailyUpgradeProgress(["28206", "28546"], ["28206"]);
        Assert.Equal(DailyUpgradePhase.CustomerDatabaseStudy, DailyUpgradeQueue.NextPhase(studyOneOnly));
        Assert.False(DailyUpgradeQueue.CanStart(DailyUpgradePhase.TailoredUpgradeEmails, studyOneOnly));
        Assert.Contains("28546", DailyUpgradeQueue.GateReason(DailyUpgradePhase.TailoredUpgradeEmails, studyOneOnly) ?? "");

        var studyBoth = new DailyUpgradeProgress(["28206", "28546"], ["28206", "28546"]);
        Assert.Equal(DailyUpgradePhase.TailoredUpgradeEmails, DailyUpgradeQueue.NextPhase(studyBoth));
        Assert.True(DailyUpgradeQueue.CanStart(DailyUpgradePhase.TailoredUpgradeEmails, studyBoth));
        Assert.Null(DailyUpgradeQueue.GateReason(DailyUpgradePhase.TailoredUpgradeEmails, studyBoth));
    }

    [Fact]
    public void Forbidden_rooftop_completion_never_counts_toward_the_gate()
    {
        var tkoOnly = new DailyUpgradeProgress(["6220"], ["6220"]);
        Assert.Equal(DailyUpgradePhase.DailyOrganizerScheduledEmails, DailyUpgradeQueue.NextPhase(tkoOnly));
    }

    [Fact]
    public void Eligible_profile_names_actual_vehicle_specific_angle_and_appraisal_offer()
    {
        var decision = TailoredUpgradeEmail.Evaluate(EligibleProfile(), Now,
            includeComplimentaryAppraisalOffer: true, Choice1, Choice2);

        Assert.Equal(TailoredUpgradeDisposition.Eligible, decision.Disposition);
        Assert.True(decision.MaySend);
        Assert.Contains("2022 Nissan Rogue SV", decision.Body);
        Assert.Contains("2026 Nissan Rogue", decision.Body);
        Assert.Contains("complimentary appraisal of your current 2022 Nissan Rogue SV", decision.Body);
        Assert.DoesNotContain("$", decision.Body);
        Assert.False(Consent.PromisesForbiddenTerms(decision.Body));
    }

    [Fact]
    public void Appraisal_offer_is_omitted_when_workflow_does_not_include_it()
    {
        var decision = TailoredUpgradeEmail.Evaluate(EligibleProfile(), Now,
            includeComplimentaryAppraisalOffer: false, Choice1, Choice2);

        Assert.True(decision.MaySend);
        Assert.DoesNotContain("appraisal", decision.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Thin_profile_without_current_vehicle_is_skipped_not_blasted()
    {
        var thin = EligibleProfile() with { CurrentVehicleModel = null };
        var decision = TailoredUpgradeEmail.Evaluate(thin, Now, true, Choice1, Choice2);

        Assert.Equal(TailoredUpgradeDisposition.SkipThinProfile, decision.Disposition);
        Assert.False(decision.MaySend);
        Assert.Null(decision.Body);
    }

    [Fact]
    public void Thin_profile_without_specific_upgrade_angle_is_skipped()
    {
        // Vehicle known and inside the ownership screen, but nothing in the
        // record gives a specific angle — no generic blast.
        var thin = EligibleProfile() with
        {
            VehicleOfInterest = null,
            RecentInquiry = false,
            ServiceContext = null,
            PurchasedAt = Now.AddDays(-30)
        };
        var decision = TailoredUpgradeEmail.Evaluate(thin, Now, true, Choice1, Choice2);

        Assert.Equal(TailoredUpgradeDisposition.SkipThinProfile, decision.Disposition);
        Assert.Null(decision.Body);
    }

    [Fact]
    public void Service_context_counts_as_a_specific_angle()
    {
        var profile = EligibleProfile() with
        {
            VehicleOfInterest = null,
            RecentInquiry = false,
            ServiceContext = "recent service visit for your Rogue",
            PurchasedAt = Now.AddDays(-30)
        };
        var decision = TailoredUpgradeEmail.Evaluate(profile, Now, true, Choice1, Choice2);

        Assert.True(decision.MaySend);
        Assert.Contains("recent service visit", decision.Body);
    }

    [Fact]
    public void Model_year_alone_never_proves_ownership_duration()
    {
        var profile = EligibleProfile() with { RecentInquiry = false, PurchasedAt = null };
        var decision = TailoredUpgradeEmail.Evaluate(profile, Now, true, Choice1, Choice2);

        Assert.Equal(TailoredUpgradeDisposition.Blocked, decision.Disposition);
        Assert.Contains("Model year never proves ownership duration", string.Join(" ", decision.Reasons));
    }

    [Fact]
    public void Outside_180_day_screen_blocks_unless_recent_inquiry_overrides()
    {
        var old = EligibleProfile() with { RecentInquiry = false, VehicleOfInterest = "2026 Nissan Rogue", PurchasedAt = Now.AddDays(-181) };
        Assert.Equal(TailoredUpgradeDisposition.Blocked,
            TailoredUpgradeEmail.Evaluate(old, Now, true, Choice1, Choice2).Disposition);

        var overridden = old with { RecentInquiry = true };
        Assert.True(TailoredUpgradeEmail.Evaluate(overridden, Now, true, Choice1, Choice2).MaySend);
    }

    [Fact]
    public void Consent_and_rooftop_blocks_win_over_tailoring()
    {
        var optedOut = EligibleProfile() with { HistoryText = "Customer replied STOP" };
        Assert.Equal(TailoredUpgradeDisposition.Blocked,
            TailoredUpgradeEmail.Evaluate(optedOut, Now, true, Choice1, Choice2).Disposition);

        var tko = EligibleProfile() with { RooftopId = "6220" };
        Assert.Equal(TailoredUpgradeDisposition.Blocked,
            TailoredUpgradeEmail.Evaluate(tko, Now, true, Choice1, Choice2).Disposition);

        var noEmail = EligibleProfile() with { EmailPermitted = false };
        Assert.Equal(TailoredUpgradeDisposition.Blocked,
            TailoredUpgradeEmail.Evaluate(noEmail, Now, true, Choice1, Choice2).Disposition);
    }

    [Fact]
    public void Sunday_appointment_choices_are_rejected()
    {
        var sunday = new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.FromHours(-4));
        var decision = TailoredUpgradeEmail.Evaluate(EligibleProfile(), Now, true, sunday, Choice2);

        Assert.Equal(TailoredUpgradeDisposition.Blocked, decision.Disposition);
        Assert.Null(decision.Body);
    }
}
