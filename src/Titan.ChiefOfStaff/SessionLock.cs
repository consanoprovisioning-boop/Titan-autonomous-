namespace Titan.ChiefOfStaff;

public sealed class SessionLock : IDisposable
{
    private readonly FileStream? _stream;
    public bool Acquired { get; }

    public SessionLock(HostPaths paths)
    {
        paths.EnsureDataTree();
        try
        {
            _stream = new FileStream(paths.SessionLock, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Acquired = true;
        }
        catch (IOException)
        {
            Acquired = false;
        }
    }

    public static SessionLock Acquire(HostPaths paths, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            var gate = new SessionLock(paths);
            if (gate.Acquired || DateTime.UtcNow >= deadline)
            {
                return gate;
            }

            gate.Dispose();
            Thread.Sleep(50);
        }
    }

    public void Dispose() => _stream?.Dispose();
}
