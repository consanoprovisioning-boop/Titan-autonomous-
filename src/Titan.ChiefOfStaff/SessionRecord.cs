using System.Text.Json;

namespace Titan.ChiefOfStaff;

public static class SessionRecord
{
    public static void Save(HostPaths paths, HostStore store, Heartbeat beat, bool glennVerified)
    {
        store.WriteHeartbeat(beat);
        var identity = glennVerified ? Policy.Identity : null;
        var verifiedAt = glennVerified ? beat.VerifiedAt : null;
        File.WriteAllText(paths.Heartbeat, JsonSerializer.Serialize(beat));
        var markerState = glennVerified ? HostState.VerifiedIdle.ToString() : beat.State;
        VerifyMarker.Write(paths, markerState, beat.Rooftop, identity, verifiedAt, glennVerified);
    }
}
