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

    public static void StartChrome(HostPaths paths)
    {
        var chrome = FindChrome() ?? throw new InvalidOperationException("Google Chrome is not installed on this PC.");
        Directory.CreateDirectory(paths.BrowserProfile);
        Process.Start(new ProcessStartInfo
        {
            FileName = chrome,
            Arguments = $"--remote-debugging-port=9222 --user-data-dir=\"{paths.BrowserProfile}\" --no-first-run --no-default-browser-check",
            UseShellExecute = false
        });
    }

    public static async Task<(IPlaywright Playwright, IBrowser Browser, IBrowserContext Context, IPage Page)> ConnectAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.ConnectOverCDPAsync(Cdp);
        var context = browser.Contexts.FirstOrDefault() ?? throw new InvalidOperationException("Chrome CDP has no context.");
        var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
        return (playwright, browser, context, page);
    }
}
