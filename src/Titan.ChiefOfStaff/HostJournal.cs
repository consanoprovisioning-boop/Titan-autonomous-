using System.Text.Json;

namespace Titan.ChiefOfStaff;

public static class HostJournal
{
    public static void Record(HostPaths paths, HostStore store, Heartbeat beat, bool glennVerified, HostIdentity? host = null)
    {
        beat = beat with { LastError = HostStore.SafeDetail(beat.LastError) };
        store.WriteHeartbeat(beat);
        Directory.CreateDirectory(paths.DataRoot);
        ProtectedFile.WriteUtf8(paths.Heartbeat, JsonSerializer.Serialize(beat));
        VerifyMarker.Write(
            paths,
            beat.State,
            beat.Rooftop,
            glennVerified ? Policy.Identity : null,
            glennVerified ? beat.VerifiedAt : null,
            glennVerified,
            host);
    }

    public static Heartbeat Reject(Heartbeat beat, string state, string error, string? rooftop = null)
        => beat with
        {
            State = state,
            Rooftop = rooftop ?? beat.Rooftop,
            VerifiedAt = null,
            LastError = error
        };
}
