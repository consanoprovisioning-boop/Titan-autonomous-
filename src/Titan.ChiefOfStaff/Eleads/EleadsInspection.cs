namespace Titan.ChiefOfStaff.Eleads;

public sealed record FrameSnapshot(string Url, string Text);

public sealed record PageSnapshot(string Url, IReadOnlyList<FrameSnapshot> Frames);

public sealed record EleadsVerdict(
    bool LoggedIn,
    string? DetectedRooftop,
    string? IdentityHint,
    bool IdentityConfirmed,
    bool LoginFormVisible,
    bool Forbidden,
    string? PageUrl,
    int PageIndex);

public static class EleadsInspection
{
    public static EleadsVerdict Choose(IReadOnlyList<PageSnapshot> pages)
    {
        if (pages.Count == 0)
        {
            return Empty();
        }

        var evaluations = new List<EleadsVerdict>(pages.Count);
        for (var i = 0; i < pages.Count; i++)
        {
            evaluations.Add(Evaluate(pages[i], i));
        }

        foreach (var verdict in evaluations)
        {
            if (IsOpenForbiddenSession(pages, verdict))
            {
                return verdict;
            }
        }

        foreach (var verdict in evaluations)
        {
            if (verdict.LoggedIn)
            {
                return verdict;
            }
        }

        foreach (var verdict in evaluations)
        {
            if (verdict.Forbidden && verdict.PageIndex >= 0 && TouchesEleads(pages[verdict.PageIndex]))
            {
                return verdict;
            }
        }

        foreach (var verdict in evaluations)
        {
            if (verdict.PageIndex >= 0 && TouchesEleads(pages[verdict.PageIndex]))
            {
                return verdict;
            }
        }

        return evaluations[0];
    }

    public static EleadsVerdict Evaluate(PageSnapshot page, int pageIndex = 0)
    {
        var text = CombinedText(page);
        var urls = CombinedUrls(page);
        var loginVisible = ShowsLoginForm(page);
        var forbidden = Policy.IsForbiddenRooftop(text) || Policy.IsForbiddenRooftop(urls);
        if (forbidden)
        {
            return new EleadsVerdict(
                false,
                Policy.ForbiddenTkoId,
                null,
                false,
                loginVisible,
                true,
                page.Url,
                pageIndex);
        }

        var rooftop = EleadsSession.DetectRooftop(text, urls);
        var identityConfirmed = text.Contains("Bordine", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Glenn", StringComparison.OrdinalIgnoreCase);
        var loggedIn = TouchesEleads(page)
            && !loginVisible
            && Policy.IsAllowedRooftop(rooftop)
            && identityConfirmed;
        return new EleadsVerdict(
            loggedIn,
            rooftop,
            identityConfirmed ? Policy.Identity : null,
            identityConfirmed,
            loginVisible,
            false,
            page.Url,
            pageIndex);
    }

    public static bool TouchesEleads(PageSnapshot page)
    {
        if (EleadsOpenGuard.IsEleadsUrl(page.Url))
        {
            return true;
        }

        foreach (var frame in page.Frames)
        {
            if (EleadsOpenGuard.IsEleadsUrl(frame.Url))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShowsLoginForm(PageSnapshot page)
    {
        var text = CombinedText(page);
        if (!HasCredentialLabels(text))
        {
            return false;
        }

        // login.asp is the sign-in form even when it also names the store.
        if (HasLoginUrl(page))
        {
            return true;
        }

        // A lead note that says "username and password" is not that form.
        // An opportunity or the Daily Organizer is the signed-in shell.
        return !LooksLikeSignedInShell(text);
    }

    private static bool HasCredentialLabels(string text)
        => text.Contains("Username", StringComparison.OrdinalIgnoreCase)
           && text.Contains("Password", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeSignedInShell(string text)
        => text.Contains("opportunity", StringComparison.OrdinalIgnoreCase)
           || text.Contains("daily organizer", StringComparison.OrdinalIgnoreCase);

    private static bool HasLoginUrl(PageSnapshot page)
    {
        if (UrlIsLogin(page.Url))
        {
            return true;
        }

        foreach (var frame in page.Frames)
        {
            if (UrlIsLogin(frame.Url))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UrlIsLogin(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !EleadsOpenGuard.IsEleadsUrl(url))
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
               && uri.AbsolutePath.Contains("login", StringComparison.OrdinalIgnoreCase);
    }

    public static bool SameUrl(string? left, string? right)
    {
        static string Norm(string? value) => (value ?? "").Trim().TrimEnd('/');
        return string.Equals(Norm(left), Norm(right), StringComparison.OrdinalIgnoreCase);
    }

    public static string Describe(EleadsVerdict verdict)
    {
        if (verdict.Forbidden || verdict.DetectedRooftop == Policy.ForbiddenTkoId)
        {
            return "FORBIDDEN rooftop 6220 — session rejected.";
        }

        if (!verdict.LoggedIn)
        {
            return "Eleads is not verified. Sign in as Bordine, Glenn on 28206 or 28546. Titan will not type credentials. Leave Chrome open.";
        }

        return $"Verified rooftop {verdict.DetectedRooftop} as {verdict.IdentityHint}.";
    }

    public static string CombinedText(PageSnapshot page)
    {
        var parts = new string[page.Frames.Count];
        for (var i = 0; i < page.Frames.Count; i++)
        {
            parts[i] = page.Frames[i].Text;
        }

        return string.Join("\n", parts);
    }

    private static string CombinedUrls(PageSnapshot page)
    {
        var parts = new List<string>(page.Frames.Count + 1) { page.Url };
        foreach (var frame in page.Frames)
        {
            parts.Add(frame.Url);
        }

        return string.Join("\n", parts);
    }

    private static bool IsOpenForbiddenSession(IReadOnlyList<PageSnapshot> pages, EleadsVerdict verdict)
        => verdict.Forbidden
           && !verdict.LoginFormVisible
           && verdict.PageIndex >= 0
           && verdict.PageIndex < pages.Count
           && TouchesEleads(pages[verdict.PageIndex]);

    private static EleadsVerdict Empty()
        => new(false, null, null, false, false, false, null, -1);
}
