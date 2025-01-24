using System.Data;
using Microsoft.Data.Sqlite;

namespace Tic.ResourceAccess;

public interface IDataContext
{
    IDbConnection Connect();
}

public class DataContext : IDataContext
{
    public IDbConnection Connect()
    {
        var connection = new SqliteConnection("Data Source=tic.db");
        connection.Open();
        return connection;
    }
}