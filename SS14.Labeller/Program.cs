using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.Endpoints;
using SS14.Labeller.Handlers;
using SS14.Labeller.Middlewares;
using SS14.Labeller.Models;

[module:DapperAot]

namespace SS14.Labeller;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", true, true);
        builder.Configuration.AddJsonFile("appsettings.Secret.json", true, true);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.RegisterDependencies(builder.Configuration);

        var app = builder.Build();

        app.UseHttpLogging();

        app.MapGet("/", () => Results.Ok("Nik is a cat!"));
        app.MapGithubWebhook();
        
        app.Run();
    }
}