using System.Security.Cryptography;
using System.Text;

namespace Titan.ChiefOfStaff;

public static class ProtectedFile
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Titan.ChiefOfStaff.v1");

    public static void WriteUtf8(string path, string content)
    {
        if (content.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to write a password to disk.");
        }

        WriteBytes(path, Encoding.UTF8.GetBytes(content));
    }

    public static string ReadUtf8(string path)
    {
        var text = Encoding.UTF8.GetString(ReadBytes(path));
        if (text.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Protected file contained a password. File is rejected.");
        }

        return text;
    }

    public static void WriteBytes(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var payload = Protect(bytes);
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, payload);
        File.Move(tmp, path, overwrite: true);
    }

    public static byte[] ReadBytes(string path)
    {
        var payload = File.ReadAllBytes(path);
        return Unprotect(payload);
    }

    private static byte[] Protect(byte[] bytes)
    {
        if (!OperatingSystem.IsWindows())
        {
            return bytes;
        }

        return ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
    }

    private static byte[] Unprotect(byte[] bytes)
    {
        if (!OperatingSystem.IsWindows())
        {
            return bytes;
        }

        try
        {
            return ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "This Titan data cannot be read for the current Windows user. Do not copy TitanChief off this PC.",
                ex);
        }
    }
}
