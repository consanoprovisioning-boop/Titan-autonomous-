namespace Titan.ChiefOfStaff;

public sealed class KillSwitch
{
    private readonly HostPaths _paths;

    public KillSwitch(HostPaths paths)
    {
        _paths = paths;
    }

    public string Read()
    {
        if (!File.Exists(_paths.KillSwitch))
        {
            return "missing";
        }

        return File.ReadAllText(_paths.KillSwitch).Trim();
    }

    public bool IsArmed() => string.Equals(Read(), "armed", StringComparison.Ordinal);

    public void Arm()
    {
        _paths.EnsureDataTree();
        File.WriteAllText(_paths.KillSwitch, "armed" + Environment.NewLine);
    }
}
