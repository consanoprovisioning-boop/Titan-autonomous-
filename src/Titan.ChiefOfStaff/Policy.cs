namespace Titan.ChiefOfStaff;

public static class Policy
{
    public const string Identity = "Bordine, Glenn";
    public const string NissanId = "28206";
    public const string MitsubishiId = "28546";
    public const string ForbiddenTkoId = "6220";

    public const int OwnershipScreenDays = 180;
    public static readonly int? OwnershipScreenMonths = null;

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

    public static bool IsBordineIdentity(string? text)
        => !string.IsNullOrWhiteSpace(text)
           && text.Contains("Bordine", StringComparison.OrdinalIgnoreCase);

    public static bool SmsOpen(DateTimeOffset nowEt)
    {
        var tod = nowEt.TimeOfDay;
        return tod >= TimeSpan.FromHours(9) && tod < TimeSpan.FromHours(19);
    }

    public static bool SmsOpenEastern(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, EasternClock.Zone());
        return SmsOpen(local);
    }

    public static bool PassesOwnershipScreen(
        DateTimeOffset now,
        DateTimeOffset? purchasedAt,
        bool recentInquiry,
        int? modelYear)
    {
        _ = modelYear;
        if (recentInquiry)
        {
            return true;
        }

        if (purchasedAt is null)
        {
            return false;
        }

        return now - purchasedAt.Value <= TimeSpan.FromDays(OwnershipScreenDays);
    }

    public static string? OwnershipBlock(
        DateTimeOffset now,
        DateTimeOffset? purchasedAt,
        bool recentInquiry,
        int? modelYear)
    {
        if (recentInquiry)
        {
            return null;
        }

        if (purchasedAt is null)
        {
            return modelYear is null
                ? null
                : "Model year never proves ownership duration.";
        }

        if (!PassesOwnershipScreen(now, purchasedAt, recentInquiry: false, modelYear))
        {
            return "Outside the 180-day ownership screen.";
        }

        return null;
    }

    public static bool HasCustomerDayAndTime(string? correspondence)
    {
        if (string.IsNullOrWhiteSpace(correspondence))
        {
            return false;
        }

        return DayInCorrespondence.IsMatch(correspondence)
               && ClockTimeInCorrespondence.IsMatch(correspondence);
    }

    public static string? AppointmentSetBlock(string? correspondence)
    {
        if (!HasCustomerDayAndTime(correspondence))
        {
            return "Appointment Set requires a customer day and time in correspondence.";
        }

        return null;
    }

    private static readonly System.Text.RegularExpressions.Regex DayInCorrespondence = new(
        @"\b(?:monday|tuesday|wednesday|thursday|friday|saturday|sunday|mon|tue|tues|wed|thu|thur|thurs|fri|sat|sun)\b|\b\d{1,2}/\d{1,2}(?:/\d{2,4})?\b|\b(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\s+\d{1,2}\b",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex ClockTimeInCorrespondence = new(
        @"\b(?:[01]?\d|2[0-3]):[0-5]\d\b|\b(?:1[0-2]|0?[1-9])(?::[0-5]\d)?\s*(?:a\.?m\.?|p\.?m\.?)\b|\bat\s+(?:1[0-2]|0?[1-9])(?::[0-5]\d)?\b",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

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
