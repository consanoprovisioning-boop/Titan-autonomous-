namespace Titan.ChiefOfStaff;

public static class Policy
{
    public const string Identity = "Bordine, Glenn";
    public const string NissanId = "28206";
    public const string MitsubishiId = "28546";
    public const string ForbiddenTkoId = "6220";

    public static readonly string[] AllowedRooftops = [NissanId, MitsubishiId];

    public static bool IsAllowedRooftop(string? rooftopId)
    {
        if (string.IsNullOrWhiteSpace(rooftopId))
        {
            return false;
        }

        var id = rooftopId.Trim();
        if (id == ForbiddenTkoId)
        {
            return false;
        }

        return AllowedRooftops.Contains(id);
    }

    public static bool IsForbiddenRooftop(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.Contains(ForbiddenTkoId, StringComparison.Ordinal)
            || text.Contains("TKO Autogroup", StringComparison.OrdinalIgnoreCase);
    }

    public static bool SmsOpen(DateTimeOffset nowEt)
    {
        if (nowEt.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        var tod = nowEt.TimeOfDay;
        return tod >= TimeSpan.FromHours(9) && tod < TimeSpan.FromHours(19);
    }

    public static bool AppointmentWindowOpen(DateTimeOffset candidateEt, bool customerRequestedTuesday)
    {
        var day = candidateEt.DayOfWeek;
        var tod = candidateEt.TimeOfDay;
        var start = TimeSpan.FromHours(9);
        var weekdayEnd = new TimeSpan(18, 30, 0);
        var saturdayEnd = new TimeSpan(17, 30, 0);

        return day switch
        {
            DayOfWeek.Sunday => false,
            DayOfWeek.Tuesday => customerRequestedTuesday && tod >= start && tod <= weekdayEnd,
            DayOfWeek.Saturday => tod >= start && tod <= saturdayEnd,
            _ => tod >= start && tod <= weekdayEnd
        };
    }

    public static bool OtherConsultantEligible(
        DateTimeOffset now,
        DateTimeOffset? lastContactByAnyone,
        bool titanTouched,
        DateTimeOffset? leadOpenedAt = null)
    {
        if (titanTouched)
        {
            return true;
        }

        if (OtherConsultantHeldByRecentContact(now, lastContactByAnyone))
        {
            return false;
        }

        if (lastContactByAnyone is null)
        {
            return leadOpenedAt is not null && now - leadOpenedAt.Value >= TimeSpan.FromHours(96);
        }

        return now - lastContactByAnyone.Value >= TimeSpan.FromHours(96);
    }

    public static bool OtherConsultantHeldByRecentContact(DateTimeOffset now, DateTimeOffset? lastQualifyingContact)
    {
        if (lastQualifyingContact is null)
        {
            return false;
        }

        return now - lastQualifyingContact.Value < TimeSpan.FromHours(72);
    }
}
