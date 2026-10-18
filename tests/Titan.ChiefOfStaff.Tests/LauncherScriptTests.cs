using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class LauncherScriptTests
{
    private const string OnePasswordExtensionId = "aeblfdkhhhdcdjpifhhbdiojplfjncoa";

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
        Assert.DoesNotContain("onepassword://", cos, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(new Regex(@"(?m)^\s*op\s"), cos);
        Assert.Contains("cursor worker start", cos, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CHECKIN.cmd\" nopause", cos, StringComparison.Ordinal);
        Assert.Contains("state=VerifiedIdle", cos, StringComparison.Ordinal);
        Assert.Contains("Fill in Browser", cos, StringComparison.Ordinal);
        Assert.Contains("Ctrl+Backslash", cos, StringComparison.Ordinal);
        Assert.Contains(OnePasswordExtensionId, cos, StringComparison.Ordinal);
        Assert.Contains("ExtensionInstallForcelist", cos, StringComparison.Ordinal);
        Assert.Contains("COS.lnk", cos, StringComparison.Ordinal);
        Assert.Contains("nopause", checkin, StringComparison.Ordinal);
        Assert.Contains("checkin", checkin, StringComparison.Ordinal);
        Assert.DoesNotContain(
            checkin.Split('\n'),
            line => !line.Contains("Do not type verify", StringComparison.Ordinal)
                    && line.Contains("verify", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("COS.cmd", install, StringComparison.Ordinal);
        Assert.Contains("COS.cmd", readme, StringComparison.Ordinal);
        Assert.Contains("already unlocked", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ctrl+Backslash", readme, StringComparison.Ordinal);
        Assert.Contains("COS.lnk", start, StringComparison.Ordinal);
        Assert.DoesNotContain("op item", readme, StringComparison.OrdinalIgnoreCase);

        var fill = DecodeFillCommand(cos);
        Assert.Contains("[char]94", fill, StringComparison.Ordinal);
        Assert.Contains("[char]92", fill, StringComparison.Ordinal);
        Assert.Contains("remote-debugging-port=9222", fill, StringComparison.Ordinal);
        Assert.Contains("json/activate/", fill, StringComparison.Ordinal);
        Assert.DoesNotContain("op item", fill, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("op read", fill, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("op inject", fill, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("op://", fill, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(new Regex(@"(?i)password\s*="), fill);
    }

    private static string DecodeFillCommand(string cos)
    {
        var match = Regex.Match(cos, @"-EncodedCommand\s+([A-Za-z0-9+/=]+)");
        Assert.True(match.Success, "COS.cmd is missing the Fill in Browser EncodedCommand.");
        var bytes = Convert.FromBase64String(match.Groups[1].Value);
        return Encoding.Unicode.GetString(bytes);
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
