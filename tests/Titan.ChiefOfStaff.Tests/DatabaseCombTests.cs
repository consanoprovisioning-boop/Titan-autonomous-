using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class DatabaseCombTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 10, 0, 0, TimeSpan.FromHours(-5));
    private static readonly DateTimeOffset Choice1 = new(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(-5));
    private static readonly DateTimeOffset Choice2 = new(2026, 11, 6, 11, 0, 0, TimeSpan.FromHours(-5));

    private static CustomerProfileFacts Profile() => new(
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
        FirstName: "Pat");

    private static DatabaseCombRow Row(CustomerProfileFacts? profile, string rooftop = "28206")
        => new("Pat Lee", "Bordine, Glenn", rooftop, "Open database row. Email permission on file.", true, RecentInquiry: true, Profile: profile);

    [Fact]
    public void Comb_is_blocked_until_that_rooftop_p1_is_clear()
    {
        var blocked = DatabaseComb.Evaluate(Row(Profile()), DailyUpgradeProgress.Empty, Now, Choice1, Choice2);
        Assert.Equal(DatabaseCombAction.Blocked, blocked.Action);
        Assert.Contains("P1 first", blocked.Reason);
        Assert.False(blocked.MaySendUpgrade);
    }

    [Fact]
    public void After_p1_the_job_studies_the_profile_and_does_not_send()
    {
        var p1 = new DailyUpgradeProgress(["28206"], []);
        var study = DatabaseComb.Evaluate(Row(Profile()), p1, Now, Choice1, Choice2);
        Assert.Equal(DatabaseCombAction.StudyProfile, study.Action);
        Assert.Equal(DailyUpgradePhase.CustomerDatabaseStudy, study.Phase);
        Assert.False(study.MaySendUpgrade);
        Assert.Contains("Do not send", study.Reason);
    }

    [Fact]
    public void After_p2_a_proven_profile_may_compose_a_tailored_upgrade()
    {
        var ready = new DailyUpgradeProgress(["28206", "28546"], ["28206", "28546"]);
        var send = DatabaseComb.Evaluate(Row(Profile()), ready, Now, Choice1, Choice2);
        Assert.Equal(DatabaseCombAction.ComposeTailoredUpgrade, send.Action);
        Assert.True(send.MaySendUpgrade);
        Assert.Contains("2022 Nissan Rogue SV", send.Upgrade!.Body);

        var tko = DatabaseComb.Evaluate(Row(Profile(), "6220"), ready, Now, Choice1, Choice2);
        Assert.Equal(DatabaseCombAction.Blocked, tko.Action);
    }
}
