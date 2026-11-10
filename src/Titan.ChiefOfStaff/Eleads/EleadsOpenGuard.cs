namespace Titan.ChiefOfStaff.Eleads;

public static class EleadsOpenGuard
{
    public const string LoginUrl = "https://www.eleadcrm.com/evo2/fresh/login.asp";

    public static bool IsCloudLinuxHost => !OperatingSystem.IsWindows();

    public static bool IsEleadsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = uri.Host;
        return host.Equals("eleadcrm.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".eleadcrm.com", StringComparison.OrdinalIgnoreCase);
    }

    public static string? NavigationTarget(bool isCloudLinux, bool allowNavigate, string? currentUrl)
        => NavigationTarget(isCloudLinux, allowNavigate, new[] { currentUrl });

    public static string? NavigationTarget(bool isCloudLinux, bool allowNavigate, IReadOnlyList<string?> urls)
    {
        if (isCloudLinux || !allowNavigate)
        {
            return null;
        }

        foreach (var url in urls)
        {
            if (IsEleadsUrl(url))
            {
                return null;
            }
        }

        return LoginUrl;
    }

    public static void RefuseCloudOpen(bool isCloudLinux, string? url)
    {
        if (!isCloudLinux)
        {
            return;
        }

        _ = url;
        throw new InvalidOperationException("This cloud Linux host refuses to open Eleads.");
    }
}
