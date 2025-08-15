using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace SS14.Labeller.Repository;

public class RepositoryBase(IConfiguration configuration)
{
    protected DbConnection OpenConnection()
    {
        var connectionString = configuration.GetConnectionString("Default") 
                               ?? "Data Source=Application.db";
        var con = new SqliteConnection(connectionString);
        con.Open();
        return con;
    }
}