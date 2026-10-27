using System.Security.Cryptography;
using System.Text;

namespace Backend.Infrastructure.Conversions;

public static class HashConversions
{
    /// <summary>
    /// Hash SHA-256 do texto (UTF-8) em hexadecimal minúsculo, com 64 caracteres.
    /// </summary>
    public static string ToSha256Hex(this string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
