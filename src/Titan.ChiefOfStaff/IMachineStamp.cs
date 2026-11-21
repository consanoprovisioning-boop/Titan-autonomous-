namespace Titan.ChiefOfStaff;

public interface IMachineStamp
{
    string MachineName { get; }
    string UserName { get; }
    string MachineId { get; }
}

public sealed class SystemMachineStamp : IMachineStamp
{
    public static SystemMachineStamp Instance { get; } = new();

    public string MachineName => Environment.MachineName;
    public string UserName => Environment.UserName;
    public string MachineId => $"{Environment.MachineName}/{Environment.UserName}/{Environment.OSVersion.Platform}";
}

public sealed record FakeMachineStamp(string MachineName, string UserName, string MachineId) : IMachineStamp;
