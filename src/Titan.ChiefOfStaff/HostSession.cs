namespace Titan.ChiefOfStaff;

public static class HostSession
{
    public static int Stop(HostPaths paths, TimeSpan wait)
    {
        using var sessionLock = SessionLock.Acquire(paths, wait);
        if (!sessionLock.Acquired)
        {
            return 4;
        }

        using var store = new HostStore(paths);
        var beat = store.ReadHeartbeat();
        HostJournal.Record(
            paths,
            store,
            HostJournal.Reject(beat, HostState.AwaitingVerify.ToString(), "stopped"),
            glennVerified: false);
        store.Log("host", "stop", "host stopped; session not closed by typing credentials");
        return 0;
    }
}
