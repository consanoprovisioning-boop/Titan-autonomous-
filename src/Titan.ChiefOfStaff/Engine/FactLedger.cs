using System.Text.RegularExpressions;

namespace Titan.ChiefOfStaff;

public sealed record Fact(string Key, string Value, string Evidence);

public sealed class FactLedger
{
    private readonly Dictionary<string, Fact> _facts = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<Fact> All => _facts.Values;

    public static FactLedger FromHistory(string? history, string? rooftop = null, string? assignedTo = null)
    {
        var ledger = new FactLedger();
        if (string.IsNullOrWhiteSpace(history))
        {
            return ledger;
        }

        if (Policy.IsAllowedRooftop(rooftop))
        {
            ledger.Add("rooftop", rooftop!, $"rooftop {rooftop}");
        }

        if (!string.IsNullOrWhiteSpace(assignedTo) && WorkOrder.IsGlennAssigned(assignedTo))
        {
            ledger.Add("identity", Policy.Identity, assignedTo);
        }

        var first = FirstName(history);
        if (first is not null)
        {
            ledger.Add("firstName", first, first);
        }

        var vehicle = VehicleMention(history);
        if (vehicle is not null)
        {
            ledger.Add("vehicle", vehicle, vehicle);
        }

        var question = LastQuestion(history);
        if (question is not null)
        {
            ledger.Add("lastQuestion", question, question);
        }

        if (Policy.HasCustomerDayAndTime(history))
        {
            var quoted = QuoteDayAndTime(history);
            if (quoted is not null)
            {
                ledger.Add("customerDayTime", quoted, quoted);
            }
        }

        return ledger;
    }

    public void Add(string key, string value, string evidence)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(evidence))
        {
            return;
        }

        if (!evidence.Contains(value, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(key, "identity", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(key, "rooftop", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _facts[key] = new Fact(key, value.Trim(), evidence.Trim());
    }

    public bool Has(string key) => _facts.ContainsKey(key);

    public string? Get(string key) => _facts.TryGetValue(key, out var fact) ? fact.Value : null;

    public bool Allows(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (Consent.PromisesForbiddenTerms(text))
        {
            return false;
        }

        return !ContainsInventedMoney(text);
    }

    public static bool ContainsInventedMoney(string text)
        => Regex.IsMatch(text, @"\$\s*\d|\b\d+(\.\d+)?\s*%|\bAPR\b", RegexOptions.IgnoreCase);

    public static string? FirstName(string history)
    {
        var match = Regex.Match(
            history,
            @"\b(?:[Cc]ustomer|[Nn]ame|[Hh]i|[Hh]ello)\s*[:\-]?\s*([A-Z][a-z]{1,20})\b");
        if (match.Success)
        {
            var name = match.Groups[1].Value;
            if (IsGeneric(name))
            {
                return null;
            }

            return name;
        }

        return null;
    }

    public static string? VehicleMention(string history)
    {
        var match = Regex.Match(
            history,
            @"\b(Sentra|Altima|Rogue|Pathfinder|Frontier|Titan|Armada|Kicks|Versa|Murano|Maxima|Outlander|Eclipse Cross|Mirage|Outlander Sport|Outlander PHEV|Triton)\b",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string? LastQuestion(string history)
    {
        var matches = Regex.Matches(history, @"([^.?\n]{8,180}\?)");
        if (matches.Count == 0)
        {
            return null;
        }

        var text = matches[^1].Groups[1].Value.Trim();
        return text.Length < 8 ? null : text;
    }

    public static string? QuoteDayAndTime(string history)
    {
        if (!Policy.HasCustomerDayAndTime(history))
        {
            return null;
        }

        var day = Regex.Match(
            history,
            @"\b(?:monday|tuesday|wednesday|thursday|friday|saturday|sunday|mon|tue|tues|wed|thu|thur|thurs|fri|sat|sun)\b|\b\d{1,2}/\d{1,2}(?:/\d{2,4})?\b",
            RegexOptions.IgnoreCase);
        var time = Regex.Match(
            history,
            @"\b(?:[01]?\d|2[0-3]):[0-5]\d\b|\b(?:1[0-2]|0?[1-9])(?::[0-5]\d)?\s*(?:a\.?m\.?|p\.?m\.?)\b",
            RegexOptions.IgnoreCase);
        if (!day.Success || !time.Success)
        {
            return null;
        }

        return $"{day.Value} {time.Value}";
    }

    private static bool IsGeneric(string name)
        => name.Equals("Customer", StringComparison.OrdinalIgnoreCase)
           || name.Equals("Hello", StringComparison.OrdinalIgnoreCase)
           || name.Equals("There", StringComparison.OrdinalIgnoreCase)
           || name.Equals("Glenn", StringComparison.OrdinalIgnoreCase)
           || name.Equals("Bordine", StringComparison.OrdinalIgnoreCase);
}
