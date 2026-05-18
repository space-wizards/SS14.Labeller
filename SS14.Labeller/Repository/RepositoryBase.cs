using System.Data.Common;
using Npgsql;

namespace SS14.Labeller.Repository;

public class RepositoryBase(IConfiguration configuration)
{
    protected DbConnection OpenConnection()
    {
        var connectionString = configuration.GetConnectionString("Default") 
                               ?? throw new InvalidOperationException(
                                   "Failed to find 'Default' connection string "
                                   + "from application configuration for repository."
                               ); ;
        var con = new NpgsqlConnection(connectionString);
        con.Open();
        return con;
    }
}