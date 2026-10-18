using System.Text.Json.Serialization;

namespace SS14.Labeller.GitHubApi.GitHubAppIntegration;

public class GitHubJwtPayload
{
    // Unix timestamps
    [JsonPropertyName("iat")]

    public long IssuedAt { get; set; }

    [JsonPropertyName("exp")]

    public long Expiry { get; set; }

    [JsonPropertyName("iss")]
     
    public long Issuer { get; set; }
}

public class GitHubJwtHeader
{
    [JsonPropertyName("alg")]
    public string Algorithm { get; set; } = "RS256";

    [JsonPropertyName("typ")]
    public string TokenType { get; set; } = "JWT";
}