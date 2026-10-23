namespace Titan.ChiefOfStaff;

public interface IOverlay : IDisposable
{
    void Publish(OverlayState state);
    bool Closed { get; }
}

public static class OverlayHud
{
    public static IOverlay Start(HostPaths paths)
    {
        var file = new FileOverlay(paths);
        if (OperatingSystem.IsWindows())
        {
            return new NativeOverlay(paths, file);
        }

        return file;
    }
}

public sealed class FileOverlay : IOverlay
{
    private readonly HostPaths _paths;
    public bool Closed { get; private set; }

    public FileOverlay(HostPaths paths)
    {
        _paths = paths;
        Publish(OverlayState.Waiting("COS overlay file is live. Sign in as Bordine, Glenn when Chrome opens."));
    }

    public void Publish(OverlayState state)
    {
        if (Closed)
        {
            return;
        }

        state.Write(_paths);
        if (File.Exists(_paths.StopFlag))
        {
            Closed = true;
        }
    }

    public void Dispose() => Closed = true;
}
