# SS14.Labeller

Simple ASP.NET NativeAOT application for labelling our [content repository](https://github.com/space-wizards/space-station-14)

## Usage

Create the a file called appsettings.json like so:
```json
{
  "GitHub": {
    "WebhookSecret": "mysecret",
    "Token": "github_pat_AAAA"
  },
  "Discourse": {
    "ApiKey": "---",
    "Username": "---",
    "DiscussionCategoryId": 0,
    "Url": "https://forum.example.com/"
  }
}
```
All of these values must be set for the application to function.

To set the port, use the `ASPNETCORE_URLS` environment variable, e.g. `ASPNETCORE_URLS=http://localhost:5000`.


To build the application, use the following command:

```bash
dotnet publish ./SS14.Labeller -c Release -r <platform ex. win-x64> --self-contained true /p:PublishAot=true
```

Running the application is just like any other executable. On Unix systems, you may need to set the executable bit on the binary.

For setting up the GitHub webhook, follow the instructions in the [GitHub documentation](https://docs.github.com/en/developers/webhooks-and-events/webhooks/creating-webhooks).
This app requires the `Pull request reviews`, `Pull requests` and `Issues` events to be enabled. The webhook URL should point to the URL where the application is running, e.g. `http://localhost:5000/webhook`.

The token must have the `Issues` and `Pull requests` scopes enabled for read and write access.

## Testing and debug

You will need set up proxy for messages from GitHub to your local machine. For that you can use https://smee.io
1. Install Smee cli using npm (if you don't have it - follow those instructions [here](https://docs.npmjs.com/downloading-and-installing-node-js-and-npm))\
``` npm install --global smee-client ```
2. Visit https://smee.io, click 'Start a new channel' and copy the link that will be generated.
3. In your console use Smee cli to start proxy forwarding to your local machine\
```smee -u https://smee.io/{place-you-channel-code-here} -t http://127.0.0.1:5000/webhook```

Upon launching, it will output line like
```
Forwarding https://smee.io/{place-you-channel-code-here} to http://127.0.0.1:5000/webhook
```
That means that every message it receives, including
* Its payload
* Its headers

will be proxied to `http://127.0.0.1:5000/webhooks`.

The labeller should already automatically start on port 5000, but if it doesn't you can use this launchProfile.json
```
{
  "profiles": {
    "SS14.Labeller": {
      "commandName": "Project",
      "launchBrowser": false,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "http://localhost:5000"
    }
  }
}
```

Now we need set up the repository. Create new repository or use an existing one. 
1. Go to the 'Settings' tab of repository, then to 'Webhooks' (`https://github.com/{owner}/{repository}/settings/hooks`)
2. Click 'Add webhook'
3. Copy your smee.io url into Payload URL field
4. Select content-type ```application/json```
5. Input any "secret" word or phrase into the Secret field.
6. In the block 'Which events would you like to trigger this webhook?' select 'Let me select individual events' and check the events as listed in the Usage section.