using System.Text.Json;

namespace Titan.ChiefOfStaff;

public sealed record OverlayState(
    string Title,
    string State,
    string Nissan,
    string Mitsubishi,
    string LastTick,
    string Learning,
    string Instruction,
    bool Verified,
    bool Looping)
{
    public static OverlayState Waiting(string instruction)
        => new(
            "Titan COS",
            "Waiting for Eleads sign-in",
            "28206 idle",
            "28546 idle",
            "-",
            "local memory ready",
            instruction,
            false,
            false);

    public string Render()
        => $"""
            {Title}
            {State}
            Nissan 28206: {Nissan}
            Mitsubishi 28546: {Mitsubishi}
            Last tick: {LastTick}
            Learning: {Learning}
            {Instruction}
            """;

    public void Write(HostPaths paths)
    {
        paths.EnsureDataTree();
        File.WriteAllText(paths.OverlayText, Render() + Environment.NewLine);
        File.WriteAllText(paths.OverlayJson, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static OverlayState Read(HostPaths paths)
    {
        if (!File.Exists(paths.OverlayJson))
        {
            return Waiting("Double-click TitanCOS.exe and sign in as Bordine, Glenn.");
        }

        try
        {
            return JsonSerializer.Deserialize<OverlayState>(File.ReadAllText(paths.OverlayJson))
                   ?? Waiting("Overlay state missing.");
        }
        catch (JsonException)
        {
            return Waiting("Overlay state unreadable.");
        }
    }
}
