using System.Diagnostics;
using Microsoft.Playwright;

namespace Titan.ChiefOfStaff.Eleads;

public static class ChromeAttach
{
    public const string Cdp = "http://127.0.0.1:9222";

    public static string? FindChrome()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var program = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string[] candidates =
        [
            Path.Combine(program, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(programX86, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(local, "Google", "Chrome", "Application", "chrome.exe")
        ];
        return candidates.FirstOrDefault(File.Exists);
    }

    public static async Task<bool> CdpReadyAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            using var response = await client.GetAsync(Cdp + "/json/version");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static int ChromeProcessCount()
        => Process.GetProcessesByName("chrome").Length;

    public static async Task EnsureCdpAsync(HostPaths paths)
    {
        if (await CdpReadyAsync())
        {
            return;
        }

        var alreadyOpen = ChromeProcessCount();
        StartChrome(paths);
        for (var i = 0; i < 40 && !await CdpReadyAsync(); i++)
        {
            await Task.Delay(500);
        }

        if (await CdpReadyAsync())
        {
            return;
        }

        if (alreadyOpen > 0)
        {
            throw new InvalidOperationException(
                "Chrome debugging port 9222 did not open. Close extra Chrome windows if needed, then double-click TitanCOS.exe again.");
        }

        throw new InvalidOperationException("Chrome did not open debugging port 9222. Install Google Chrome, then double-click TitanCOS.exe.");
    }

    public static void StartChrome(HostPaths paths)
    {
        EleadsOpenGuard.RefuseCloudOpen(EleadsOpenGuard.IsCloudLinuxHost, null);
        var chrome = FindChrome() ?? throw new InvalidOperationException("Google Chrome is not installed on this PC.");
        Directory.CreateDirectory(paths.BrowserProfile);
        Process.Start(new ProcessStartInfo
        {
            FileName = chrome,
            Arguments = $"--remote-debugging-port=9222 --user-data-dir=\"{paths.BrowserProfile}\" --no-first-run --no-default-browser-check",
            UseShellExecute = false
        });
    }

    public static async Task<CdpAttachment> ConnectAsync()
    {
        var targets = await CdpPageCatalog.ListAsync();
        var playwright = await Playwright.CreateAsync();
        try
        {
            var browser = await playwright.Chromium.ConnectOverCDPAsync(Cdp);
            var pages = new List<IPage>();
            foreach (var context in browser.Contexts)
            {
                pages.AddRange(context.Pages);
            }

            var eleadsTarget = false;
            foreach (var target in targets)
            {
                if (EleadsOpenGuard.IsEleadsUrl(target.Url))
                {
                    eleadsTarget = true;
                    break;
                }
            }

            if (pages.Count == 0 && !eleadsTarget)
            {
                var context = browser.Contexts.FirstOrDefault()
                    ?? throw new InvalidOperationException("Chrome CDP has no context.");
                pages.Add(await context.NewPageAsync());
            }

            return new CdpAttachment(playwright, browser, pages, targets);
        }
        catch
        {
            playwright.Dispose();
            throw;
        }
    }

    public static async Task<PageSnapshot> CaptureAsync(IPage page)
    {
        var frames = new List<FrameSnapshot>();
        foreach (var frame in page.Frames)
        {
            var text = "";
            try
            {
                text = await frame.Locator("body").InnerTextAsync(new LocatorInnerTextOptions { Timeout = 3000 });
            }
            catch (Exception)
            {
                text = "";
            }

            frames.Add(new FrameSnapshot(frame.Url, text));
        }

        if (frames.Count == 0)
        {
            frames.Add(new FrameSnapshot(page.Url, ""));
        }

        return new PageSnapshot(page.Url, frames);
    }

    public static async Task AppendUnmatchedTargetsAsync(List<PageSnapshot> snapshots, IReadOnlyList<CdpPageTarget> targets)
    {
        foreach (var target in targets)
        {
            if (!string.Equals(target.Type, "page", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(target.Url))
            {
                continue;
            }

            var covered = false;
            foreach (var snapshot in snapshots)
            {
                if (SnapshotCovers(snapshot, target.Url))
                {
                    covered = true;
                    break;
                }
            }

            if (covered)
            {
                continue;
            }

            var read = await CdpTargetReader.TryReadAsync(target);
            if (read is not null)
            {
                snapshots.Add(read);
                continue;
            }

            if (EleadsOpenGuard.IsEleadsUrl(target.Url))
            {
                snapshots.Add(new PageSnapshot(target.Url, [new FrameSnapshot(target.Url, "")]));
            }
        }
    }

    public static bool SnapshotCovers(PageSnapshot snapshot, string url)
    {
        if (EleadsInspection.SameUrl(snapshot.Url, url))
        {
            return true;
        }

        foreach (var frame in snapshot.Frames)
        {
            if (EleadsInspection.SameUrl(frame.Url, url))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class CdpAttachment
{
    public CdpAttachment(IPlaywright playwright, IBrowser browser, IReadOnlyList<IPage> pages, IReadOnlyList<CdpPageTarget> targets)
    {
        Playwright = playwright;
        Browser = browser;
        Pages = pages;
        Targets = targets;
    }

    public IPlaywright Playwright { get; }
    public IBrowser Browser { get; }
    public IReadOnlyList<IPage> Pages { get; }
    public IReadOnlyList<CdpPageTarget> Targets { get; }
}
