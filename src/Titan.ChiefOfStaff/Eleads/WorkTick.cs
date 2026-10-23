using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class WorkTick
{
    public async Task<string> RunAsync(IPage page, HostStore store, LearningStore learning, string rooftop, DateTimeOffset nowEt)
    {
        if (Policy.IsForbiddenRooftop(rooftop) || !Policy.IsAllowedRooftop(rooftop))
        {
            store.Log("guard", "rooftop", "blocked", rooftop);
            return "Blocked: rooftop not 28206/28546.";
        }

        await page.BringToFrontAsync();
        var body = await SafeBodyAsync(page);
        if (Policy.IsForbiddenRooftop(body))
        {
            store.Log("guard", "tko", "blocked", rooftop);
            return "Blocked: TKO Autogroup detected.";
        }

        var organizer = await EnsureOrganizerAsync(page);
        if (organizer is null)
        {
            store.Log("organizer", "open", "not visible", rooftop);
            return "Daily Organizer is not visible. In Eleads set status Open and contact type All, then the next cycle retries. No send was made.";
        }

        var started = Environment.TickCount;
        var seen = 0;
        var held = 0;
        var ready = 0;
        var sent = 0;
        var proof = new List<string>();
        string? workedQueue = null;
        foreach (var queue in WorkOrder.MandatoryOrder)
        {
            store.WriteHeartbeat(store.ReadHeartbeat() with { State = "Tick" + queue });
            var rows = ParseVisibleRows(organizer, queue);
            var unfinished = rows.Where(row => WorkOrder.HoldReason(row, nowEt) is null).ToArray();
            foreach (var row in rows)
            {
                seen++;
                if (WorkOrder.HoldReason(row, nowEt) is not null)
                {
                    held++;
                }
                else
                {
                    ready++;
                }
            }

            if (unfinished.Length == 0)
            {
                learning.RecordExperience(rooftop, queue.ToString(), 0, 0, 0, rows.Count, "rank empty");
                continue;
            }

            workedQueue = queue.ToString();
            var outcome = await AutonomousWork.TryFirstEligibleAsync(page, store, learning, rooftop, nowEt, unfinished);
            sent += outcome.Sent;
            proof.AddRange(outcome.Proof);
            var drafts = unfinished.Select(row =>
            {
                var ledger = FactLedger.FromHistory(row.HistoryText, rooftop, row.AssignedTo);
                return Policy.SmsOpen(nowEt)
                    ? GroundedDraft.Sms(ledger, nowEt, row.HistoryText)
                    : GroundedDraft.Email(ledger, nowEt, row.HistoryText);
            }).ToArray();
            var specialists = SpecialistCore.Run(rooftop, rows, nowEt, drafts, proof);
            store.Log(queue.ToString(), "specialists", string.Join(",", specialists.Select(s => s.Specialist + ":" + s.Status)), rooftop);
            learning.RecordExperience(rooftop, queue.ToString(), Environment.TickCount - started, outcome.Qualified, outcome.Sent, outcome.Held, outcome.Summary);
            var smsOpen = Policy.SmsOpen(nowEt);
            return $"Tick {rooftop} {queue}: {seen} visible, {ready} qualified, {held} held, {sent} CRM-proven send(s). SMS {(smsOpen ? "open" : "closed")}. {outcome.Summary}";
        }

        learning.RecordExperience(rooftop, workedQueue ?? "none", Environment.TickCount - started, ready, sent, held, "all ranks clear");
        return $"Tick {rooftop}: {seen} visible rows, {ready} qualified, {held} held, {sent} CRM-proven send(s). All ranks clear this pass. SMS {(Policy.SmsOpen(nowEt) ? "open" : "closed")}.";
    }

    public static IReadOnlyList<OrganizerRow> ParseVisibleRows(string organizerText, WorkQueue queue)
    {
        var rows = new List<OrganizerRow>();
        foreach (var raw in organizerText.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!LineLooksLikeRow(raw, queue))
            {
                continue;
            }

            rows.Add(new OrganizerRow(
                CustomerKey: raw,
                AssignedTo: raw,
                Queue: queue,
                LastContact: null,
                OpenedAt: null,
                TitanTouched: raw.Contains("Titan", StringComparison.OrdinalIgnoreCase),
                HistoryText: raw));
        }

        return rows;
    }

    private static bool LineLooksLikeRow(string line, WorkQueue queue)
    {
        if (line.Length < 8 || Policy.IsForbiddenRooftop(line))
        {
            return false;
        }

        return queue switch
        {
            WorkQueue.NewLeads => Contains(line, "new lead", "internet", "fresh"),
            WorkQueue.Planner => Contains(line, "planner", "scheduled", "appointment"),
            WorkQueue.Overdue => Contains(line, "overdue", "past due"),
            WorkQueue.Database => Contains(line, "equity", "abandon", "database", "in-market", "upgrade"),
            _ => false
        };
    }

    private static async Task<string?> EnsureOrganizerAsync(IPage page)
    {
        var body = await SafeBodyAsync(page);
        if (LooksLikeOrganizer(body))
        {
            return body;
        }

        var locator = page.GetByText("Daily Organizer", new PageGetByTextOptions { Exact = false });
        if (await locator.CountAsync() > 0)
        {
            await locator.First.ClickAsync();
            await page.WaitForTimeoutAsync(800);
            body = await SafeBodyAsync(page);
        }

        return LooksLikeOrganizer(body) ? body : null;
    }

    private static bool LooksLikeOrganizer(string body)
        => Contains(body, "daily organizer")
           || Contains(body, "organizer") && Contains(body, "open");

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

    private static bool Contains(string text, params string[] needles)
        => needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));
}
