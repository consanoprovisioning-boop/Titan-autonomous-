namespace Titan.ChiefOfStaff;

/// <summary>
/// 4-PC local cluster for Titan COS. Alienware-18 and Desktop3 are named.
/// PC3 and PC4 use the reusable node-onboarding template until Glenn names them.
/// Rooftop + lane splits prevent two nodes from sending to the same customer.
/// This type only assigns. Cloud Linux never opens Eleads or claims sends.
/// </summary>
public enum ClusterLane
{
    P1Organizer,
    P2P3DatabaseUpgrade
}

public enum ClusterJobKind
{
    AppointmentCommitFastPath,
    OverdueEmailClearance,
    OverdueCallReschedule,
    DailyOrganizerRemainder,
    SmsInWindow,
    DatabaseComb,
    TailoredUpgradeEmail
}

public sealed record ClusterNodeSpec(
    string NodeId,
    string DisplayName,
    string HostnameHint,
    string RooftopId,
    ClusterLane Lane,
    IReadOnlyList<ClusterJobKind> Jobs,
    string Role)
{
    public bool OwnsRooftop(string? rooftopId)
        => Policy.IsAllowedRooftop(rooftopId)
           && string.Equals(RooftopId, rooftopId!.Trim(), StringComparison.Ordinal);

    public bool OwnsJob(ClusterJobKind job) => Jobs.Contains(job);
}

public static class ClusterPartition
{
    public const string Alienware18 = "Alienware-18";
    public const string Desktop3 = "Desktop3";
    public const string Node3Template = "Node3";
    public const string Node4Template = "Node4";

    public static IReadOnlyList<ClusterNodeSpec> Nodes { get; } =
    [
        new(
            Alienware18,
            "Alienware-18 (PC1)",
            "Alienware-18",
            Policy.NissanId,
            ClusterLane.P1Organizer,
            [
                ClusterJobKind.AppointmentCommitFastPath,
                ClusterJobKind.OverdueEmailClearance,
                ClusterJobKind.OverdueCallReschedule,
                ClusterJobKind.DailyOrganizerRemainder,
                ClusterJobKind.SmsInWindow
            ],
            "Valdosta Nissan 28206 P1 — overdue emails, overdue call reschedule, Daily Organizer remainder, SMS in window. Fastest legal path toward 60 actions/hour."),
        new(
            Desktop3,
            "Desktop3 (PC2)",
            "Desktop3",
            Policy.MitsubishiId,
            ClusterLane.P1Organizer,
            [
                ClusterJobKind.AppointmentCommitFastPath,
                ClusterJobKind.OverdueEmailClearance,
                ClusterJobKind.OverdueCallReschedule,
                ClusterJobKind.DailyOrganizerRemainder,
                ClusterJobKind.SmsInWindow
            ],
            "Valdosta Mitsubishi 28546 P1 — same high-velocity organizer clearance as PC1, other rooftop. Clone + worker-start is Glenn-local; cloud does not copy files onto Desktop3."),
        new(
            Node3Template,
            "Node3 (PC3, unnamed until enrolled)",
            "Node3",
            Policy.NissanId,
            ClusterLane.P2P3DatabaseUpgrade,
            [
                ClusterJobKind.AppointmentCommitFastPath,
                ClusterJobKind.DatabaseComb,
                ClusterJobKind.TailoredUpgradeEmail
            ],
            "Valdosta Nissan 28206 P2/P3 — comb database then tailored upgrade emails. Blocked until Nissan P1 is clear."),
        new(
            Node4Template,
            "Node4 (PC4, unnamed until enrolled)",
            "Node4",
            Policy.MitsubishiId,
            ClusterLane.P2P3DatabaseUpgrade,
            [
                ClusterJobKind.AppointmentCommitFastPath,
                ClusterJobKind.DatabaseComb,
                ClusterJobKind.TailoredUpgradeEmail
            ],
            "Valdosta Mitsubishi 28546 P2/P3 — comb database then tailored upgrade emails. Blocked until Mitsubishi P1 is clear.")
    ];

    public static ClusterNodeSpec? Find(string? nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        return Nodes.FirstOrDefault(n =>
            string.Equals(n.NodeId, nodeId.Trim(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(n.HostnameHint, nodeId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static ClusterNodeSpec? Resolve(
        string? hostname,
        string? envNodeName,
        string? enrolledNodeId)
    {
        foreach (var candidate in new[] { enrolledNodeId, envNodeName, hostname })
        {
            var match = MatchHint(candidate);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    public static bool MayWorkRooftop(ClusterNodeSpec node, string? rooftopId)
        => node.OwnsRooftop(rooftopId);

    public static bool MayWorkQueue(ClusterNodeSpec node, WorkQueue queue, DailyUpgradeProgress progress)
    {
        if (!MayWorkRooftop(node, node.RooftopId))
        {
            return false;
        }

        return node.Lane switch
        {
            ClusterLane.P1Organizer => queue is WorkQueue.NewLeads or WorkQueue.Planner or WorkQueue.Overdue,
            ClusterLane.P2P3DatabaseUpgrade =>
                queue == WorkQueue.Database
                && DailyUpgradeQueue.CanStartOnRooftop(DailyUpgradePhase.CustomerDatabaseStudy, node.RooftopId, progress),
            _ => false
        };
    }

    public static string? LaneBlockReason(ClusterNodeSpec node, WorkQueue queue, DailyUpgradeProgress progress)
    {
        if (!Policy.IsAllowedRooftop(node.RooftopId))
        {
            return "Cluster node rooftop is not 28206 or 28546.";
        }

        if (node.Lane == ClusterLane.P1Organizer && queue == WorkQueue.Database)
        {
            return $"{node.DisplayName} is the P1 organizer lane for {node.RooftopId}. Database comb belongs to the P2/P3 node on this rooftop.";
        }

        if (node.Lane == ClusterLane.P2P3DatabaseUpgrade && queue != WorkQueue.Database)
        {
            return $"{node.DisplayName} is the P2/P3 lane for {node.RooftopId}. Organizer / overdue clearance belongs to the P1 node on this rooftop.";
        }

        if (node.Lane == ClusterLane.P2P3DatabaseUpgrade && queue == WorkQueue.Database)
        {
            return DailyUpgradeQueue.GateReasonOnRooftop(
                DailyUpgradePhase.CustomerDatabaseStudy,
                node.RooftopId,
                progress);
        }

        return null;
    }

    public static IReadOnlyList<ClusterJobKind> HighVelocityJobs { get; } =
    [
        ClusterJobKind.OverdueEmailClearance,
        ClusterJobKind.OverdueCallReschedule
    ];

    private static ClusterNodeSpec? MatchHint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        var folded = text.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        foreach (var node in Nodes)
        {
            if (string.Equals(node.NodeId, text, StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.HostnameHint, text, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var hint = node.HostnameHint.Replace("-", "", StringComparison.Ordinal);
            if (folded.Contains(hint, StringComparison.OrdinalIgnoreCase)
                || text.Contains(node.HostnameHint, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }
}
