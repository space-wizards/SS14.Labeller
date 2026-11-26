namespace SS14.Labeller.Repository.Commands;

/// <summary>
/// Base type for all database requests.
/// </summary>
/// <remarks>
/// Requests are executed through Dapper.AOT, which intercepts matching Dapper calls at compile time and
/// generates reflection-free code. <c>CommandDefinition</c> based calls are not intercepted, so requests
/// pass themselves as the parameter object instead, with the <see cref="CancellationToken" /> as a member.
/// </remarks>
public abstract class DatabaseRequestBase
{
    /// <summary>
    /// Cancellation token forwarded to the underlying ADO.NET command by the generated interceptor.
    /// Dapper.AOT recognizes it by type, so it is not treated as a SQL parameter.
    /// TODO: when we will move from AOT - use GetCommand(CancellationToken ct) and pass token as argument with
    /// CommandDefinition overload
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}