namespace Titan.ChiefOfStaff;

public static class Consent
{
    public static string? BlockReason(string? historyText)
    {
        if (string.IsNullOrWhiteSpace(historyText))
        {
            return "Manual verification required — history did not load.";
        }

        var text = historyText;
        if (Contains(text, "unable to obtain funding"))
        {
            return "Funding bypass — no later inquiry on file in this text.";
        }

        if (Contains(text, "do not text", "dnt", "opt-out", "opt out", "unsubscribe", "hard bounce")
            || WrittenChannelClosed(text)
            || System.Text.RegularExpressions.Regex.IsMatch(text, @"\bstop\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return "SMS/email consent block (DNT, Stop, opt-out, or bounce).";
        }

        // DNC closes the phone channel only. Callers treat any reason as a full
        // email and SMS hold, and this host does not place calls.

        if (Contains(text, "bought elsewhere", "not in the market", "no further contact", "do not contact"))
        {
            return "Customer asked off or ended the search.";
        }

        return null;
    }

    public static bool PromisesForbiddenTerms(string? draft)
    {
        if (string.IsNullOrWhiteSpace(draft))
        {
            return true;
        }

        var text = draft;
        if (text.Contains("all possible qualifying incentives, rebates, and special manufacturer APR programs", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Replace("all possible qualifying incentives, rebates, and special manufacturer APR programs", "", StringComparison.OrdinalIgnoreCase);
        }

        return Contains(text, "you are approved", "you're approved", "$", "/mo", "per month", "apr ", " at apr", "rebate of", "i can get you", "guaranteed payment", "we will pay");
    }

    private static bool WrittenChannelClosed(string text)
        => Contains(
            text,
            "do not email",
            "do not e-mail",
            "don't email",
            "do not call or email",
            "do not call or text",
            "don't call or email",
            "don't call or text");

    private static bool Contains(string text, params string[] needles)
        => needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));
}
