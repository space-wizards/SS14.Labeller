using System.Data.Common;
using SS14.Labeller.Repository.Commands;

namespace SS14.Labeller.Repository.Queries;

public abstract class DatabaseQueryBase<T> : DatabaseRequestBase
{
    public abstract Task<T> Query(DbConnection connection, CancellationToken ct);
}