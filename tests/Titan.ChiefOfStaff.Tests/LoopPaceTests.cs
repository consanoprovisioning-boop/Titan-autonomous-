using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class LoopPaceTests
{
    private static DateTimeOffset Et(int hour, int minute = 0)
        => new(2026, 10, 22, hour, minute, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Sms_hours_pace_is_15_minutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(15), LoopPace.Interval(Et(9)));
        Assert.Equal(TimeSpan.FromMinutes(15), LoopPace.Interval(Et(18, 59)));
    }

    [Fact]
    public void Evening_pace_is_30_minutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), LoopPace.Interval(Et(19)));
        Assert.Equal(TimeSpan.FromMinutes(30), LoopPace.Interval(Et(21, 30)));
        Assert.Equal(TimeSpan.FromMinutes(30), LoopPace.Interval(Et(7)));
    }

    [Fact]
    public void Overnight_pace_is_45_minutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(45), LoopPace.Interval(Et(22)));
        Assert.Equal(TimeSpan.FromMinutes(45), LoopPace.Interval(Et(2)));
        Assert.Equal(TimeSpan.FromMinutes(45), LoopPace.Interval(Et(5, 59)));
    }
}
