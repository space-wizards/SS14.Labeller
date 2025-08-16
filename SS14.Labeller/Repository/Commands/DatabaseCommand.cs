using System.Data.Common;

namespace SS14.Labeller.Repository.Commands;

public abstract class DatabaseCommandBase<T> : DatabaseRequestBase
{
    public abstract Task<T> Execute(DbConnection connection, CancellationToken ct);

}