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

        Assert.Equal(Policy.NissanId, alienware.RooftopId);
        Assert.Equal(ClusterLane.P1Organizer, alienware.Lane);
        Assert.Contains(ClusterJobKind.OverdueEmailClearance, alienware.Jobs);

        Assert.Equal(Policy.MitsubishiId, desktop3.RooftopId);
        Assert.Equal(ClusterLane.P1Organizer, desktop3.Lane);
        Assert.Contains(ClusterJobKind.OverdueEmailClearance, desktop3.Jobs);

        Assert.Equal(Policy.NissanId, node3.RooftopId);
        Assert.Equal(ClusterLane.P2P3DatabaseUpgrade, node3.Lane);
        Assert.Equal(Policy.MitsubishiId, node4.RooftopId);
        Assert.Equal(ClusterLane.P2P3DatabaseUpgrade, node4.Lane);
    }

    [Fact]
    public void Hostname_hints_resolve_named_pcs()
    {
        Assert.Equal("Alienware-18", ClusterPartition.Resolve("Alienware-18", null, null)?.NodeId);
        Assert.Equal("Desktop3", ClusterPartition.Resolve("DESKTOP3", null, null)?.NodeId);
        Assert.Equal("Desktop3", ClusterPartition.Resolve(null, "Desktop3", null)?.NodeId);
        Assert.Equal("Node3", ClusterPartition.Resolve(null, null, "Node3")?.NodeId);
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
    public void Per_rooftop_gate_does_not_wait_on_the_other_store()
    {
        var nissanOnly = new DailyUpgradeProgress(["28206"], []);
        Assert.True(DailyUpgradeQueue.CanStartOnRooftop(DailyUpgradePhase.CustomerDatabaseStudy, "28206", nissanOnly));
        Assert.False(DailyUpgradeQueue.CanStartOnRooftop(DailyUpgradePhase.CustomerDatabaseStudy, "28546", nissanOnly));
        Assert.False(DailyUpgradeQueue.CanStart(DailyUpgradePhase.CustomerDatabaseStudy, nissanOnly));
        Assert.False(DailyUpgradeQueue.CanStartOnRooftop(DailyUpgradePhase.CustomerDatabaseStudy, "6220", nissanOnly));
    }
}
