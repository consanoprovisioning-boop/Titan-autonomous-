using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class EleadsSession : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly List<IPage> _pages = new();
    private IPage? _page;

    public IPage? Page => _page;
    public string? DetectedRooftop { get; private set; }
    public string? IdentityHint { get; private set; }
    public bool LoggedIn { get; private set; }
    public bool IdentityConfirmed { get; private set; }

    public async Task<string> AttachAsync(HostPaths paths, bool allowNavigate, bool startChromeIfNeeded)
    {
        EleadsOpenGuard.RefuseCloudOpen(EleadsOpenGuard.IsCloudLinuxHost, EleadsOpenGuard.LoginUrl);

        if (!await ChromeAttach.CdpReadyAsync())
        {
            if (!startChromeIfNeeded)
            {
                throw new InvalidOperationException("Chrome port 9222 is not running. Run CHECKIN.cmd first and leave that Chrome window open.");
            }

            await ChromeAttach.EnsureCdpAsync(paths);
        }

        var attached = await ChromeAttach.ConnectAsync();
        _playwright = attached.Playwright;
        _browser = attached.Browser;
        _pages.Clear();
        _pages.AddRange(attached.Pages);

        var snapshots = new List<PageSnapshot>(_pages.Count);
        foreach (var page in _pages)
        {
            snapshots.Add(await ChromeAttach.CaptureAsync(page));
        }

        await ChromeAttach.AppendUnmatchedTargetsAsync(snapshots, attached.Targets);
        var urls = SnapshotUrls(snapshots, attached.Targets);
        var target = EleadsOpenGuard.NavigationTarget(EleadsOpenGuard.IsCloudLinuxHost, allowNavigate, urls);
        if (target is not null)
        {
            if (_pages.Count == 0)
            {
                throw new InvalidOperationException("Chrome CDP has no page to open.");
            }

            if (!EleadsOpenGuard.IsEleadsUrl(_pages[0].Url))
            {
                await _pages[0].GotoAsync(target, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 45000
                });
            }

            snapshots[0] = await ChromeAttach.CaptureAsync(_pages[0]);
        }

        Apply(EleadsInspection.Choose(snapshots));
        return Describe();
    }

    public async Task InspectAsync()
    {
        var snapshots = new List<PageSnapshot>(_pages.Count);
        foreach (var page in _pages)
        {
            snapshots.Add(await ChromeAttach.CaptureAsync(page));
        }

        var targets = await CdpPageCatalog.ListAsync();
        await ChromeAttach.AppendUnmatchedTargetsAsync(snapshots, targets);
        if (snapshots.Count == 0)
        {
            LoggedIn = false;
            DetectedRooftop = null;
            IdentityHint = null;
            IdentityConfirmed = false;
            _page = null;
            return;
        }

        Apply(EleadsInspection.Choose(snapshots));
    }

    public static string? DetectRooftop(string text, string url)
    {
        var blob = text + "\n" + url;
        var nissanName = blob.Contains("Valdosta Nissan", StringComparison.OrdinalIgnoreCase);
        var mitsubishiName = blob.Contains("Valdosta Mitsubishi", StringComparison.OrdinalIgnoreCase);

        // The store name is the session. A customer zip, phone, or lead id can
        // contain the other rooftop's digits and must not switch the store.
        if (nissanName && !mitsubishiName)
        {
            return Policy.NissanId;
        }

        if (mitsubishiName && !nissanName)
        {
            return Policy.MitsubishiId;
        }

        foreach (var id in Policy.AllowedRooftops)
        {
            if (WholeRooftopId(blob, id))
            {
                return id;
            }
        }

        return null;
    }

    private static bool WholeRooftopId(string blob, string rooftopId)
        => rooftopId switch
        {
            Policy.NissanId => NissanIdToken.IsMatch(blob),
            Policy.MitsubishiId => MitsubishiIdToken.IsMatch(blob),
            _ => false
        };

    private static readonly Regex NissanIdToken = new(
        "(?<!\\d)" + Policy.NissanId + "(?!\\d)",
        RegexOptions.CultureInvariant);

    private static readonly Regex MitsubishiIdToken = new(
        "(?<!\\d)" + Policy.MitsubishiId + "(?!\\d)",
        RegexOptions.CultureInvariant);

    public static bool IsEleadsUrl(string? url) => EleadsOpenGuard.IsEleadsUrl(url);

    public string Describe()
    {
        var verdict = new EleadsVerdict(
            LoggedIn,
            DetectedRooftop,
            IdentityHint,
            IdentityConfirmed,
            false,
            DetectedRooftop == Policy.ForbiddenTkoId,
            _page?.Url,
            -1);
        return EleadsInspection.Describe(verdict);
    }

    public ValueTask DisposeAsync()
    {
        // Drop the CDP client. Do not close the Chrome window Glenn left open.
        _page = null;
        _pages.Clear();
        _browser = null;
        _playwright?.Dispose();
        _playwright = null;
        return ValueTask.CompletedTask;
    }

    private void Apply(EleadsVerdict verdict)
    {
        DetectedRooftop = verdict.DetectedRooftop;
        IdentityHint = verdict.IdentityHint;
        IdentityConfirmed = verdict.IdentityConfirmed;
        LoggedIn = verdict.LoggedIn;
        if (verdict.PageIndex >= 0 && verdict.PageIndex < _pages.Count)
        {
            _page = _pages[verdict.PageIndex];
            return;
        }

        _page = null;
        if (verdict.PageUrl is null)
        {
            return;
        }

        foreach (var page in _pages)
        {
            if (EleadsInspection.SameUrl(page.Url, verdict.PageUrl))
            {
                _page = page;
                return;
            }
        }
    }

    private static List<string?> SnapshotUrls(IReadOnlyList<PageSnapshot> snapshots, IReadOnlyList<CdpPageTarget> targets)
    {
        var urls = new List<string?>();
        foreach (var snapshot in snapshots)
        {
            urls.Add(snapshot.Url);
            foreach (var frame in snapshot.Frames)
            {
                urls.Add(frame.Url);
            }
        }

        foreach (var target in targets)
        {
            urls.Add(target.Url);
        }

        return urls;
    }
}
