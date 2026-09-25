using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class WorkTick
{
    public async Task<string> RunAsync(IPage page, HostStore store, string rooftop, bool smsOpen)
    {
        if (Policy.IsForbiddenRooftop(rooftop) || !Policy.IsAllowedRooftop(rooftop))
        {
            store.Log("guard", "rooftop", "blocked", rooftop);
            return "Blocked: rooftop not 28206/28546.";
        }

        await page.BringToFrontAsync();
        var body = await page.InnerTextAsync("body");
        if (Policy.IsForbiddenRooftop(body))
        {
            store.Log("guard", "tko", "blocked", rooftop);
            return "Blocked: TKO Autogroup detected.";
        }

        var queues = new[] { "new-leads", "planner", "overdue", "database" };
        foreach (var queue in queues)
        {
            store.Log(queue, "inspect", smsOpen ? "sms+email eligible window" : "email-only window", rooftop);
        }

        return "Tick recorded. Organizer rows are processed only from a Glenn-verified Eleads page; Titan does not invent customers or send from a blank composer.";
    }
}
