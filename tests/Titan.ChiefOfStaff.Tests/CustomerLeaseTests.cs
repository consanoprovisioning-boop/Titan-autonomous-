using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class CustomerLeaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 10, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void Second_node_cannot_lease_the_same_customer()
    {
        var path = Path.Combine(Path.GetTempPath(), "titan-lease-" + Guid.NewGuid().ToString("n"), "cluster-leases.json");
        var store = new CustomerLeaseStore(path);

        var first = store.TryAcquire("Alienware-18", "28206", "Jane Doe 123", "Overdue", Now);
        var second = store.TryAcquire("Node3", "28206", "jane doe 123", "Database", Now);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.True(store.Release("Alienware-18", "28206", "Jane Doe 123", Now));
        Assert.NotNull(store.TryAcquire("Node3", "28206", "Jane Doe 123", "Database", Now));
    }

    [Fact]
    public void Same_node_may_renew_and_tko_never_leases()
    {
        var path = Path.Combine(Path.GetTempPath(), "titan-lease-" + Guid.NewGuid().ToString("n"), "cluster-leases.json");
        var store = new CustomerLeaseStore(path);

        var first = store.TryAcquire("Desktop3", "28546", "Sam Lee", "Overdue", Now);
        var renew = store.TryAcquire("Desktop3", "28546", "Sam Lee", "Overdue", Now.AddMinutes(1));
        Assert.NotNull(first);
        Assert.NotNull(renew);
        Assert.Null(store.TryAcquire("Desktop3", "6220", "Sam Lee", "Overdue", Now));
        Assert.Null(store.TryAcquire("Desktop3", "28546", "TKO Autogroup 6220", "Overdue", Now));
    }

    [Fact]
    public void Expired_lease_is_free()
    {
        var path = Path.Combine(Path.GetTempPath(), "titan-lease-" + Guid.NewGuid().ToString("n"), "cluster-leases.json");
        var store = new CustomerLeaseStore(path);
        Assert.NotNull(store.TryAcquire("Alienware-18", "28206", "Pat", "Overdue", Now, TimeSpan.FromMinutes(5)));
        Assert.Null(store.TryAcquire("Node3", "28206", "Pat", "Database", Now.AddMinutes(2)));
        Assert.NotNull(store.TryAcquire("Node3", "28206", "Pat", "Database", Now.AddMinutes(6)));
    }

    [Fact]
    public void Eight_nodes_cannot_lease_the_same_customer()
    {
        var path = Path.Combine(Path.GetTempPath(), "titan-lease-" + Guid.NewGuid().ToString("n"), "cluster-leases.json");
        var store = new CustomerLeaseStore(path);
        var winner = store.TryAcquire("Alienware-18", "28206", "Kim Cole", "P1Email", Now);
        Assert.NotNull(winner);

        foreach (var other in new[] { "Desktop3", "Node3", "Desktop4", "Node4", "Node5", "Node6", "Node7", "Node8" })
        {
            Assert.Null(store.TryAcquire(other, "28206", "Kim Cole", "any-job", Now));
        }

        Assert.True(store.Release("Alienware-18", "28206", "Kim Cole", Now));
        Assert.NotNull(store.TryAcquire("Node8", "28206", "Kim Cole", "AppointmentCommit", Now));
    }

    [Fact]
    public void Two_stores_on_the_same_file_are_exclusive()
    {
        var path = Path.Combine(Path.GetTempPath(), "titan-lease-" + Guid.NewGuid().ToString("n"), "cluster-leases.json");
        var left = new CustomerLeaseStore(path);
        var right = new CustomerLeaseStore(path);
        var first = left.TryAcquire("Alienware-18", "28206", "Lee Park", "P1Email", Now);
        var second = right.TryAcquire("Desktop3", "28206", "Lee Park", "P1Call", Now);
        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal("Alienware-18", right.Peek("28206", "Lee Park", Now)?.NodeId);
    }
}
