namespace Titan.ChiefOfStaff;

/// <summary>Live-loop cadence from the operating law: 15 min in the SMS
/// window, 30 min evenings, 45 min overnight (America/New_York).</summary>
public static class LoopPace
{
    public static TimeSpan Interval(DateTimeOffset nowEt)
    {
        if (Policy.SmsOpen(nowEt))
        {
            return TimeSpan.FromMinutes(15);
        }

        var hour = nowEt.Hour;
        if (hour >= 22 || hour < 6)
        {
            return TimeSpan.FromMinutes(45);
        }

        return TimeSpan.FromMinutes(30);
    }

    public static string Describe(DateTimeOffset nowEt)
    {
        var interval = Interval(nowEt);
        var kind = Policy.SmsOpen(nowEt) ? "SMS window open" : interval == TimeSpan.FromMinutes(45) ? "overnight" : "evening / SMS closed";
        return $"{(int)interval.TotalMinutes} min ({kind})";
    }
}
