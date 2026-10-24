using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed record WorkOutcome(int Qualified, int Held, int Sent, string Summary, IReadOnlyList<string> Proof);

public static class AutonomousWork
{
    public static readonly string[] ComposerSelectors =
    [
        "textarea",
        "[contenteditable='true']"
    ];

    public static readonly string[] SendLabels =
    [
        "Send Email",
        "Send SMS",
        "Send Text",
        "Send"
    ];

    public static readonly TimeSpan OwnCustomerMinInterval = TimeSpan.FromHours(12);

    public static string? RecentSendSkip(DateTimeOffset? lastAttemptAt, DateTimeOffset nowEt)
    {
        if (lastAttemptAt is null || nowEt - lastAttemptAt.Value >= OwnCustomerMinInterval)
        {
            return null;
        }

        return "12h spacing — this customer already has a send attempt. Not clicking Send again.";
    }

    public static async Task<WorkOutcome> TryFirstEligibleAsync(
        IPage page,
        HostStore store,
        LearningStore learning,
        string rooftop,
        DateTimeOffset nowEt,
        IReadOnlyList<OrganizerRow> rows)
    {
        var held = 0;
        var qualified = 0;
        foreach (var row in rows)
        {
            var hold = WorkOrder.HoldReason(row, nowEt);
            if (hold is not null)
            {
                held++;
                store.Log(row.Queue.ToString(), "hold", hold, rooftop, row.CustomerKey);
                continue;
            }

            var spacing = RecentSendSkip(store.LatestSendAttemptAt(rooftop, row.CustomerKey), nowEt);
            if (spacing is not null)
            {
                held++;
                store.Log(row.Queue.ToString(), "hold", spacing, rooftop, row.CustomerKey);
                continue;
            }

            qualified++;
            var outcome = await TryRowAsync(page, store, learning, rooftop, nowEt, row);
            if (outcome.Sent > 0)
            {
                return new WorkOutcome(qualified, held, outcome.Sent, outcome.Summary, outcome.Proof);
            }

            return new WorkOutcome(qualified, held, 0, outcome.Summary, outcome.Proof);
        }

        return new WorkOutcome(qualified, held, 0, "No eligible unfinished row on this rooftop.", []);
    }

    public static async Task<WorkOutcome> TryRowAsync(
        IPage page,
        HostStore store,
        LearningStore learning,
        string rooftop,
        DateTimeOffset nowEt,
        OrganizerRow row)
    {
        var opened = await OpenRowAsync(page, learning, row.CustomerKey);
        if (!opened)
        {
            store.Log(row.Queue.ToString(), "profile", "row did not open — no send", rooftop, row.CustomerKey);
            return new WorkOutcome(1, 0, 0, "Profile did not open. No send.", []);
        }

        var history = await SafeBodyAsync(page);
        if (Policy.IsForbiddenRooftop(history))
        {
            return new WorkOutcome(1, 0, 0, "TKO 6220 blocked.", []);
        }

        var consent = Consent.BlockReason(history);
        if (consent is not null)
        {
            store.Log(row.Queue.ToString(), "hold", consent, rooftop, row.CustomerKey);
            return new WorkOutcome(1, 1, 0, consent, []);
        }

        var ownership = Policy.OwnershipBlock(nowEt, row.PurchasedAt, row.RecentInquiry, row.ModelYear);
        if (ownership is not null)
        {
            store.Log(row.Queue.ToString(), "hold", ownership, rooftop, row.CustomerKey);
            return new WorkOutcome(1, 1, 0, ownership, []);
        }

        var ledger = FactLedger.FromHistory(history, rooftop, row.AssignedTo);
        var smsOpen = Policy.SmsOpen(nowEt);
        var draft = smsOpen ? GroundedDraft.Sms(ledger, nowEt, history) : GroundedDraft.Email(ledger, nowEt, history);
        if (!draft.Ok || draft.Body is null)
        {
            if (smsOpen)
            {
                draft = GroundedDraft.Email(ledger, nowEt, history);
            }
        }

        if (!draft.Ok || draft.Body is null)
        {
            store.Log(row.Queue.ToString(), "draft", draft.Block ?? "blocked", rooftop, row.CustomerKey);
            return new WorkOutcome(1, 0, 0, draft.Block ?? "Draft blocked.", []);
        }

        var filled = await FillComposerAsync(page, learning, draft.Body);
        if (!filled)
        {
            store.Log(row.Queue.ToString(), "composer", "no live composer — no send from a list row", rooftop, row.CustomerKey);
            return new WorkOutcome(1, 0, 0, "No live composer. Eligible work still needs a profile composer and CRM history under Bordine, Glenn.", []);
        }

        var beforeSend = await SafeBodyAsync(page);
        var sent = await ClickSendAsync(page, learning, draft.Channel);
        if (!sent)
        {
            store.Log(row.Queue.ToString(), "send", "Send control not found — no send counted", rooftop, row.CustomerKey);
            return new WorkOutcome(1, 0, 0, "Send control not found. No send counted.", []);
        }

        store.Log(row.Queue.ToString(), "send-attempt", "Send clicked; CRM proof still required", rooftop, row.CustomerKey);
        await page.WaitForTimeoutAsync(800);
        var proofBody = await SafeBodyAsync(page);
        if (!LooksLikeNewSendProof(beforeSend, proofBody, draft.Channel))
        {
            store.Log(row.Queue.ToString(), "proof", "CRM history did not confirm a new send", rooftop, row.CustomerKey);
            return new WorkOutcome(1, 0, 0, "Clicked Send but CRM history did not confirm a new completion. Not counted.", []);
        }

        var note = draft.Channel == "SMS"
            ? "SMS text completed via system."
            : "Manual email completed via system.";
        store.Log(row.Queue.ToString(), "send", note, rooftop, row.CustomerKey);
        learning.RememberDraft(draft.Channel, rooftop, sent: true, reply: false);
        return new WorkOutcome(1, 0, 1, $"{draft.Channel} confirmed in CRM history under {Policy.Identity}.", [note]);
    }

    public static bool LooksLikeSendProof(string history, string channel)
        => LooksLikeNewSendProof("", history, channel);

    public static bool LooksLikeNewSendProof(string? before, string? after, string channel)
    {
        if (string.IsNullOrWhiteSpace(after) || Policy.IsForbiddenRooftop(after))
        {
            return false;
        }

        if (!after.Contains("Bordine", StringComparison.OrdinalIgnoreCase)
            && !after.Contains("Glenn", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var phrase = channel.Equals("SMS", StringComparison.OrdinalIgnoreCase)
            ? "SMS text completed via system"
            : "Manual email completed via system";
        return CountPhrase(after, phrase) > CountPhrase(before ?? "", phrase);
    }

    public static int CountPhrase(string text, string phrase)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(phrase))
        {
            return 0;
        }

        var count = 0;
        var start = 0;
        while (start < text.Length)
        {
            var found = text.IndexOf(phrase, start, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                return count;
            }

            count++;
            start = found + phrase.Length;
        }

        return count;
    }

    private static async Task<bool> OpenRowAsync(IPage page, LearningStore learning, string customerKey)
    {
        var snippet = customerKey.Length > 48 ? customerKey[..48] : customerKey;
        if (snippet.Length < 8)
        {
            return false;
        }

        try
        {
            var locator = page.GetByText(snippet, new PageGetByTextOptions { Exact = false });
            if (await locator.CountAsync() == 0)
            {
                learning.RememberSelector("row:" + snippet, false);
                return false;
            }

            await locator.First.ClickAsync();
            await page.WaitForTimeoutAsync(700);
            learning.RememberSelector("row:" + snippet, true);
            return true;
        }
        catch (PlaywrightException)
        {
            learning.RememberSelector("row:" + snippet, false);
            return false;
        }
    }

    private static async Task<bool> FillComposerAsync(IPage page, LearningStore learning, string body)
    {
        foreach (var selector in learning.RankedSelectors(ComposerSelectors))
        {
            try
            {
                var matches = page.Locator(selector);
                if (await matches.CountAsync() != 1)
                {
                    learning.RememberSelector(selector, false);
                    continue;
                }

                await matches.First.FillAsync(body);
                learning.RememberSelector(selector, true);
                return true;
            }
            catch (PlaywrightException)
            {
                learning.RememberSelector(selector, false);
            }
        }

        return false;
    }

    private static async Task<bool> ClickSendAsync(IPage page, LearningStore learning, string channel)
    {
        var labels = channel.Equals("SMS", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Send SMS", "Send Text", "Send" }
            : new[] { "Send Email", "Send" };
        foreach (var label in learning.RankedSelectors(labels))
        {
            try
            {
                var button = page.GetByText(label, new PageGetByTextOptions { Exact = true });
                if (await button.CountAsync() != 1)
                {
                    learning.RememberSelector("send:" + label, false);
                    continue;
                }

                await button.First.ClickAsync();
                learning.RememberSelector("send:" + label, true);
                return true;
            }
            catch (PlaywrightException)
            {
                learning.RememberSelector("send:" + label, false);
            }
        }

        return false;
    }

    private static async Task<string> SafeBodyAsync(IPage page)
    {
        try
        {
            return await page.InnerTextAsync("body");
        }
        catch (PlaywrightException)
        {
            return page.Url;
        }
    }
}
