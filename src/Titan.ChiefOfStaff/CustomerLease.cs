using System.Text.Json;

namespace Titan.ChiefOfStaff;

/// <summary>
/// Coordinator lock so two cluster nodes do not send to the same customer.
/// Primary split is rooftop + lane. This lease is the second fence:
/// local file, or TITAN_LEASE_DIR if Glenn points it at a shared folder.
/// Cloud Linux never claims a send from a lease. CRM history remains proof.
/// </summary>
public sealed record CustomerLease(
    string RooftopId,
    string CustomerKey,
    string NodeId,
    DateTimeOffset ExpiresAtEt,
    string Job)
{
    public string Fingerprint => FingerprintOf(RooftopId, CustomerKey);

    public static string FingerprintOf(string rooftopId, string customerKey)
        => $"{rooftopId.Trim()}|{NormalizeKey(customerKey)}";

    public static string NormalizeKey(string customerKey)
        => string.Join(' ', customerKey.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToUpperInvariant();
}

public sealed class CustomerLeaseStore
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    private readonly string _path;
    private readonly object _gate = new();

    public CustomerLeaseStore(string leaseFilePath)
    {
        _path = leaseFilePath;
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public static string ResolveLeasePath(HostPaths paths)
    {
        var shared = Environment.GetEnvironmentVariable("TITAN_LEASE_DIR");
        if (!string.IsNullOrWhiteSpace(shared))
        {
            return Path.Combine(shared.Trim(), "cluster-leases.json");
        }

        return paths.ClusterLeases;
    }

    public CustomerLease? TryAcquire(
        string nodeId,
        string? rooftopId,
        string customerKey,
        string job,
        DateTimeOffset nowEt,
        TimeSpan? ttl = null)
    {
        if (!Policy.IsAllowedRooftop(rooftopId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(customerKey) || Policy.IsForbiddenRooftop(customerKey))
        {
            return null;
        }

        var lease = new CustomerLease(
            rooftopId!.Trim(),
            CustomerLease.NormalizeKey(customerKey),
            nodeId,
            nowEt.Add(ttl ?? DefaultTtl),
            job);

        lock (_gate)
        {
            var live = Load(nowEt);
            if (live.TryGetValue(lease.Fingerprint, out var held)
                && !string.Equals(held.NodeId, nodeId, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            live[lease.Fingerprint] = lease;
            Save(live);
            return lease;
        }
    }

    public bool Release(string nodeId, string? rooftopId, string customerKey, DateTimeOffset nowEt)
    {
        if (!Policy.IsAllowedRooftop(rooftopId))
        {
            return false;
        }

        var fingerprint = CustomerLease.FingerprintOf(rooftopId!, customerKey);
        lock (_gate)
        {
            var live = Load(nowEt);
            if (!live.TryGetValue(fingerprint, out var held))
            {
                return false;
            }

            if (!string.Equals(held.NodeId, nodeId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            live.Remove(fingerprint);
            Save(live);
            return true;
        }
    }

    public CustomerLease? Peek(string? rooftopId, string customerKey, DateTimeOffset nowEt)
    {
        if (!Policy.IsAllowedRooftop(rooftopId))
        {
            return null;
        }

        lock (_gate)
        {
            Load(nowEt).TryGetValue(CustomerLease.FingerprintOf(rooftopId!, customerKey), out var held);
            return held;
        }
    }

    public IReadOnlyList<CustomerLease> Snapshot(DateTimeOffset nowEt)
    {
        lock (_gate)
        {
            return Load(nowEt).Values.OrderBy(v => v.Fingerprint).ToArray();
        }
    }

    private Dictionary<string, CustomerLease> Load(DateTimeOffset nowEt)
    {
        var live = new Dictionary<string, CustomerLease>(StringComparer.Ordinal);
        if (!File.Exists(_path))
        {
            return live;
        }

        try
        {
            var rows = JsonSerializer.Deserialize<List<CustomerLease>>(File.ReadAllText(_path)) ?? [];
            foreach (var row in rows)
            {
                if (!Policy.IsAllowedRooftop(row.RooftopId) || row.ExpiresAtEt <= nowEt)
                {
                    continue;
                }

                live[row.Fingerprint] = row;
            }
        }
        catch (JsonException)
        {
            return live;
        }

        return live;
    }

    private void Save(Dictionary<string, CustomerLease> live)
    {
        var json = JsonSerializer.Serialize(live.Values.ToArray(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json + Environment.NewLine);
    }
}
