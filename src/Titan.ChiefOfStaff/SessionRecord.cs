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
        VerifyMarker.Write(paths, beat.State, beat.Rooftop, identity, verifiedAt, glennVerified);
    }
}
