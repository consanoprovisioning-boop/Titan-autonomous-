using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class WorkTick
{
    public async Task<string> RunAsync(IPage page, HostStore store, string rooftop, DateTimeOffset nowEt)
        => await RunAsync(page, store, rooftop, nowEt, paths: null, progress: DailyUpgradeProgress.Empty);

    public async Task<string> RunAsync(
        IPage page,
        HostStore store,
        string rooftop,
        DateTimeOffset nowEt,
        HostPaths? paths,
        DailyUpgradeProgress? progress)
    {
        if (Policy.IsForbiddenRooftop(rooftop) || !Policy.IsAllowedRooftop(rooftop))
        {
            store.Log("guard", "rooftop", "blocked", rooftop);
            return "Blocked: rooftop not 28206/28546.";
        }

        if (ClusterPartition.HoldLaunch(paths))
        {
            store.Log("cluster", "hold", "launch held until Glenn releases the cluster. Alienware-18 is MAIN. No send.", rooftop);
            return "Blocked: cluster launch is held. Alienware-18 is MAIN. Install only. No send.";
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
        var skippedLane = 0;
        var leased = 0;
        var scaleOut = ClusterPartition.IsScaleOut(
            presentNodeIds: paths?.ReadEnrolledNodeId() is { } self ? [self] : null,
            clusterSizeEnv: Environment.GetEnvironmentVariable("TITAN_CLUSTER_SIZE"),
            modeFileScaleOut: paths?.ReadScaleOutMode() == true);
        var node = ClusterPartition.Resolve(
            Environment.MachineName,
            Environment.GetEnvironmentVariable("TITAN_NODE_NAME"),
            paths?.ReadEnrolledNodeId(),
            scaleOut);
        if (node is not null && !ClusterPartition.MayWorkRooftop(node, rooftop))
        {
            store.Log("cluster", "rooftop", $"{node.DisplayName} does not own {rooftop}. No send.", rooftop);
            return $"Tick {rooftop}: {node.DisplayName} is assigned {string.Join("/", node.AllowedRooftops)} only. No send.";
        }

        CustomerLeaseStore? leases = paths is null ? null : new CustomerLeaseStore(CustomerLeaseStore.ResolveLeasePath(paths));
        var gate = progress ?? DailyUpgradeProgress.Empty;
        foreach (var queue in WorkOrder.MandatoryOrder)
        {
            if (node is not null)
            {
                var laneBlock = ClusterPartition.LaneBlockReason(node, queue, gate);
                if (laneBlock is not null)
                {
                    skippedLane++;
                    store.Log(queue.ToString(), "lane", laneBlock, rooftop);
                    continue;
                }
            }

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

                if (node is not null
                    && queue != WorkQueue.Database
                    && !ClusterPartition.MayWorkTaskKind(node, ClusterPartition.GuessTaskKind(row.CustomerKey)))
                {
                    skippedLane++;
                    store.Log(queue.ToString(), "lane-task", $"{node.DisplayName} does not own this task kind. No send.", rooftop, row.CustomerKey);
                    continue;
                }

                if (leases is not null && node is not null)
                {
                    var lease = leases.TryAcquire(node.NodeId, rooftop, row.CustomerKey, queue.ToString(), nowEt);
                    if (lease is null)
                    {
                        held++;
                        store.Log(queue.ToString(), "lease", "Another cluster node holds this customer. No double-send.", rooftop, row.CustomerKey);
                        continue;
                    }

                    leased++;
                }

                ready++;
                var channel = smsOpen ? "SMS+email window" : "email-only window";
                store.Log(queue.ToString(), "qualified", $"{channel}; no send from a list row. Eligible work still needs consent, identity, and CRM history under Bordine, Glenn.", rooftop, row.CustomerKey);
            }
        }

        var nodeLabel = node?.DisplayName ?? "unenrolled-node";
        return $"Tick {rooftop} {nodeLabel}: {seen} visible rows, {ready} qualified, {held} held, {skippedLane} other-lane queues skipped, {leased} leases. Sends only after a live composer is filled from the profile and CRM history shows the completed activity. SMS {(smsOpen ? "open" : "closed")}.";
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
