using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class WorkTick
{
    public async Task<string> RunAsync(IPage page, HostStore store, string rooftop, DateTimeOffset nowEt, DailyUpgradeProgress? dailyUpgradeProgress = null)
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
        var progress = dailyUpgradeProgress ?? DailyUpgradeProgress.Empty;
        foreach (var queue in WorkOrder.MandatoryOrder)
        {
            store.WriteHeartbeat(store.ReadHeartbeat() with { State = "Tick" + queue });
            // Glenn 2026-10-18 strict order: database prospecting (P2/P3)
            // stays closed until P1 Daily Organizer scheduled emails are
            // complete on both rooftops.
            if (queue == WorkQueue.Database
                && DailyUpgradeQueue.GateReason(DailyUpgradePhase.CustomerDatabaseStudy, progress) is { } gate)
            {
                store.Log(queue.ToString(), "held-order", gate, rooftop);
                continue;
            }

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
        store.Log(
            "appointment-commitment",
            decision.Action.ToString(),
            decision.CrmNote ?? decision.Reason,
            rooftop,
            customerKey);
        return decision;
    }

    /// <summary>
    /// P3 gatekeeper log (Glenn 2026-10-18): a tailored upgrade email may
    /// be composed/sent on the verified host only when P1 and P2 are
    /// complete for both rooftops and this customer's own profile passes
    /// TailoredUpgradeEmail.Evaluate. Thin profiles are skipped, never
    /// blasted with generic copy. Decision/log only here.
    /// </summary>
    public static TailoredUpgradeDecision HandleTailoredUpgradeCandidate(
        HostStore store, string rooftop, string customerKey,
        CustomerProfileFacts profile, DailyUpgradeProgress progress, TailoredUpgradeDecision decision)
    {
        var gate = DailyUpgradeQueue.GateReason(DailyUpgradePhase.TailoredUpgradeEmails, progress);
        store.Log(
            "tailored-upgrade",
            gate is not null ? "held-order" : decision.Disposition.ToString(),
            gate ?? string.Join(" ", decision.Reasons),
            rooftop,
            customerKey);
        return gate is not null
            ? new TailoredUpgradeDecision(TailoredUpgradeDisposition.Blocked, [gate], null, null)
            : decision;
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
