using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class AutoLaunchTests
{
    [Fact]
    public void Verified_goes_straight_to_loop()
        => Assert.Equal(AutoLaunchStep.StartLoop, AutoLaunch.Decide(true, TimeSpan.Zero, AutoLaunch.DefaultWait));

    [Fact]
    public void Unverified_keeps_checking_while_wait_budget_remains()
        => Assert.Equal(AutoLaunchStep.RunCheckin, AutoLaunch.Decide(false, TimeSpan.FromMinutes(2), AutoLaunch.DefaultWait));

    [Fact]
    public void Unverified_gives_up_after_wait_budget()
        => Assert.Equal(AutoLaunchStep.GiveUp, AutoLaunch.Decide(false, AutoLaunch.DefaultWait, AutoLaunch.DefaultWait));
}
