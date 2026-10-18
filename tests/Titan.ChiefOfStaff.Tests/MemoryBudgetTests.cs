using System.Text.Json;
using Titan.ChiefOfStaff.Eleads;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class MemoryBudgetTests
{
    private const long TwoGigabytes = 2L * 1024 * 1024 * 1024;
    private const long FourGigabytes = 4L * 1024 * 1024 * 1024;
    private const long EightGigabytes = 8L * 1024 * 1024 * 1024;
    private const long TwentyGigabytes = 20L * 1024 * 1024 * 1024;
    private const long ThirtyTwoGigabytes = 32L * 1024 * 1024 * 1024;

    [Fact]
    public void Chrome_launch_caps_javascript_heap_and_disk_cache()
    {
        var profile = @"C:\Titan\browser";
        var args = ChromeAttach.BuildLaunchArguments(profile);

        Assert.Contains("--remote-debugging-port=9222", args, StringComparison.Ordinal);
        Assert.Contains($"--user-data-dir=\"{profile}\"", args, StringComparison.Ordinal);
        Assert.Contains("--no-first-run", args, StringComparison.Ordinal);
        Assert.Contains("--no-default-browser-check", args, StringComparison.Ordinal);
        Assert.Contains("--disk-cache-size=1073741824", args, StringComparison.Ordinal);
        Assert.Contains("--js-flags=--max-old-space-size=4096", args, StringComparison.Ordinal);
        Assert.Equal(4096, ChromeAttach.V8OldSpaceMegabytes);
        Assert.Equal(1_073_741_824L, ChromeAttach.DiskCacheBytes);
        Assert.DoesNotContain("disable-dev-shm-usage", args, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no-sandbox", args, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("disable-gpu", args, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ignore-gpu-blocklist", args, StringComparison.OrdinalIgnoreCase);
        Assert.True(FourGigabytes <= TwentyGigabytes);
        Assert.True(TwoGigabytes + FourGigabytes + EightGigabytes <= ThirtyTwoGigabytes);
    }

    [Fact]
    public void Runtimeconfig_caps_the_host_heap_at_2gb_with_server_gc()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Titan.ChiefOfStaff.runtimeconfig.json");
        Assert.True(File.Exists(path), path);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var props = doc.RootElement.GetProperty("runtimeOptions").GetProperty("configProperties");
        Assert.True(props.GetProperty("System.GC.Server").GetBoolean());
        Assert.True(props.GetProperty("System.GC.Concurrent").GetBoolean());

        var heap = props.GetProperty("System.GC.HeapHardLimit").GetInt64();
        Assert.Equal(TwoGigabytes, heap);
        Assert.True(heap <= TwentyGigabytes);
        Assert.True(heap + FourGigabytes + EightGigabytes <= ThirtyTwoGigabytes);
    }
}
