namespace Titan.ChiefOfStaff;

public static class EasternClock
{
    public static DateTimeOffset Now()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        }
        catch (TimeZoneNotFoundException)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        }
    }
}
