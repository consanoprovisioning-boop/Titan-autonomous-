using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public static class DualRooftop
{
    public static string? OtherAllowed(string? current)
    {
        if (current == Policy.NissanId)
        {
            return Policy.MitsubishiId;
        }

        if (current == Policy.MitsubishiId)
        {
            return Policy.NissanId;
        }

        return null;
    }

    public static IReadOnlyList<string> SwitchCandidates(string targetRooftop)
    {
        if (targetRooftop == Policy.NissanId)
        {
            return ["Valdosta Nissan", "28206", "Nissan"];
        }

        if (targetRooftop == Policy.MitsubishiId)
        {
            return ["Valdosta Mitsubishi", "28546", "Mitsubishi"];
        }

        return [];
    }

    public static bool ForbiddenSwitch(string candidate)
        => Policy.IsForbiddenRooftop(candidate);

    public static async Task<string?> SwitchAsync(IPage page, LearningStore learning, string current, string target)
    {
        if (!Policy.IsAllowedRooftop(target) || target == Policy.ForbiddenTkoId || ForbiddenSwitch(target))
        {
            return null;
        }

        if (current == target)
        {
            return current;
        }

        var ranked = learning.RankedSelectors(SwitchCandidates(target));
        foreach (var label in ranked)
        {
            if (ForbiddenSwitch(label))
            {
                learning.RememberSelector(label, false);
                continue;
            }

            try
            {
                var locator = page.GetByText(label, new PageGetByTextOptions { Exact = false });
                if (await locator.CountAsync() == 0)
                {
                    learning.RememberSelector(label, false);
                    continue;
                }

                await locator.First.ClickAsync();
                await page.WaitForTimeoutAsync(800);
                var body = await page.InnerTextAsync("body");
                if (Policy.IsForbiddenRooftop(body))
                {
                    learning.RememberSelector(label, false);
                    return null;
                }

                var detected = EleadsSession.DetectRooftop(body, page.Url);
                if (detected == target)
                {
                    learning.RememberSelector(label, true);
                    return detected;
                }

                learning.RememberSelector(label, false);
            }
            catch (PlaywrightException)
            {
                learning.RememberSelector(label, false);
            }
        }

        return current;
    }
}
