using System.Text.Json;

namespace Titan.ChiefOfStaff;

public sealed record VerifyReceipt(
    bool GlennVerified,
    string State,
    string? Identity,
    string? Rooftop,
    string? VerifiedAt,
    bool LoginClaim,
    string? HostId,
    string? MachineName,
    string? Fingerprint);

public static class VerifyMarker
{
    public static void Write(
        HostPaths paths,
        string state,
        string? rooftop,
        string? identity,
        string? verifiedAt,
        bool glennVerified,
        HostIdentity? host = null)
    {
        if (glennVerified)
        {
            identity = Policy.Identity;
            if (host is null || string.IsNullOrWhiteSpace(host.HostId) || string.IsNullOrWhiteSpace(host.Fingerprint))
            {
                throw new InvalidOperationException("Verify on this PC requires an enrolled host identity.");
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["glennVerified"] = glennVerified,
            ["state"] = state,
            ["identity"] = identity,
            ["rooftop"] = rooftop,
            ["verifiedAt"] = verifiedAt,
            ["loginClaim"] = false,
            ["hostId"] = host?.HostId,
            ["machineName"] = host?.MachineName,
            ["fingerprint"] = host?.Fingerprint
        };

        foreach (var value in payload.Values)
        {
            if (value is string text && text.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Verify marker must not store a password.");
            }
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        if (json.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Verify marker must not store a password.");
        }

        Directory.CreateDirectory(paths.DataRoot);
        ProtectedFile.WriteUtf8(paths.VerifyMarker, json + Environment.NewLine);
    }

    public static bool TryRead(HostPaths paths, IMachineStamp stamp, out VerifyReceipt receipt)
    {
        receipt = new VerifyReceipt(false, "AwaitingVerify", null, null, null, false, null, null, null);
        if (!File.Exists(paths.VerifyMarker))
        {
            return false;
        }

        try
        {
            var json = ProtectedFile.ReadUtf8(paths.VerifyMarker);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            receipt = new VerifyReceipt(
                root.TryGetProperty("glennVerified", out var verified) && verified.GetBoolean(),
                root.TryGetProperty("state", out var state) ? state.GetString() ?? "" : "",
                root.TryGetProperty("identity", out var identity) ? identity.GetString() : null,
                root.TryGetProperty("rooftop", out var rooftop) ? rooftop.GetString() : null,
                root.TryGetProperty("verifiedAt", out var at) ? at.GetString() : null,
                root.TryGetProperty("loginClaim", out var claim) && claim.GetBoolean(),
                root.TryGetProperty("hostId", out var hostId) ? hostId.GetString() : null,
                root.TryGetProperty("machineName", out var machine) ? machine.GetString() : null,
                root.TryGetProperty("fingerprint", out var fingerprint) ? fingerprint.GetString() : null);
        }
        catch
        {
            return false;
        }

        if (!receipt.GlennVerified)
        {
            return true;
        }

        HostIdentity? host;
        try
        {
            host = HostIdentity.RequireThisMachine(paths, stamp);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return string.Equals(receipt.HostId, host.HostId, StringComparison.Ordinal)
               && string.Equals(receipt.Fingerprint, host.Fingerprint, StringComparison.Ordinal)
               && receipt.Identity == Policy.Identity
               && receipt.State == HostState.VerifiedIdle.ToString()
               && Policy.IsAllowedRooftop(receipt.Rooftop)
               && !receipt.LoginClaim;
    }
}
