using System.Text.RegularExpressions;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class LauncherScriptTests
{
    [Fact]
    public void Cos_cmd_is_ascii_and_does_not_read_a_password()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "deploy", "alienware-local-program");
        var cos = File.ReadAllText(Path.Combine(dir, "COS.cmd"));
        var checkin = File.ReadAllText(Path.Combine(dir, "CHECKIN.cmd"));
        var install = File.ReadAllText(Path.Combine(dir, "INSTALL.cmd"));
        var readme = File.ReadAllText(Path.Combine(dir, "README.md"));
        var start = File.ReadAllText(Path.Combine(dir, "START-HERE.txt"));

        Assert.All(cos, c => Assert.True(c < 128));
        Assert.All(checkin, c => Assert.True(c < 128));
        Assert.All(install, c => Assert.True(c < 128));
        Assert.DoesNotContain("taskkill", cos, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stop-Process", cos, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("op item", cos, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("op read", cos, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("op inject", cos, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("op://", cos, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(new Regex(@"(?m)^\s*op\s"), cos);
        Assert.Contains("cursor worker start", cos, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CHECKIN.cmd\" nopause", cos, StringComparison.Ordinal);
        Assert.Contains("state=VerifiedIdle", cos, StringComparison.Ordinal);
        Assert.Contains("onepassword://search/Eleads", cos, StringComparison.Ordinal);
        Assert.Contains("COS.lnk", cos, StringComparison.Ordinal);
        Assert.Contains("nopause", checkin, StringComparison.Ordinal);
        Assert.Contains("checkin", checkin, StringComparison.Ordinal);
        Assert.DoesNotContain(
            checkin.Split('\n'),
            line => !line.Contains("Do not type verify", StringComparison.Ordinal)
                    && line.Contains("verify", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("COS.cmd", install, StringComparison.Ordinal);
        Assert.Contains("COS.cmd", readme, StringComparison.Ordinal);
        Assert.Contains("COS.lnk", start, StringComparison.Ordinal);
        Assert.DoesNotContain("op item", readme, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Titan.ChiefOfStaff.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found from test output.");
    }
}
