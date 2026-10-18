using System.Net;

namespace Titan.ChiefOfStaff;

public static class CdpEndpoint
{
    public const int Port = 9222;
    public const string LoopbackHttp = "http://127.0.0.1:9222";
    public const string LoopbackOrigin = "http://127.0.0.1:9222";

    public static bool IsTrusted(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not "http" and not "https" and not "ws" and not "wss")
        {
            return false;
        }

        if (uri.Port != Port)
        {
            return false;
        }

        return IsLoopbackHost(uri.Host);
    }

    public static bool IsLoopbackHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (string.Equals(host, "127.0.0.1", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip);
    }

    public static void RefuseUntrusted(string? url)
    {
        if (!IsTrusted(url))
        {
            throw new InvalidOperationException(
                "Chrome debugging is loopback-only on this PC. Titan will not attach to a remote DevTools port.");
        }
    }
}
