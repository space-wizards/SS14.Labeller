using Dapper;

namespace SS14.Labeller.Repository.Commands;

public abstract class DatabaseRequestBase
{
    protected CommandDefinition GetCommand(CancellationToken ct)
    {
        var commandText = GetSql();
        return new CommandDefinition(commandText, this, cancellationToken: ct);
    }

    protected abstract string GetSql();
}