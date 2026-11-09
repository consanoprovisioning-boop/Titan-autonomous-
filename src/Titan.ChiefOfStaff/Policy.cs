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
        => !string.IsNullOrWhiteSpace(correspondence)
           && FactLedger.QuoteDayAndTime(correspondence) is not null;

    /// <summary>
    /// "sat" and "sun" are ordinary words as well as day abbreviations.
    /// "I sat at 10:30" and "the sun at 4:00 PM" are not Saturday or Sunday.
    /// A real abbreviation stands alone: "Sat at 10:30 AM".
    /// Store hours and a truck size are not a customer visit.
    /// </summary>
    internal static bool IsWeekdayMention(string text, System.Text.RegularExpressions.Match day)
    {
        if (IsLoggedActivity(text, day.Index))
        {
            return false;
        }

        if (IsStoreHoursLead(text, day.Index) || IsTruckSize(text, day))
        {
            return false;
        }

        var word = day.Value;
        if (word.Equals("sat", StringComparison.OrdinalIgnoreCase)
            || word.Equals("sun", StringComparison.OrdinalIgnoreCase))
        {
            return !PrecededByWord(text, day.Index);
        }

        return true;
    }

    private static bool IsStoreHoursLead(string text, int index)
    {
        if (index <= 0)
        {
            return false;
        }

        var from = Math.Max(0, index - 48);
        var prefix = text.Substring(from, index - from);
        return StoreHoursLead.IsMatch(prefix);
    }

    private static bool IsTruckSize(string text, System.Text.RegularExpressions.Match day)
    {
        if (!day.Value.Contains('/'))
        {
            return false;
        }

        var rest = text[(day.Index + day.Length)..];
        return System.Text.RegularExpressions.Regex.IsMatch(
            rest,
            @"^\s*-?\s*tons?\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// A CRM activity stamp names when the store called, emailed, or texted.
    /// "Called on Monday at 2:30 PM" is that stamp, not the customer choosing a visit.
    /// The verb sits immediately in front of the day or the clock, with only on/at/around between.
    /// </summary>
    internal static bool IsLoggedActivity(string text, int index)
    {
        if (index <= 0)
        {
            return false;
        }

        var from = Math.Max(0, index - 48);
        var prefix = text.Substring(from, index - from);
        return LoggedActivity.IsMatch(prefix);
    }

    private static bool PrecededByWord(string text, int index)
    {
        var i = index - 1;
        while (i >= 0 && char.IsWhiteSpace(text[i]))
        {
            i--;
        }

        return i >= 0 && char.IsLetterOrDigit(text[i]);
    }

    public static string? AppointmentSetBlock(string? correspondence)
    {
        if (!HasCustomerDayAndTime(correspondence))
        {
            return "Appointment Set requires a customer day and time in correspondence.";
        }

        return null;
    }

    private static readonly System.Text.RegularExpressions.Regex LoggedActivity = new(
        @"\b(?:called|e-?mailed|texted|voicemail|contacted|phoned|phone\s+call|updated|created)\s+(?:(?:on|at|around)\s+)?$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex StoreHoursLead = new(
        @"\b(?:hours|open|opens|opening|close|closes|closing|closed)\b[\s:.-]*(?:(?:on|at|around)\s+)?$",
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
