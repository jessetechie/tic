using System.Data;
using Microsoft.Data.Sqlite;

namespace Tik.ResourceAccess;

public interface IDataContext
{
    IDbConnection Connect();
}

public class DataContext : IDataContext
{
    public IDbConnection Connect()
    {
        var connection = new SqliteConnection("Data Source=tik.db");
        connection.Open();
        return connection;
    }
}