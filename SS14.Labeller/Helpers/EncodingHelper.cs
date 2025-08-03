using System.Security.Cryptography;
using System.Text;

namespace SS14.Labeller.Helpers;

public static class EncodingHelper
{
    public static string ToHmacSha256(byte[] data, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(key);
        var hashBytes = hmac.ComputeHash(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}