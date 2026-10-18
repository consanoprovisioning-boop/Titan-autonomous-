using System.Text.Json;
using Titan.ChiefOfStaff;
using Titan.ChiefOfStaff.Eleads;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class CheckInVerifyTests
{
    [Fact]
    public void Later_page_or_frame_verifies_when_first_page_is_login_or_blank()
    {
        var blank = new PageSnapshot("about:blank", [new FrameSnapshot("about:blank", "")]);
        var login = new PageSnapshot(
            "https://www.eleadcrm.com/evo2/fresh/login.asp",
            [new FrameSnapshot(
                "https://www.eleadcrm.com/evo2/fresh/login.asp",
                "Username\nPassword")]);
        var app = new PageSnapshot(
            "https://www.eleadcrm.com/evo2/fresh/elead-v45/elead_track/index.aspx",
            [
                new FrameSnapshot(
                    "https://www.eleadcrm.com/evo2/fresh/elead-v45/elead_track/index.aspx",
                    "Opportunity desk"),
                new FrameSnapshot(
                    "https://www.eleadcrm.com/evo2/fresh/elead-v45/elead_track/header.aspx",
                    "Valdosta Nissan 28206\nBordine, Glenn")
            ]);
        var pages = new[] { blank, login, app };

        var firstPageOnly = EleadsInspection.Evaluate(pages[0]);
        Assert.False(firstPageOnly.LoggedIn);
        var firstEleadsPageOnly = EleadsInspection.Evaluate(pages[1]);
        Assert.False(firstEleadsPageOnly.LoggedIn);
        var mainFrameOnly = EleadsInspection.Evaluate(new PageSnapshot(app.Url, [app.Frames[0]]));
        Assert.False(mainFrameOnly.LoggedIn);

        var chosen = EleadsInspection.Choose(pages);
        Assert.True(chosen.LoggedIn);
        Assert.Equal(2, chosen.PageIndex);
        Assert.Equal("28206", chosen.DetectedRooftop);
        Assert.Equal(Policy.Identity, chosen.IdentityHint);
        Assert.Equal("Verified rooftop 28206 as Bordine, Glenn.", EleadsInspection.Describe(chosen));
        Assert.Null(EleadsOpenGuard.NavigationTarget(
            isCloudLinux: false,
            allowNavigate: true,
            urls: pages.Select(page => (string?)page.Url).ToArray()));
    }

    [Fact]
    public void Child_frame_verifies_when_main_frame_is_blank()
    {
        var framed = new PageSnapshot(
            "https://www.eleadcrm.com/evo2/fresh/elead.aspx",
            [
                new FrameSnapshot("https://www.eleadcrm.com/evo2/fresh/elead.aspx", ""),
                new FrameSnapshot(
                    "https://www.eleadcrm.com/evo2/fresh/header.aspx",
                    "Valdosta Mitsubishi\nBordine")
            ]);

        Assert.False(EleadsInspection.Evaluate(new PageSnapshot(framed.Url, [framed.Frames[0]])).LoggedIn);
        var verdict = EleadsInspection.Evaluate(framed);
        Assert.True(verdict.LoggedIn);
        Assert.Equal("28546", verdict.DetectedRooftop);
        Assert.Equal(Policy.Identity, verdict.IdentityHint);
    }

    [Fact]
    public void Login_form_and_non_eleads_page_do_not_verify()
    {
        var login = EleadsInspection.Evaluate(new PageSnapshot(
            "https://www.eleadcrm.com/evo2/fresh/login.asp",
            [new FrameSnapshot(
                "https://www.eleadcrm.com/evo2/fresh/login.asp",
                "Username\nPassword\nValdosta Nissan 28206\nGlenn")]));
        Assert.False(login.LoggedIn);

        var otherSite = EleadsInspection.Evaluate(new PageSnapshot(
            "https://example.com/notes",
            [new FrameSnapshot("https://example.com/notes", "Valdosta Nissan 28206 Bordine")]));
        Assert.False(otherSite.LoggedIn);
    }

    [Fact]
    public void Open_6220_session_rejects_even_when_another_page_shows_valdosta()
    {
        var valdosta = new PageSnapshot(
            "https://www.eleadcrm.com/evo2/fresh/elead.aspx",
            [new FrameSnapshot(
                "https://www.eleadcrm.com/evo2/fresh/elead.aspx",
                "Valdosta Nissan 28206\nBordine, Glenn")]);
        var tko = new PageSnapshot(
            "https://www.eleadcrm.com/evo2/fresh/elead.aspx?store=6220",
            [new FrameSnapshot(
                "https://www.eleadcrm.com/evo2/fresh/elead.aspx?store=6220",
                "The TKO Autogroup (6220)")]);
        var verdict = EleadsInspection.Choose([valdosta, tko]);
        Assert.False(verdict.LoggedIn);
        Assert.Equal(Policy.ForbiddenTkoId, verdict.DetectedRooftop);
        Assert.Equal("FORBIDDEN rooftop 6220 — session rejected.", EleadsInspection.Describe(verdict));
    }

    [Fact]
    public void Login_page_mentioning_6220_does_not_hide_a_valdosta_session()
    {
        var login = new PageSnapshot(
            "https://www.eleadcrm.com/evo2/fresh/login.asp",
            [new FrameSnapshot(
                "https://www.eleadcrm.com/evo2/fresh/login.asp",
                "Username\nPassword\n6220")]);
        var valdosta = new PageSnapshot(
            "https://www.eleadcrm.com/evo2/fresh/elead.aspx",
            [new FrameSnapshot(
                "https://www.eleadcrm.com/evo2/fresh/elead.aspx",
                "Valdosta Nissan 28206\nBordine")]);
        var verdict = EleadsInspection.Choose([login, valdosta]);
        Assert.True(verdict.LoggedIn);
        Assert.Equal("28206", verdict.DetectedRooftop);
    }

    [Fact]
    public void News_page_mentioning_6220_does_not_reject_valdosta()
    {
        var news = new PageSnapshot(
            "https://example.com/story",
            [new FrameSnapshot("https://example.com/story", "The TKO Autogroup (6220)")]);
        var valdosta = new PageSnapshot(
            "https://www.eleadcrm.com/app",
            [new FrameSnapshot("https://www.eleadcrm.com/app", "28206 Glenn")]);
        var verdict = EleadsInspection.Choose([news, valdosta]);
        Assert.True(verdict.LoggedIn);
        Assert.Equal("28206", verdict.DetectedRooftop);
    }

    [Fact]
    public void Login_url_opens_only_when_no_eleads_page_exists()
    {
        Assert.Equal(
            EleadsOpenGuard.LoginUrl,
            EleadsOpenGuard.NavigationTarget(false, true, new[] { "about:blank", "chrome://newtab" }));
        Assert.Null(EleadsOpenGuard.NavigationTarget(
            false,
            true,
            new[] { "about:blank", "https://www.eleadcrm.com/evo2/fresh/elead.aspx" }));
        Assert.Null(EleadsOpenGuard.NavigationTarget(true, true, new[] { "about:blank" }));
        Assert.Null(EleadsOpenGuard.NavigationTarget(false, false, new[] { "about:blank" }));
    }

    [Fact]
    public void Cdp_catalog_keeps_a_later_eleads_page()
    {
        var json = """
            [
              {"type":"page","id":"1","url":"about:blank","webSocketDebuggerUrl":"ws://127.0.0.1:9222/devtools/page/1"},
              {"type":"service_worker","id":"2","url":"chrome-extension://abc"},
              {"type":"page","id":"3","url":"https://www.eleadcrm.com/evo2/fresh/elead.aspx","webSocketDebuggerUrl":"ws://127.0.0.1:9222/devtools/page/3"}
            ]
            """;
        var pages = CdpPageCatalog.Parse(json);
        Assert.Equal(2, pages.Count);
        Assert.Equal("about:blank", pages[0].Url);
        Assert.Contains("eleadcrm.com", pages[1].Url, StringComparison.OrdinalIgnoreCase);
        Assert.False(EleadsInspection.Evaluate(new PageSnapshot(pages[0].Url!, [new FrameSnapshot(pages[0].Url!, "")])).LoggedIn);
    }

    [Fact]
    public void Verify_marker_records_receipt_without_a_password()
    {
        var root = Path.Combine(Path.GetTempPath(), "titan-verify-" + Guid.NewGuid().ToString("n"));
        try
        {
            var paths = new HostPaths(root);
            var stamp = new FakeMachineStamp("ALIENWARE-18", "Glenn", "machine-a");
            var host = HostIdentity.Enroll(paths, stamp);
            VerifyMarker.Write(paths, "VerifiedIdle", "28206", Policy.Identity, "2026-10-16T12:00:00-04:00", true, host);
            var json = File.ReadAllText(paths.VerifyMarker);
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.GetProperty("glennVerified").GetBoolean());
            Assert.Equal("VerifiedIdle", doc.RootElement.GetProperty("state").GetString());
            Assert.Equal("Bordine, Glenn", doc.RootElement.GetProperty("identity").GetString());
            Assert.Equal("28206", doc.RootElement.GetProperty("rooftop").GetString());
            Assert.False(doc.RootElement.GetProperty("loginClaim").GetBoolean());
            Assert.Equal(host.HostId, doc.RootElement.GetProperty("hostId").GetString());
            Assert.Equal(Path.Combine(root, "verify.json"), paths.VerifyMarker);
            Assert.True(VerifyMarker.TryRead(paths, stamp, out var receipt));
            Assert.True(receipt.GlennVerified);

            VerifyMarker.Write(paths, "BlockedForbiddenRooftop", "6220", null, null, false, host);
            json = File.ReadAllText(paths.VerifyMarker);
            using var rejected = JsonDocument.Parse(json);
            Assert.False(rejected.RootElement.GetProperty("glennVerified").GetBoolean());
            Assert.Equal("6220", rejected.RootElement.GetProperty("rooftop").GetString());
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
