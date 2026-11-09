namespace Titan.ChiefOfStaff;

public readonly record struct TickAdmission(bool ClearVerifiedSession, HostState State, int ExitCode, string Error)
{
    public static TickAdmission Ready { get; } = new(false, HostState.VerifiedIdle, 0, "");

    public static TickAdmission Inspect(bool loggedIn, bool pageBound, string? rooftop)
    {
        if (!loggedIn)
        {
            return new TickAdmission(
                true,
                HostState.AwaitingSignIn,
                6,
                "Eleads session is not verified. Titan will not type the password.");
        }

        // A CDP target can prove the login when Playwright did not return that tab.
        // Missing a page handle is not proof the session ended, so Verify must stay.
        if (!pageBound)
        {
            return new TickAdmission(
                false,
                HostState.VerifiedIdle,
                6,
                "Eleads is logged in, but Titan could not bind that Chrome tab. Leave Chrome open and run the tick again. No send was made.");
        }

        if (rooftop == Policy.ForbiddenTkoId || !Policy.IsAllowedRooftop(rooftop))
        {
            return new TickAdmission(
                true,
                HostState.BlockedForbiddenRooftop,
                7,
                "Forbidden rooftop. TKO 6220 is blocked.");
        }

        return Ready;
    }

    public static TickAdmission AfterWork(string result)
    {
        if (result.StartsWith("Blocked:", StringComparison.Ordinal))
        {
            return new TickAdmission(true, HostState.BlockedForbiddenRooftop, 7, result);
        }

        return Ready;
    }
}
