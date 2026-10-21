using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class WorkTick
{
    public async Task<string> RunAsync(IPage page, HostStore store, string rooftop, DateTimeOffset nowEt)
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
            return "Daily Organizer is not visible. In Eleads set status Open and contact type All, then tick again. No send was made.";
        }

        var smsOpen = Policy.SmsOpen(nowEt);
        var seen = 0;
        var held = 0;
        var ready = 0;
        foreach (var queue in WorkOrder.MandatoryOrder)
        {
            store.WriteHeartbeat(store.ReadHeartbeat() with { State = "Tick" + queue });
            var rows = ParseVisibleRows(organizer, queue);
            foreach (var row in rows)
            {
                seen++;
                var hold = WorkOrder.HoldReason(row, nowEt);
                if (hold is not null)
                {
                    held++;
                    store.Log(queue.ToString(), "hold", hold, rooftop, row.CustomerKey);
                    continue;
                }

                ready++;
                var channel = smsOpen ? "SMS+email window" : "email-only window";
                store.Log(queue.ToString(), "qualified", $"{channel}; no send from a list row. Eligible work still needs consent, identity, and CRM history under Bordine, Glenn.", rooftop, row.CustomerKey);
            }
        }

        return $"Tick {rooftop}: {seen} visible rows, {ready} qualified, {held} held. Sends only after a live composer is filled from the profile and CRM history shows the completed activity. SMS {(smsOpen ? "open" : "closed")}.";
    }

    /// <summary>
    /// Appointment-commitment fast path (Glenn 2026-10-18): preempts the
    /// mandatory queue order for this lead. Decision only here — the actual
    /// same-channel reply, Eleads booking, Appointment Set status change, and
    /// CRM note are executed on the Glenn-verified Alienware host session and
    /// count only with CRM history proof under Bordine, Glenn.
    /// </summary>
    public static AppointmentCommitmentDecision HandleInboundReply(
        HostStore store, string rooftop, string customerKey, AppointmentCommitmentRequest request)
    {
        var decision = AppointmentCommitment.Evaluate(request);
        var detail = decision.CrmNote is null ? decision.Reason : $"{decision.Reason} {decision.CrmNote}";
        store.Log(
            "appointment-commitment",
            decision.Action.ToString(),
            detail,
            rooftop,
            customerKey);
        return decision;
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
