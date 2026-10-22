namespace Titan.ChiefOfStaff;

public enum AutoLaunchStep
{
    StartLoop,
    RunCheckin,
    GiveUp
}

/// <summary>One-double-click launch decision: if Verify already exists go
/// straight to the live loop, otherwise keep re-checking (Glenn is signing
/// in in Chrome) until the wait budget runs out.</summary>
public static class AutoLaunch
{
    public static readonly TimeSpan DefaultWait = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan DefaultPoll = TimeSpan.FromSeconds(5);

    public static AutoLaunchStep Decide(bool verified, TimeSpan waited, TimeSpan waitBudget)
    {
        if (verified)
        {
            return AutoLaunchStep.StartLoop;
        }

        return waited >= waitBudget ? AutoLaunchStep.GiveUp : AutoLaunchStep.RunCheckin;
    }
}
