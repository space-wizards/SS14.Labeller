using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace SS14.Labeller.Helpers;

public static class SecurityHelper
{
    public static bool IsRequestAuthorized(
        byte[] body, 
        string gitHubSecret, 
        IHeaderDictionary headerDictionary, 
        [NotNullWhen(false)] out string? unauthorizedError
    )
    {
        unauthorizedError = null;
        if (!headerDictionary.TryGetValue("X-Hub-Signature-256", out var signatureHeader))
        {
            unauthorizedError = "Missing signature header.";
            return false;
        }

        var expectedSignature = "sha256=" + EncodingHelper.ToHmacSha256(body, gitHubSecret);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(signatureHeader!)))
        {
            unauthorizedError = "Invalid hash";
            return false;
        }

        return true;
    }
}