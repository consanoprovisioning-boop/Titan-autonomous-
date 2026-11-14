using System.Text.Json;

namespace Titan.ChiefOfStaff;

public static class VerifyMarker
{
    public static bool IsLiveVerification(Heartbeat beat)
        => beat.State == HostState.VerifiedIdle.ToString()
           && Policy.IsAllowedRooftop(beat.Rooftop)
           && !Policy.IsForbiddenRooftop(beat.Rooftop);

    public static void Publish(HostPaths paths, Heartbeat beat)
    {
        var verified = IsLiveVerification(beat);
        Directory.CreateDirectory(paths.DataRoot);
        File.WriteAllText(paths.Heartbeat, JsonSerializer.Serialize(beat));
        Write(
            paths,
            beat.State,
            beat.Rooftop,
            verified ? Policy.Identity : null,
            verified ? beat.VerifiedAt : null,
            verified);
    }

    public static void Write(
        HostPaths paths,
        string state,
        string? rooftop,
        string? identity,
        string? verifiedAt,
        bool glennVerified)
    {
        if (glennVerified)
        {
            identity = Policy.Identity;
        }

        var payload = new Dictionary<string, object?>
        {
            ["glennVerified"] = glennVerified,
            ["state"] = state,
            ["identity"] = identity,
            ["rooftop"] = rooftop,
            ["verifiedAt"] = verifiedAt,
            ["loginClaim"] = false
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
        File.WriteAllText(paths.VerifyMarker, json + Environment.NewLine);
    }
}
