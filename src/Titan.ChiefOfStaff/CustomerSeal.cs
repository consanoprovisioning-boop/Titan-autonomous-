using System.Security.Cryptography;
using System.Text;

namespace Titan.ChiefOfStaff;

public static class CustomerSeal
{
    public static string FromVisibleText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }
}
