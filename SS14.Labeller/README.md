# SS14.Labeller

Simple ASP.NET NativeAOT application for labelling our [content repository](https://github.com/space-wizards/space-station-14)

## Usage

Set the GITHUB_WEBHOOK_SECRET and GITHUB_TOKEN environment variables to your GitHub webhook secret and token respectively.

To set the port, use the `ASPNETCORE_URLS` environment variable, e.g. `ASPNETCORE_URLS=http://localhost:5000`.

To build the application, use the following command:

```bash
dotnet publish -c Release -r <platform ex. win-x64> --self-contained true /p:PublishAot=true
```

Running the application is just like any other executable. On Unix systems, you may need to set the executable bit on the binary.

For setting up the GitHub webhook, follow the instructions in the [GitHub documentation](https://docs.github.com/en/developers/webhooks-and-events/webhooks/creating-webhooks).
This app requires the `Pull request reviews`, `Pull requests` and `Issues` events to be enabled. The webhook URL should point to the URL where the application is running, e.g. `http://localhost:5000/webhook`.

The token must have the `Issues` and `Pull requests` scopes enabled for read and write access.

