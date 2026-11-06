namespace Titan.ChiefOfStaff;

/// <summary>
/// Local cluster for Titan COS. The live 4-PC go-live set is unchanged:
/// Alienware-18, Desktop3, Node3, Desktop4 (Node4). Desktop5 (Node5) and
/// Desktop6 (Node6) are named 8-PC overflow; Node7–Node8 stay unnamed until enrolled.
/// Same clone / %USERPROFILE% worker / CDP 9222 path. Lanes for Desktop5/6 are provisional.
/// This type only assigns. Cloud Linux never opens Eleads or claims sends.
/// </summary>
public enum ClusterLane
{
    P1Organizer,
    P2P3DatabaseUpgrade,
    P1Email,
    P1Call,
    SmsInWindow,
    AppointmentCommit
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
    string Role,
    IReadOnlyList<string>? Rooftops = null,
    IReadOnlyList<string>? Aliases = null)
{
    public IReadOnlyList<string> AllowedRooftops =>
        Rooftops is { Count: > 0 } ? Rooftops : [RooftopId];

    public IReadOnlyList<string> KnownAliases => Aliases ?? [];

    public bool OwnsRooftop(string? rooftopId)
        => Policy.IsAllowedRooftop(rooftopId)
           && AllowedRooftops.Any(r => string.Equals(r, rooftopId!.Trim(), StringComparison.Ordinal));

    public bool OwnsJob(ClusterJobKind job) => Jobs.Contains(job);
}

public static class ClusterPartition
{
    public const string Alienware18 = "Alienware-18";
    public const string Desktop3 = "Desktop3";
    public const string Desktop4 = "Desktop4";
    public const string Desktop5 = "Desktop5";
    public const string Desktop6 = "Desktop6";
    public const string Node3Template = "Node3";
    public const string Node4Template = "Node4";
    public const string Node5Template = "Node5";
    public const string Node6Template = "Node6";
    public const string Node7Template = "Node7";
    public const string Node8Template = "Node8";

    public static readonly string[] OverflowNodeIds =
    [
        Desktop5, Desktop6, Node5Template, Node6Template, Node7Template, Node8Template
    ];

    public static IReadOnlyList<ClusterJobKind> HighVelocityJobs { get; } =
    [
        ClusterJobKind.OverdueEmailClearance,
        ClusterJobKind.OverdueCallReschedule
    ];

    /// <summary>Live 4-PC go-live. Do not replace this set when adding Nodes 5–8.</summary>
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
            Desktop4,
            "Desktop4 (PC4 / Node4)",
            "Desktop4",
            Policy.MitsubishiId,
            ClusterLane.P2P3DatabaseUpgrade,
            [
                ClusterJobKind.AppointmentCommitFastPath,
                ClusterJobKind.DatabaseComb,
                ClusterJobKind.TailoredUpgradeEmail
            ],
            "Provisional Valdosta Mitsubishi 28546 P2/P3 — comb database then tailored upgrade emails after Desktop3 P1. Final COS tasks wait until Glenn says the cluster is complete. Cloud does not copy files onto Desktop4. Enroll as Desktop4 or Node4. Do not launch until Glenn releases the hold.",
            Aliases: [Node4Template])
    ];

    /// <summary>
    /// 8-PC scale-out. PC1–PC4 keep their node ids. When scale-out is on,
    /// PC1/PC2 narrow to P1 email so Desktop5/Desktop6 (Node5/Node6, provisional)
    /// can take P1 calls without colliding. Node3 and Desktop4 (Node4) stay P2/P3.
    /// Node7 is SMS; Node8 is appointments.
    /// </summary>
    public static IReadOnlyList<ClusterNodeSpec> ScaleOutNodes { get; } =
    [
        new(
            Alienware18,
            "Alienware-18 (PC1, 8-PC email lane)",
            "Alienware-18",
            Policy.NissanId,
            ClusterLane.P1Email,
            [ClusterJobKind.OverdueEmailClearance, ClusterJobKind.DailyOrganizerRemainder],
            "8-PC: Valdosta Nissan 28206 P1 email only. Calls move to Desktop5 (Node5, provisional); SMS to Node7; appointments to Node8."),
        new(
            Desktop3,
            "Desktop3 (PC2, 8-PC email lane)",
            "Desktop3",
            Policy.MitsubishiId,
            ClusterLane.P1Email,
            [ClusterJobKind.OverdueEmailClearance, ClusterJobKind.DailyOrganizerRemainder],
            "8-PC: Valdosta Mitsubishi 28546 P1 email only. Calls move to Desktop6 (Node6, provisional); SMS to Node7; appointments to Node8."),
        Nodes[2],
        Nodes[3],
        new(
            Desktop5,
            "Desktop5 (PC5 / Node5)",
            "Desktop5",
            Policy.NissanId,
            ClusterLane.P1Call,
            [ClusterJobKind.OverdueCallReschedule],
            "Provisional 8-PC overflow: Valdosta Nissan 28206 P1 overdue call reschedule. Final COS tasks wait until Glenn says the cluster is complete. Never mark a call complete if no call occurred. Enroll as Desktop5 or Node5. Do not launch until Glenn releases the hold.",
            Aliases: [Node5Template]),
        new(
            Desktop6,
            "Desktop6 (PC6 / Node6)",
            "Desktop6",
            Policy.MitsubishiId,
            ClusterLane.P1Call,
            [ClusterJobKind.OverdueCallReschedule],
            "Provisional 8-PC overflow: Valdosta Mitsubishi 28546 P1 overdue call reschedule. Final COS tasks wait until Glenn says the cluster is complete. Never mark a call complete if no call occurred. Enroll as Desktop6 or Node6. Do not launch until Glenn releases the hold.",
            Aliases: [Node6Template]),
        new(
            Node7Template,
            "Node7 (PC7, unnamed until enrolled)",
            "Node7",
            Policy.NissanId,
            ClusterLane.SmsInWindow,
            [ClusterJobKind.SmsInWindow],
            "8-PC overflow: eligible SMS 08:00–21:00 ET on 28206 and 28546. Lease is still per rooftop + customer.",
            Rooftops: [Policy.NissanId, Policy.MitsubishiId]),
        new(
            Node8Template,
            "Node8 (PC8, unnamed until enrolled)",
            "Node8",
            Policy.NissanId,
            ClusterLane.AppointmentCommit,
            [ClusterJobKind.AppointmentCommitFastPath],
            "8-PC overflow: appointment-commit fast path on both rooftops. Preempts other lanes via the customer lease.",
            Rooftops: [Policy.NissanId, Policy.MitsubishiId])
    ];

    public static IReadOnlyList<ClusterNodeSpec> OverflowNodes { get; } =
        ScaleOutNodes.Where(n => OverflowNodeIds.Contains(n.NodeId, StringComparer.OrdinalIgnoreCase)).ToArray();

    public static IReadOnlyList<ClusterNodeSpec> AllKnownNodes { get; } =
        Nodes.Concat(OverflowNodes).ToArray();

    public static bool IsOverflowNode(string? nodeId)
        => !string.IsNullOrWhiteSpace(nodeId)
           && OverflowNodeIds.Any(id => string.Equals(id, nodeId.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsScaleOut(
        IEnumerable<string>? presentNodeIds = null,
        string? clusterSizeEnv = null,
        bool modeFileScaleOut = false)
    {
        if (modeFileScaleOut)
        {
            return true;
        }

        if (int.TryParse(clusterSizeEnv?.Trim(), out var size) && size >= 8)
        {
            return true;
        }

        return presentNodeIds?.Any(IsOverflowNode) == true;
    }

    public static IReadOnlyList<ClusterNodeSpec> ActiveNodes(bool scaleOut)
        => scaleOut ? ScaleOutNodes : Nodes;

    /// <summary>
    /// Glenn standing order: install/enroll allowed; do not launch workers or
    /// CHECKIN-to-send until he says all PCs are ready. Fail-closed.
    /// TITAN_CLUSTER_HOLD_LAUNCH=off is the only override.
    /// </summary>
    public static bool HoldLaunch(HostPaths? paths)
    {
        var env = Environment.GetEnvironmentVariable("TITAN_CLUSTER_HOLD_LAUNCH")?.Trim();
        if (string.Equals(env, "off", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(env, "on", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return paths?.ReadHoldLaunch() == true;
    }

    public static ClusterNodeSpec? Find(string? nodeId)
        => MatchHint(nodeId, AllKnownNodes);

    public static ClusterNodeSpec? EffectiveSpec(string? nodeId, bool scaleOut)
        => MatchHint(nodeId, ActiveNodes(scaleOut));

    public static ClusterNodeSpec? Resolve(
        string? hostname,
        string? envNodeName,
        string? enrolledNodeId,
        bool scaleOut = false)
    {
        var catalog = ActiveNodes(scaleOut);
        foreach (var candidate in new[] { enrolledNodeId, envNodeName, hostname })
        {
            var match = MatchHint(candidate, catalog) ?? MatchHint(candidate, AllKnownNodes);
            if (match is not null)
            {
                return EffectiveSpec(match.NodeId, scaleOut) ?? match;
            }
        }

        return null;
    }

    public static bool MayWorkRooftop(ClusterNodeSpec node, string? rooftopId)
        => node.OwnsRooftop(rooftopId);

    public static bool MayWorkQueue(ClusterNodeSpec node, WorkQueue queue, DailyUpgradeProgress progress)
    {
        if (!node.AllowedRooftops.Any(Policy.IsAllowedRooftop))
        {
            return false;
        }

        return node.Lane switch
        {
            ClusterLane.P1Organizer or ClusterLane.P1Email or ClusterLane.P1Call
                or ClusterLane.SmsInWindow or ClusterLane.AppointmentCommit
                => queue is WorkQueue.NewLeads or WorkQueue.Planner or WorkQueue.Overdue,
            ClusterLane.P2P3DatabaseUpgrade =>
                queue == WorkQueue.Database
                && node.AllowedRooftops.Any(r =>
                    DailyUpgradeQueue.CanStartOnRooftop(DailyUpgradePhase.CustomerDatabaseStudy, r, progress)),
            _ => false
        };
    }

    public static bool MayWorkTaskKind(ClusterNodeSpec node, OverdueTaskKind kind)
        => node.Lane switch
        {
            ClusterLane.P1Organizer => kind is OverdueTaskKind.Email or OverdueTaskKind.Call
                or OverdueTaskKind.Sms or OverdueTaskKind.Other,
            ClusterLane.P1Email => kind is OverdueTaskKind.Email or OverdueTaskKind.Other,
            ClusterLane.P1Call => kind == OverdueTaskKind.Call,
            ClusterLane.SmsInWindow => kind == OverdueTaskKind.Sms,
            ClusterLane.AppointmentCommit => false,
            ClusterLane.P2P3DatabaseUpgrade => false,
            _ => false
        };

    public static string? LaneBlockReason(ClusterNodeSpec node, WorkQueue queue, DailyUpgradeProgress progress)
    {
        if (!node.AllowedRooftops.Any(Policy.IsAllowedRooftop))
        {
            return "Cluster node rooftop is not 28206 or 28546.";
        }

        if (node.Lane == ClusterLane.P2P3DatabaseUpgrade && queue != WorkQueue.Database)
        {
            return $"{node.DisplayName} is the P2/P3 lane. Organizer / overdue clearance belongs to the P1 node on this rooftop.";
        }

        if (node.Lane != ClusterLane.P2P3DatabaseUpgrade && queue == WorkQueue.Database)
        {
            return $"{node.DisplayName} is not the P2/P3 database lane. Database comb belongs to Node3 (Nissan) or Desktop4 / Node4 (Mitsubishi).";
        }

        if (node.Lane == ClusterLane.P2P3DatabaseUpgrade && queue == WorkQueue.Database)
        {
            return node.AllowedRooftops
                .Select(r => DailyUpgradeQueue.GateReasonOnRooftop(
                    DailyUpgradePhase.CustomerDatabaseStudy, r, progress))
                .FirstOrDefault(reason => reason is not null);
        }

        return null;
    }

    public static OverdueTaskKind GuessTaskKind(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return OverdueTaskKind.Other;
        }

        if (Contains(text, "sms", "text message", "texted"))
        {
            return OverdueTaskKind.Sms;
        }

        if (Contains(text, "call", "phone", "voicemail"))
        {
            return OverdueTaskKind.Call;
        }

        if (Contains(text, "email", "e-mail", "mail"))
        {
            return OverdueTaskKind.Email;
        }

        return OverdueTaskKind.Other;
    }

    private static bool Contains(string text, params string[] needles)
        => needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static ClusterNodeSpec? MatchHint(string? value, IEnumerable<ClusterNodeSpec> catalog)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        var folded = text.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        foreach (var node in catalog)
        {
            if (string.Equals(node.NodeId, text, StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.HostnameHint, text, StringComparison.OrdinalIgnoreCase)
                || node.KnownAliases.Any(alias =>
                    string.Equals(alias, text, StringComparison.OrdinalIgnoreCase)))
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
