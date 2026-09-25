using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class EleadsSession : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowserContext? _context;
    private IPage? _page;

    public IPage? Page => _page;
    public string? DetectedRooftop { get; private set; }
    public string? IdentityHint { get; private set; }
    public bool LoggedIn { get; private set; }

    public async Task<string> AttachOrOpenAsync(HostPaths paths, bool allowNavigate)
    {
        Directory.CreateDirectory(paths.BrowserProfile);
        _playwright = await Playwright.CreateAsync();
        _context = await _playwright.Chromium.LaunchPersistentContextAsync(paths.BrowserProfile, new BrowserTypeLaunchPersistentContextOptions
        {
            Channel = "chrome",
            Headless = false,
            ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
            IgnoreDefaultArgs = ["--disable-extensions"]
        });

        _page = _context.Pages.FirstOrDefault() ?? await _context.NewPageAsync();
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
            return;
        }

        DetectedRooftop = DetectRooftop(body, _page.Url);
        IdentityHint = body.Contains("Bordine", StringComparison.OrdinalIgnoreCase) ? Policy.Identity : null;
        var loginVisible = body.Contains("Username", StringComparison.OrdinalIgnoreCase)
            && body.Contains("Password", StringComparison.OrdinalIgnoreCase);
        LoggedIn = !loginVisible && Policy.IsAllowedRooftop(DetectedRooftop);
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
            return "Eleads is not verified. Glenn must sign in in the Chrome window. Titan will not type the password.";
        }

        return $"Verified rooftop {DetectedRooftop} as {IdentityHint ?? Policy.Identity}.";
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
