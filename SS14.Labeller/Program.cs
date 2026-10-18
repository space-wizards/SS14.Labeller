using Dapper;
using Serilog;
using SS14.Labeller.Endpoints;

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

        builder.Services.RegisterDependencies(builder.Configuration);

        var app = builder.Build();

        app.UseHttpLogging();

        app.MapGet("/", () => Results.Ok("Nik is a cat!"));
        app.MapGithubWebhook();
        
        app.Run();

        Log.CloseAndFlush();
    }
}