using System.Text.Json;

namespace Titan.ChiefOfStaff;

/// <summary>
/// Coordinator lock so two cluster nodes do not send to the same customer.
/// Primary split is rooftop + lane. This lease is the second fence:
/// local file, or TITAN_LEASE_DIR on Alienware-18 (the MAIN lease host).
/// Do not host the shared lease folder on Desktop4.
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

        using var fileLock = TryLock();
        if (fileLock is null)
        {
            return null;
        }

        lock (_gate)
        {
            var live = Load(nowEt);
            if (live is null)
            {
                return null;
            }

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
        using var fileLock = TryLock();
        if (fileLock is null)
        {
            return false;
        }

        lock (_gate)
        {
            var live = Load(nowEt);
            if (live is null || !live.TryGetValue(fingerprint, out var held))
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

        using var fileLock = TryLock();
        if (fileLock is null)
        {
            return null;
        }

        lock (_gate)
        {
            var live = Load(nowEt);
            if (live is null)
            {
                return null;
            }

            live.TryGetValue(CustomerLease.FingerprintOf(rooftopId!, customerKey), out var held);
            return held;
        }
    }

    public IReadOnlyList<CustomerLease> Snapshot(DateTimeOffset nowEt)
    {
        using var fileLock = TryLock();
        if (fileLock is null)
        {
            return [];
        }

        lock (_gate)
        {
            var live = Load(nowEt);
            if (live is null)
            {
                return [];
            }

            return live.Values.OrderBy(v => v.Fingerprint).ToArray();
        }
    }

    private FileStream? TryLock()
    {
        var lockPath = _path + ".lock";
        var dir = Path.GetDirectoryName(lockPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        for (var attempt = 0; attempt < 40; attempt++)
        {
            try
            {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < 39)
            {
                Thread.Sleep(25);
            }
            catch (IOException)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Missing file means there are no holders. Unreadable JSON does not:
    /// a torn write is not an empty set, and saving over it would drop every
    /// other node's lease so a second PC could message the same customer.
    /// </summary>
    private Dictionary<string, CustomerLease>? Load(DateTimeOffset nowEt)
    {
        if (!File.Exists(_path))
        {
            return new Dictionary<string, CustomerLease>(StringComparer.Ordinal);
        }

        string json;
        try
        {
            json = File.ReadAllText(_path);
        }
        catch (IOException)
        {
            return null;
        }

        try
        {
            var rows = JsonSerializer.Deserialize<List<CustomerLease>>(json);
            if (rows is null)
            {
                return null;
            }

            var live = new Dictionary<string, CustomerLease>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (row is null
                    || string.IsNullOrWhiteSpace(row.NodeId)
                    || string.IsNullOrWhiteSpace(row.CustomerKey)
                    || !Policy.IsAllowedRooftop(row.RooftopId)
                    || row.ExpiresAtEt <= nowEt)
                {
                    continue;
                }

                live[row.Fingerprint] = row;
            }

            return live;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void Save(Dictionary<string, CustomerLease> live)
    {
        var json = JsonSerializer.Serialize(live.Values.ToArray(), new JsonSerializerOptions { WriteIndented = true });
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, json + Environment.NewLine);
        File.Move(temporary, _path, overwrite: true);
    }
}
