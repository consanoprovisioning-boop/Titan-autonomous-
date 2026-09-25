namespace Titan.ChiefOfStaff;

public enum HostState
{
    AwaitingInstall,
    KillTripped,
    AwaitingVerify,
    VerifiedIdle,
    TickNewLeads,
    TickPlanner,
    TickOverdue,
    TickDatabase,
    BlockedForbiddenRooftop
}

public static class HostStateMachine
{
    public static HostState FromHeartbeat(KillSwitch kill, Heartbeat beat)
    {
        if (!kill.IsArmed())
        {
            return HostState.KillTripped;
        }

        if (!Enum.TryParse<HostState>(beat.State, out var state))
        {
            return HostState.AwaitingInstall;
        }

        return state;
    }

    public static HostState AfterInstall(KillSwitch kill)
    {
        kill.Arm();
        return HostState.AwaitingVerify;
    }

    public static HostState AfterVerify(string rooftopId)
    {
        if (!Policy.IsAllowedRooftop(rooftopId) || Policy.IsForbiddenRooftop(rooftopId))
        {
            return HostState.BlockedForbiddenRooftop;
        }

        return HostState.VerifiedIdle;
    }

    public static IReadOnlyList<HostState> TickOrder { get; } =
    [
        HostState.TickNewLeads,
        HostState.TickPlanner,
        HostState.TickOverdue,
        HostState.TickDatabase
    ];
}
