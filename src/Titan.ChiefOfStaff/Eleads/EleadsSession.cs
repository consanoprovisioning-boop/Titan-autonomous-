using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class EleadsSession : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public IPage? Page => _page;
    public string? DetectedRooftop { get; private set; }
    public string? IdentityHint { get; private set; }
    public bool LoggedIn { get; private set; }
    public bool IdentityConfirmed { get; private set; }

    public async Task<string> AttachAsync(HostPaths paths, bool allowNavigate, bool startChromeIfNeeded)
    {
        if (!await ChromeAttach.CdpReadyAsync())
        {
            if (!startChromeIfNeeded)
            {
                throw new InvalidOperationException("Chrome CDP 9222 is not running. Run verify first and leave Chrome open.");
            }

            ChromeAttach.StartChrome(paths);
            for (var i = 0; i < 20 && !await ChromeAttach.CdpReadyAsync(); i++)
            {
                await Task.Delay(250);
            }

            if (!await ChromeAttach.CdpReadyAsync())
            {
                throw new InvalidOperationException("Chrome started but CDP 9222 did not come up.");
            }
        }

        var attached = await ChromeAttach.ConnectAsync();
        _playwright = attached.Playwright;
        _browser = attached.Browser;
        _page = attached.Page;

        if (allowNavigate && !IsEleadsUrl(_page.Url))
        {
            await _page.GotoAsync("https://www.eleadcrm.com/evo2/fresh/login.asp", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 45000
            });
        }

        await InspectAsync();
        return Describe();
    }

    public async Task InspectAsync()
    {
        if (_page is null)
        {
            LoggedIn = false;
            return;
        }

        var body = "";
        try
        {
            body = await _page.InnerTextAsync("body");
        }
        catch (PlaywrightException)
        {
            body = _page.Url;
        }

        if (Policy.IsForbiddenRooftop(body) || Policy.IsForbiddenRooftop(_page.Url))
        {
            DetectedRooftop = Policy.ForbiddenTkoId;
            LoggedIn = false;
            IdentityConfirmed = false;
            return;
        }

        DetectedRooftop = DetectRooftop(body, _page.Url);
        IdentityHint = body.Contains("Bordine", StringComparison.OrdinalIgnoreCase)
                       || body.Contains("Glenn", StringComparison.OrdinalIgnoreCase)
            ? Policy.Identity
            : null;
        IdentityConfirmed = IdentityHint is not null;
        var loginVisible = body.Contains("Username", StringComparison.OrdinalIgnoreCase)
            && body.Contains("Password", StringComparison.OrdinalIgnoreCase);
        LoggedIn = !loginVisible && Policy.IsAllowedRooftop(DetectedRooftop) && IdentityConfirmed;
    }

    public static string? DetectRooftop(string text, string url)
    {
        foreach (var id in Policy.AllowedRooftops)
        {
            if (text.Contains(id, StringComparison.Ordinal) || url.Contains(id, StringComparison.Ordinal))
            {
                return id;
            }
        }

        if (text.Contains("Valdosta Nissan", StringComparison.OrdinalIgnoreCase))
        {
            return Policy.NissanId;
        }

        if (text.Contains("Valdosta Mitsubishi", StringComparison.OrdinalIgnoreCase))
        {
            return Policy.MitsubishiId;
        }

        return null;
    }

    public static bool IsEleadsUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && url.Contains("eleadcrm.com", StringComparison.OrdinalIgnoreCase);

    public string Describe()
    {
        if (DetectedRooftop == Policy.ForbiddenTkoId)
        {
            return "FORBIDDEN rooftop 6220 — session rejected.";
        }

        if (!LoggedIn)
        {
            return "Eleads is not verified. Sign in as Bordine, Glenn on 28206 or 28546. Titan will not type the password. Leave Chrome open.";
        }

        return $"Verified rooftop {DetectedRooftop} as {IdentityHint}.";
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
