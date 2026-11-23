using Titan.ChiefOfStaff;
using Titan.ChiefOfStaff.Eleads;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class HostSecurityTests
{
    [Theory]
    [InlineData("http://127.0.0.1:9222", true)]
    [InlineData("ws://127.0.0.1:9222/devtools/page/1", true)]
    [InlineData("http://localhost:9222", true)]
    [InlineData("http://192.168.1.20:9222", false)]
    [InlineData("ws://10.0.0.8:9222/devtools/page/1", false)]
    [InlineData("http://0.0.0.0:9222", false)]
    [InlineData("http://127.0.0.1:9333", false)]
    [InlineData("http://example.com:9222", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Cdp_is_loopback_only(string? url, bool trusted)
        => Assert.Equal(trusted, CdpEndpoint.IsTrusted(url));

    [Fact]
    public void Chrome_arguments_bind_devtools_to_loopback()
    {
        var paths = new HostPaths(Path.Combine(Path.GetTempPath(), "titan-chrome-" + Guid.NewGuid().ToString("n")));
        var args = ChromeAttach.ChromeArguments(paths);
        Assert.Contains("--remote-debugging-address=127.0.0.1", args, StringComparison.Ordinal);
        Assert.Contains("--remote-debugging-port=9222", args, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1:9222", args, StringComparison.Ordinal);
        Assert.Equal("http://127.0.0.1:9222", ChromeAttach.Cdp);
        var remote = Assert.Throws<InvalidOperationException>(() => CdpEndpoint.RefuseUntrusted("http://192.168.1.20:9222"));
        Assert.Contains("loopback-only", remote.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Customer_keys_are_sealed_and_are_not_the_visible_name()
    {
        var raw = "Jane Doe 28206 Valdosta Nissan overdue";
        var sealedKey = CustomerSeal.FromVisibleText(raw);
        Assert.Equal(16, sealedKey.Length);
        Assert.DoesNotContain("Jane", sealedKey, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Doe", sealedKey, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("28206", sealedKey, StringComparison.Ordinal);

        var root = Path.Combine(Path.GetTempPath(), "titan-seal-" + Guid.NewGuid().ToString("n"));
        try
        {
            var paths = new HostPaths(root);
            using var store = new HostStore(paths);
            store.Log("NewLeads", "hold", "72h hold", "28206", raw);
            var line = Assert.Single(store.RecentLogs(1));
            Assert.Contains(sealedKey, line, StringComparison.Ordinal);
            Assert.DoesNotContain("Jane", line, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public void Host_identity_and_verify_do_not_travel_to_another_pc()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-host-" + Guid.NewGuid().ToString("n"));
        try
        {
            var paths = new HostPaths(root);
            var pcA = new FakeMachineStamp("ALIENWARE-18", "Glenn", "machine-a");
            var pcB = new FakeMachineStamp("GLENN-LAPTOP", "Glenn", "machine-b");
            var hostA = HostIdentity.Enroll(paths, pcA);
            Assert.True(hostA.Matches(pcA, HostIdentity.LoadOrCreateSecret(paths)));
            Assert.False(hostA.Matches(pcB, HostIdentity.LoadOrCreateSecret(paths)));
            Assert.Throws<InvalidOperationException>(() => HostIdentity.RequireThisMachine(paths, pcB));
            Assert.Equal(hostA.HostId, HostIdentity.EnsureThisMachine(paths, pcA).HostId);
            Assert.Throws<InvalidOperationException>(() => HostIdentity.EnsureThisMachine(paths, pcB));

            var freshPaths = new HostPaths(Path.Combine(root, "fresh"));
            var created = HostIdentity.EnsureThisMachine(freshPaths, pcA);
            Assert.False(string.IsNullOrWhiteSpace(created.HostId));
            Assert.Equal(created.HostId, HostIdentity.EnsureThisMachine(freshPaths, pcA).HostId);

            VerifyMarker.Write(paths, "VerifiedIdle", "28206", Policy.Identity, "2026-10-18T12:00:00-04:00", true, hostA);
            Assert.True(VerifyMarker.TryRead(paths, pcA, out var receipt));
            Assert.True(receipt.GlennVerified);
            Assert.Equal(hostA.HostId, receipt.HostId);
            Assert.False(VerifyMarker.TryRead(paths, pcB, out _));

            Assert.Throws<InvalidOperationException>(
                () => new HostPaths(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Public", "stolen-titan")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public void Host_store_refuses_a_password_field()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-pwd-" + Guid.NewGuid().ToString("n"));
        try
        {
            var paths = new HostPaths(root);
            using var store = new HostStore(paths);
            Assert.Throws<InvalidOperationException>(() => store.Log("host", "verify", "password=secret", "28206"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public void Organizer_row_mentioning_password_does_not_crash_or_keep_the_session_verified()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-pwd-row-" + Guid.NewGuid().ToString("n"));
        try
        {
            var paths = new HostPaths(root);
            var stamp = new FakeMachineStamp("ALIENWARE-18", "Glenn", "machine-a");
            var host = HostIdentity.Enroll(paths, stamp);
            using var store = new HostStore(paths);
            var verified = new Heartbeat("VerifiedIdle", "28206", "2026-11-23T12:00:00-05:00", null, null, true);
            HostJournal.Record(paths, store, verified, glennVerified: true, host);

            var line = "Internet new lead — customer forgot password for the portal";
            var row = Assert.Single(WorkTick.ParseVisibleRows(line, WorkQueue.NewLeads));
            var logged = Record.Exception(() => store.Log(
                "NewLeads",
                "qualified",
                "email-only window; no send from a list row. Eligible work still needs consent, identity, and CRM history under Bordine, Glenn.",
                "28206",
                row.CustomerKey));
            Assert.Null(logged);

            var saved = Assert.Single(store.RecentLogs(1));
            Assert.DoesNotContain("password", saved, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("portal", saved, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(CustomerSeal.FromVisibleText(line), saved, StringComparison.Ordinal);

            var refusal = new InvalidOperationException("Host store must not record a password.");
            var journaled = Record.Exception(() => HostJournal.Record(
                paths,
                store,
                HostJournal.Reject(store.ReadHeartbeat(), "AwaitingVerify", refusal.Message),
                glennVerified: false));
            Assert.Null(journaled);
            Assert.True(VerifyMarker.TryRead(paths, stamp, out var receipt));
            Assert.False(receipt.GlennVerified);
            var beat = store.ReadHeartbeat();
            Assert.Equal("AwaitingVerify", beat.State);
            Assert.DoesNotContain("password", beat.LastError ?? "", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
