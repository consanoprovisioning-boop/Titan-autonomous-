namespace Titan.ChiefOfStaff;

public static class LoopPace
{
    public static readonly TimeSpan SmsSending = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan EveningOrBlocked = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Overnight = TimeSpan.FromMinutes(45);

    public static TimeSpan NextDelay(DateTimeOffset nowEt, bool hostBlocked, bool sentThisSession)
    {
        if (hostBlocked)
        {
            return EveningOrBlocked;
        }

        var hour = nowEt.TimeOfDay;
        if (hour >= TimeSpan.FromHours(22) || hour < TimeSpan.FromHours(9))
        {
            return Overnight;
        }

        if (hour >= TimeSpan.FromHours(19))
        {
            return EveningOrBlocked;
        }

        return sentThisSession || Policy.SmsOpen(nowEt) ? SmsSending : EveningOrBlocked;
    }

    public static string Label(TimeSpan delay)
    {
        if (delay == SmsSending)
        {
            return "15m SMS-hour cycle";
        }

        if (delay == Overnight)
        {
            return "45m overnight cycle";
        }

        return "30m evening/blocked cycle";
    }
}
