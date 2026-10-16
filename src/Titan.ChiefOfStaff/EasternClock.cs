namespace Titan.ChiefOfStaff;

public static class EasternClock
{
    public static TimeZoneInfo Zone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("America/New_York", out var ny))
        {
            return ny;
        }

        return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    }

    public static DateTimeOffset Now()
        => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone());

    public static DateTimeOffset AtLocal(int year, int month, int day, int hour, int minute)
    {
        var unspecified = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        var offset = Zone().GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset);
    }
}
