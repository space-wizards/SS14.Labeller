using Microsoft.Extensions.Options;

namespace SS14.Labeller.Configuration;

public sealed class GitHubConfigValidator : IValidateOptions<GitHubConfig>
{
    public ValidateOptionsResult Validate(string? name, GitHubConfig options)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(options.WebhookSecret))
            errors.Add("GitHub:WebhookSecret is required.");

        if (string.IsNullOrEmpty(options.Owner))
            errors.Add("GitHub:Owner is required.");

        if (string.IsNullOrEmpty(options.Repo))
            errors.Add("GitHub:Repo is required.");

        switch (options.AuthMode)
        {
            case GitHubAuthMode.Pat:
                if (string.IsNullOrEmpty(options.Token))
                    errors.Add("GitHub:Token is required when GitHub:AuthMode is 'Pat'.");

                break;
            case GitHubAuthMode.App:
                if (options.AppId <= 0)
                    errors.Add("GitHub:AppId must be set to the GitHub App ID when GitHub:AuthMode is 'App'.");

                if (string.IsNullOrEmpty(options.AppPrivateKey))
                    errors.Add("GitHub:AppPrivateKey must be set to the PEM-encoded App private key when GitHub:AuthMode is 'App'.");

                break;
            default:
                errors.Add($"GitHub:AuthMode has an unsupported value '{(int)options.AuthMode}'.");
                break;
        }

        if (errors.Count > 0)
            return ValidateOptionsResult.Fail(errors);

        return ValidateOptionsResult.Success;
    }
}