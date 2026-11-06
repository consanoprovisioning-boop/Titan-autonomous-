using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class ClusterPartitionTests
{
    [Fact]
    public void Four_named_lanes_split_rooftop_and_phase()
    {
        Assert.Equal(4, ClusterPartition.Nodes.Count);
        var alienware = ClusterPartition.Find("Alienware-18")!;
        var desktop3 = ClusterPartition.Find("Desktop3")!;
        var node3 = ClusterPartition.Find("Node3")!;
        var node4 = ClusterPartition.Find("Node4")!;
        var desktop4 = ClusterPartition.Find("Desktop4")!;

        Assert.Equal(Policy.NissanId, alienware.RooftopId);
        Assert.Equal(ClusterLane.P1Organizer, alienware.Lane);
        Assert.Contains(ClusterJobKind.OverdueEmailClearance, alienware.Jobs);

        Assert.Equal(Policy.MitsubishiId, desktop3.RooftopId);
        Assert.Equal(ClusterLane.P1Organizer, desktop3.Lane);
        Assert.Contains(ClusterJobKind.OverdueEmailClearance, desktop3.Jobs);

        Assert.Equal(Policy.NissanId, node3.RooftopId);
        Assert.Equal(ClusterLane.P2P3DatabaseUpgrade, node3.Lane);
        Assert.Equal("Desktop4", desktop4.NodeId);
        Assert.Equal("Desktop4", node4.NodeId);
        Assert.Equal(Policy.MitsubishiId, desktop4.RooftopId);
        Assert.Equal(ClusterLane.P2P3DatabaseUpgrade, desktop4.Lane);
    }

    [Fact]
    public void Hostname_hints_resolve_named_pcs()
    {
        Assert.Equal("Alienware-18", ClusterPartition.Resolve("Alienware-18", null, null)?.NodeId);
        Assert.Equal("Desktop3", ClusterPartition.Resolve("DESKTOP3", null, null)?.NodeId);
        Assert.Equal("Desktop3", ClusterPartition.Resolve(null, "Desktop3", null)?.NodeId);
        Assert.Equal("Node3", ClusterPartition.Resolve(null, null, "Node3")?.NodeId);
        Assert.Equal("Desktop4", ClusterPartition.Resolve("DESKTOP4", null, null)?.NodeId);
        Assert.Equal("Desktop4", ClusterPartition.Resolve("DeskTop4", null, null)?.NodeId);
        Assert.Equal("Desktop4", ClusterPartition.Resolve(null, "Desktop4", null)?.NodeId);
        Assert.Equal("Desktop4", ClusterPartition.Resolve(null, null, "Desktop4")?.NodeId);
        Assert.Equal("Desktop4", ClusterPartition.Resolve(null, null, "Node4")?.NodeId);
        Assert.Null(ClusterPartition.Resolve("random-laptop", null, null));
    }

    [Fact]
    public void P1_node_does_not_work_the_other_rooftop_or_database_lane()
    {
        var alienware = ClusterPartition.Find("Alienware-18")!;
        var empty = DailyUpgradeProgress.Empty;
        Assert.True(ClusterPartition.MayWorkRooftop(alienware, "28206"));
        Assert.False(ClusterPartition.MayWorkRooftop(alienware, "28546"));
        Assert.False(ClusterPartition.MayWorkRooftop(alienware, "6220"));
        Assert.True(ClusterPartition.MayWorkQueue(alienware, WorkQueue.Overdue, empty));
        Assert.False(ClusterPartition.MayWorkQueue(alienware, WorkQueue.Database, empty));
        Assert.Contains("P2/P3", ClusterPartition.LaneBlockReason(alienware, WorkQueue.Database, empty));
    }

    [Fact]
    public void P2_node_stays_closed_until_that_rooftop_p1_is_clear()
    {
        var node3 = ClusterPartition.Find("Node3")!;
        var none = DailyUpgradeProgress.Empty;
        Assert.False(ClusterPartition.MayWorkQueue(node3, WorkQueue.Database, none));
        Assert.Contains("28206", ClusterPartition.LaneBlockReason(node3, WorkQueue.Database, none));

        var nissanP1Done = new DailyUpgradeProgress(["28206"], []);
        Assert.True(ClusterPartition.MayWorkQueue(node3, WorkQueue.Database, nissanP1Done));
        Assert.False(ClusterPartition.MayWorkQueue(node3, WorkQueue.Overdue, nissanP1Done));
    }

    [Fact]
    public void Desktop4_mitsubishi_p2_stays_closed_until_desktop3_p1_is_clear()
    {
        var desktop4 = ClusterPartition.Find("Desktop4")!;
        var none = DailyUpgradeProgress.Empty;
        Assert.True(ClusterPartition.MayWorkRooftop(desktop4, "28546"));
        Assert.False(ClusterPartition.MayWorkRooftop(desktop4, "28206"));
        Assert.False(ClusterPartition.MayWorkRooftop(desktop4, "6220"));
        Assert.False(ClusterPartition.MayWorkQueue(desktop4, WorkQueue.Database, none));
        Assert.Contains("28546", ClusterPartition.LaneBlockReason(desktop4, WorkQueue.Database, none)!);

        var mitsubishiP1Done = new DailyUpgradeProgress(["28546"], []);
        Assert.True(ClusterPartition.MayWorkQueue(desktop4, WorkQueue.Database, mitsubishiP1Done));
        Assert.False(ClusterPartition.MayWorkQueue(desktop4, WorkQueue.Overdue, mitsubishiP1Done));
        Assert.False(ClusterPartition.MayWorkQueue(ClusterPartition.Find("Desktop3")!, WorkQueue.Database, mitsubishiP1Done));
    }

    [Fact]
    public void Per_rooftop_gate_does_not_wait_on_the_other_store()
    {
        var nissanOnly = new DailyUpgradeProgress(["28206"], []);
        Assert.True(DailyUpgradeQueue.CanStartOnRooftop(DailyUpgradePhase.CustomerDatabaseStudy, "28206", nissanOnly));
        Assert.False(DailyUpgradeQueue.CanStartOnRooftop(DailyUpgradePhase.CustomerDatabaseStudy, "28546", nissanOnly));
        Assert.False(DailyUpgradeQueue.CanStart(DailyUpgradePhase.CustomerDatabaseStudy, nissanOnly));
        Assert.False(DailyUpgradeQueue.CanStartOnRooftop(DailyUpgradePhase.CustomerDatabaseStudy, "6220", nissanOnly));
    }

    [Fact]
    public void Go_live_stays_four_nodes_until_scale_out()
    {
        Assert.Equal(4, ClusterPartition.Nodes.Count);
        Assert.Equal(8, ClusterPartition.ScaleOutNodes.Count);
        Assert.Equal(4, ClusterPartition.OverflowNodes.Count);
        Assert.False(ClusterPartition.IsScaleOut(presentNodeIds: ["Alienware-18", "Desktop3", "Node3", "Node4"]));
        Assert.False(ClusterPartition.IsScaleOut(presentNodeIds: ["Alienware-18", "Desktop3", "Node3", "Desktop4"]));
        Assert.True(ClusterPartition.IsScaleOut(presentNodeIds: ["Node5"]));
        Assert.True(ClusterPartition.IsScaleOut(clusterSizeEnv: "8"));
        Assert.True(ClusterPartition.IsScaleOut(modeFileScaleOut: true));
        Assert.Equal(ClusterLane.P1Organizer, ClusterPartition.EffectiveSpec("Alienware-18", scaleOut: false)!.Lane);
        Assert.Equal(ClusterLane.P1Email, ClusterPartition.EffectiveSpec("Alienware-18", scaleOut: true)!.Lane);
        Assert.Equal(ClusterLane.P2P3DatabaseUpgrade, ClusterPartition.EffectiveSpec("Node3", scaleOut: true)!.Lane);
    }

    [Fact]
    public void Eight_node_overflow_lanes_do_not_share_a_send_job()
    {
        var eight = ClusterPartition.ScaleOutNodes;
        Assert.Equal("Node5", eight[4].NodeId);
        Assert.Equal(ClusterLane.P1Call, eight[4].Lane);
        Assert.Equal(Policy.NissanId, eight[4].RooftopId);
        Assert.Equal(ClusterLane.P1Call, eight[5].Lane);
        Assert.Equal(Policy.MitsubishiId, eight[5].RooftopId);
        Assert.Equal(ClusterLane.SmsInWindow, eight[6].Lane);
        Assert.True(eight[6].OwnsRooftop("28206"));
        Assert.True(eight[6].OwnsRooftop("28546"));
        Assert.Equal(ClusterLane.AppointmentCommit, eight[7].Lane);
        Assert.False(eight[6].OwnsRooftop("6220"));

        var nissanEmail = eight[0];
        var nissanCall = eight[4];
        Assert.True(ClusterPartition.MayWorkTaskKind(nissanEmail, OverdueTaskKind.Email));
        Assert.False(ClusterPartition.MayWorkTaskKind(nissanEmail, OverdueTaskKind.Call));
        Assert.True(ClusterPartition.MayWorkTaskKind(nissanCall, OverdueTaskKind.Call));
        Assert.False(ClusterPartition.MayWorkTaskKind(nissanCall, OverdueTaskKind.Email));
        Assert.False(ClusterPartition.MayWorkTaskKind(eight[7], OverdueTaskKind.Email));
        Assert.Equal(OverdueTaskKind.Call, ClusterPartition.GuessTaskKind("Overdue phone call"));
        Assert.Equal(OverdueTaskKind.Sms, ClusterPartition.GuessTaskKind("SMS follow-up"));
    }

    [Fact]
    public void Resolve_finds_overflow_nodes_without_dropping_go_live()
    {
        Assert.Equal("Node5", ClusterPartition.Resolve(null, null, "Node5")?.NodeId);
        Assert.Equal(ClusterLane.P1Organizer, ClusterPartition.Resolve("Alienware-18", null, null, scaleOut: false)!.Lane);
        Assert.Equal(ClusterLane.P1Email, ClusterPartition.Resolve("Alienware-18", null, null, scaleOut: true)!.Lane);
        Assert.Equal("Desktop4", ClusterPartition.Resolve(null, null, "Node4", scaleOut: true)?.NodeId);
        Assert.Equal(ClusterLane.P2P3DatabaseUpgrade, ClusterPartition.EffectiveSpec("Desktop4", scaleOut: true)!.Lane);
    }
}
