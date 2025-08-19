using System.Data.Common;
using Npgsql;

namespace SS14.Labeller.Repository;

public class RepositoryBase(IConfiguration configuration)
{
    protected DbConnection OpenConnection()
    {
        var connectionString = configuration.GetConnectionString("Default") 
                               ?? "Data Source=Application.db";
        var con = new NpgsqlConnection(connectionString);
        con.Open();
        return con;
    }
}