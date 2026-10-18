using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Titan.ChiefOfStaff;

public sealed class HostIdentity
{
    public string HostId { get; init; } = "";
    public string MachineName { get; init; } = "";
    public string UserName { get; init; } = "";
    public string MachineId { get; init; } = "";
    public string Fingerprint { get; init; } = "";
    public string EnrolledAt { get; init; } = "";

    public static HostIdentity Enroll(HostPaths paths, IMachineStamp stamp)
    {
        paths.EnsureDataTree();
        var secret = LoadOrCreateSecret(paths);
        var existing = TryLoad(paths);
        if (existing is not null && existing.Matches(stamp, secret))
        {
            return existing;
        }

        var host = Create(stamp, secret);
        Save(paths, host);
        return host;
    }

    public static HostIdentity RequireThisMachine(HostPaths paths, IMachineStamp stamp)
    {
        var host = TryLoad(paths)
            ?? throw new InvalidOperationException("This PC is not enrolled. Run INSTALL.cmd on this PC first.");
        var secret = LoadOrCreateSecret(paths);
        if (!host.Matches(stamp, secret))
        {
            throw new InvalidOperationException(
                "TitanChief on this folder belongs to another PC. Run INSTALL.cmd on this PC. Do not copy TitanChief between machines.");
        }

        return host;
    }

    public static HostIdentity? TryLoad(HostPaths paths)
    {
        if (!File.Exists(paths.HostIdentity))
        {
            return null;
        }

        var json = ProtectedFile.ReadUtf8(paths.HostIdentity);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new HostIdentity
        {
            HostId = root.GetProperty("hostId").GetString() ?? "",
            MachineName = root.GetProperty("machineName").GetString() ?? "",
            UserName = root.GetProperty("userName").GetString() ?? "",
            MachineId = root.GetProperty("machineId").GetString() ?? "",
            Fingerprint = root.GetProperty("fingerprint").GetString() ?? "",
            EnrolledAt = root.TryGetProperty("enrolledAt", out var enrolled) ? enrolled.GetString() ?? "" : ""
        };
    }

    public bool Matches(IMachineStamp stamp, byte[] secret)
    {
        if (string.IsNullOrWhiteSpace(HostId) || secret.Length == 0)
        {
            return false;
        }

        var expected = ComputeFingerprint(HostId, stamp.MachineName, stamp.UserName, stamp.MachineId, secret);
        return string.Equals(Fingerprint, expected, StringComparison.Ordinal)
               && string.Equals(MachineName, stamp.MachineName, StringComparison.OrdinalIgnoreCase)
               && string.Equals(MachineId, stamp.MachineId, StringComparison.Ordinal);
    }

    public static string ComputeFingerprint(
        string hostId,
        string machineName,
        string userName,
        string machineId,
        byte[] secret)
    {
        using var hmac = new HMACSHA256(secret);
        var material = Encoding.UTF8.GetBytes($"{hostId}\n{machineName}\n{userName}\n{machineId}");
        return Convert.ToHexString(hmac.ComputeHash(material)).ToLowerInvariant();
    }

    public static byte[] LoadOrCreateSecret(HostPaths paths)
    {
        paths.EnsureDataTree();
        if (File.Exists(paths.HostSecret))
        {
            return ProtectedFile.ReadBytes(paths.HostSecret);
        }

        var secret = RandomNumberGenerator.GetBytes(32);
        ProtectedFile.WriteBytes(paths.HostSecret, secret);
        return secret;
    }

    private static HostIdentity Create(IMachineStamp stamp, byte[] secret)
    {
        var hostId = Guid.NewGuid().ToString("n");
        return new HostIdentity
        {
            HostId = hostId,
            MachineName = stamp.MachineName,
            UserName = stamp.UserName,
            MachineId = stamp.MachineId,
            Fingerprint = ComputeFingerprint(hostId, stamp.MachineName, stamp.UserName, stamp.MachineId, secret),
            EnrolledAt = DateTimeOffset.UtcNow.ToString("o")
        };
    }

    private static void Save(HostPaths paths, HostIdentity host)
    {
        var payload = new Dictionary<string, string>
        {
            ["hostId"] = host.HostId,
            ["machineName"] = host.MachineName,
            ["userName"] = host.UserName,
            ["machineId"] = host.MachineId,
            ["fingerprint"] = host.Fingerprint,
            ["enrolledAt"] = host.EnrolledAt
        };
        ProtectedFile.WriteUtf8(
            paths.HostIdentity,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }
}
