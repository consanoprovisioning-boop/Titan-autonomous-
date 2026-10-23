namespace Titan.ChiefOfStaff;

public sealed record GroundedDraftResult(bool Ok, string Channel, string? Body, string? Block);

public static class GroundedDraft
{
    public static GroundedDraftResult Email(FactLedger ledger, DateTimeOffset nowEt, string history)
    {
        return Compose("email", ledger, nowEt, history, sms: false);
    }

    public static GroundedDraftResult Sms(FactLedger ledger, DateTimeOffset nowEt, string history)
    {
        if (!Policy.SmsOpen(nowEt))
        {
            return new GroundedDraftResult(false, "SMS", null, "SMS window closed (9:00–19:00 ET).");
        }

        return Compose("SMS", ledger, nowEt, history, sms: true);
    }

    public static GroundedDraftResult Compose(string channel, FactLedger ledger, DateTimeOffset nowEt, string history, bool sms)
    {
        var consent = Consent.BlockReason(history);
        if (consent is not null)
        {
            return new GroundedDraftResult(false, channel, null, consent);
        }

        var first = ledger.Get("firstName");
        var greeting = first is null ? "Hi," : $"Hi {first},";
        var vehicle = ledger.Get("vehicle");
        var question = ledger.Get("lastQuestion");
        var committed = ledger.Get("customerDayTime");
        var slots = AppointmentSlots.NextTwo(nowEt);
        if (slots.Count < 2 && committed is null)
        {
            return new GroundedDraftResult(false, channel, null, "No verified appointment windows to offer.");
        }

        string body;
        if (sms)
        {
            body = committed is not null
                ? $"{greeting} I have you down for {committed}. Reply if you need that moved."
                : $"{greeting} {Answer(question, vehicle)} What works better, {slots[0]} or {slots[1]}?";
        }
        else
        {
            body = committed is not null
                ? $"{greeting}\n\n{Answer(question, vehicle)}\n\nI have you down for {committed} as you wrote. Reply if you need that moved.\n\nValdosta — {Policy.Identity}"
                : $"{greeting}\n\n{Answer(question, vehicle)}\n\nWhat day and time work for a visit? {slots[0]} or {slots[1]}?\n\nValdosta — {Policy.Identity}";
        }

        body = Collapse(body);
        if (Consent.PromisesForbiddenTerms(body) || FactLedger.ContainsInventedMoney(body))
        {
            return new GroundedDraftResult(false, channel, null, "Draft blocked — forbidden payment, APR, or invented figure.");
        }

        if (!ledger.Allows(body) && Consent.PromisesForbiddenTerms(body))
        {
            return new GroundedDraftResult(false, channel, null, "Draft blocked by fact ledger.");
        }

        return new GroundedDraftResult(true, channel, body, null);
    }

    private static string Answer(string? question, string? vehicle)
    {
        if (question is not null)
        {
            return vehicle is not null
                ? $"Thanks for asking about the {vehicle}. I will confirm every number in person and will not invent a payment or rate here."
                : "Thanks for the note. I will confirm every number in person and will not invent a payment or rate here.";
        }

        return vehicle is not null
            ? $"Checking in on the {vehicle}."
            : "Checking in.";
    }

    private static string Collapse(string text)
        => string.Join("\n", text.Split('\n').Select(line => line.Trim())).Replace("\n\n\n", "\n\n", StringComparison.Ordinal).Trim();
}
