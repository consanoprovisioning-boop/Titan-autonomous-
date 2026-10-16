namespace Titan.ChiefOfStaff.Eleads;

public static class EleadsOpenGuard
{
    public const string LoginUrl = "https://www.eleadcrm.com/evo2/fresh/login.asp";

    public static bool IsCloudLinuxHost => !OperatingSystem.IsWindows();

    public static bool IsEleadsUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && url.Contains("eleadcrm.com", StringComparison.OrdinalIgnoreCase);

    public static string? NavigationTarget(bool isCloudLinux, bool allowNavigate, string? currentUrl)
    {
        if (isCloudLinux || !allowNavigate || IsEleadsUrl(currentUrl))
        {
            return null;
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
