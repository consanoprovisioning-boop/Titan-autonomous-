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

    public void Dispose() => _stream?.Dispose();
}
