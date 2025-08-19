# SS14.Labeller

Simple ASP.NET NativeAOT application for labelling our [content repository](https://github.com/space-wizards/space-station-14)

## Features

Main features of application are related to reaction to github events:
* Event of issue creation will lead to issue getting marked as ```S: Untriaged``` label
* Event of leaving review under pull request will lead to marking it with ```S: Approved``` or ```S: Awaiting Changes``` labels
* Events, related to pull request support following labels:
  * marked with ```size/XS```, ```size/S```, ```size/M```, ```size/L``` or ```size/XL``` for size (depending on a total lines changed in PR)
  * marked with ```Changes: Audio```, ```Changes: Map```, ```Changes: NoCSharp```, ```Changes: Shaders```, ```Changes: Sprites```, ```Changes: Ui``` labels based on extensions and routes of files that were affected by PR
  * marked ```Branch: Stable``` or ```Branch: Staging``` if PR is targeting specific branch
  * marked with ```S: Untriaged``` on creation
  * marked ```S: Approved``` if created by maintainer
  * can create discourse thread when PR is labelled with ```S: Undergoing Discussion``` by gh users (will post link to discussion in comment of PR)
  * marks with either ```S: Needs Review``` or ```S: Awaiting Changes``` depending on review state that maintainers leave on PR (set to ```S: Needs Review``` on opening)
  
## Usage        

Create the a file called appsettings.json like so:
```json
{
  "GitHub": {
    "WebhookSecret": "mysecret",
    "Token": "github_pat_AAAA"
  },
  "Discourse": {
    "Enable": false,
    "ApiKey": "---",
    "Username": "---",
    "DiscussionCategoryId": 0,
    "Url": "https://forum.example.com/"
  }
}
```
All of these values must be set for the application to function.

To set the port, use the `ASPNETCORE_URLS` environment variable, e.g. `ASPNETCORE_URLS=http://localhost:5000`.

### Config File Reference

#### GitHub
*WebhookSecret*: The secret you set for your webhook.\
*Token*: A GitHub PAT token.

#### Discourse
*Enable*: Whether to enable the discourse integration. If false, you can leave the rest unset.\
*ApiKey*: An API key for Discourse. Follow [these](https://meta.discourse.org/t/create-and-configure-an-api-key/230124) docs for how to get one.\
*Username*: The username to use for Discourse.\
*DiscussionCategoryId*: What category to send new discussion Topics in. You can get this by opening the Topic in your browser and the number in the URL is the category ID.\
*Url*: The Forum URL. Must end with a trailing slash.

## Building and setting up hooks

To build application for release and deployment, use the following command:

```bash
dotnet publish ./SS14.Labeller -c Release -r <platform ex. win-x64> --self-contained true /p:PublishAot=true
```

Running the application is just like any other executable. On Unix systems, you may need to set the executable bit on the binary.

For setting up the GitHub webhook, follow the instructions in the [GitHub documentation](https://docs.github.com/en/developers/webhooks-and-events/webhooks/creating-webhooks).
This app requires the `Pull request reviews`, `Pull requests` and `Issues` events to be enabled. The webhook URL should point to the URL where the application is running, e.g. `http://localhost:5000/webhook`.

The token must have the `Issues` and `Pull requests` scopes enabled for read and write access.

## Testing and debug

To run application locally you can launch run it as any other dotnet application. To set up its dependencies (database) locally you can run docker-compose:
```
docker-compose up -d
```
This will run local postgres to which labeller will try attach upon launching and when running integration tests.

### Forwarding github events for local debugging
You will need set up proxy for messages from GitHub to your local machine. For that you can use https://smee.io
1. Install Smee cli using npm (if you don't have it - follow those instructions [here](https://docs.npmjs.com/downloading-and-installing-node-js-and-npm))\
``` npm install --global smee-client ```
2. Visit https://smee.io, click 'Start a new channel' and copy the link that will be generated.
3. In your console use Smee cli to start proxy forwarding to your local machine: ``` smee -u https://smee.io/{place-you-channel-code-here} -t http://127.0.0.1:5000/webhook ```


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

### Debugging behaviour in container

To debug app behaviour in container environment you can use docker-compose-debug.yaml (it will pick latest version of labeller app from image repository):
```
docker compose -f docker-compose-debug.yml up -d
```
Or build Dockerfile yourself to try out your local code.