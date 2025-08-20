using System.Reflection;
using System.Text.Json;
using System.Text;

namespace SS14.Labeller.Models;

public abstract class EventBase
{
    public required string Action { get; init; }

    public required GithubRepo Repository { get; init; }

    /// <summary>
    /// Deserialization logic for model binding.
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-9.0#bindasync
    /// </summary>
    public static async ValueTask<EventBase?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var eventTypeName = context.Request
                                   .Headers["X-GitHub-Event"]
                                   .FirstOrDefault();

        if (eventTypeName == null)
        {
            throw new InvalidOperationException("Failed to parse request - had no 'X-GitHub-Event' header, which describes type of event in request.");
        }

        if (!EventTypesMap.TryGetValue(eventTypeName, out var eventType))
        {
            throw new InvalidOperationException($"Failed to parse request with event type name '{eventTypeName}' - no type found to deserialize event into.");
        }

        string requestBody;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            requestBody = await reader.ReadToEndAsync();

        var deserialized = (EventBase?)JsonSerializer.Deserialize(requestBody, eventType, SourceGenerationContext.DeserializationContext);
        if (deserialized == null)
        {
            throw new InvalidOperationException($"Failed to parse request into {eventType.Name} according with event type {eventTypeName}.");
        }

        return deserialized;
    }
}

public class GithubRepo
{
    public required User Owner { get; init; }

    public required string Name { get; set; }
}

public class User
{
    public required string Login { get; set; }
}

public class Label
{
    public string? Name { get; set; }
}