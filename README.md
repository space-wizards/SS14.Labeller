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

## Testing and debug

You will need set up proxy for messages from github to your local machine. For that you can use https://smee.io
You can use 'Use the CLI' version of proxy:
1. Install smee cli using npm (if you dont have it - follow those instructions here https://docs.npmjs.com/downloading-and-installing-node-js-and-npm)
``` npm install --global smee-client ```
2. visit https://smee.io, click 'Start a new channel' and copy link that will be generated on top.
3. In console use smee cli to start proxy forwarding to your local machine```smee -u https://smee.io/{place-you-channel-code-here} -P //webhook```

Upon launching it will output line like
```
Forwarding https://smee.io/5999VPv39Kc69sxj to http://127.0.0.1:3000/webhooks
```
That means that every message it receives, including
* its payload
* its headers

will be proxied to http://127.0.0.1:3000/webhooks, so you need to configure your debug launch to use that port. To do that you can set launchSettings.json to following:
```
{
  "profiles": {
    "SS14.Labeller": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "http://localhost:3000"
    }
  }
}
```

Now we need set up repository. Create new repository or use existing one. 
1. Go to 'Settings' tab of repository, then to 'Webhooks' (https://github.com/{owner}/{repository}/settings/hooks)
2. click 'Add webhook' option
3. Input url that smee.io gave your on previous step (should look like https://smee.io/5999VPv39Kc69sxj) into Payload URL
4. select content-type ```application/json```
5. Input secret word into Secret
6. In block 'which events would you like to trigger this webhook' select 'Let me select individual events' and check only event types you need to debug (currently supported are Issue/Pull Request/ Pull Request Review)
7. Set env variables GITHUB_WEBHOOK_SECRET using secret you passed in step 5, and GITHUB_TOKEN using PAT token for interaction (can be created in profile https://github.com/settings/personal-access-tokens)
Now you are all set up to try and create issure/ PR and get some events debugging!