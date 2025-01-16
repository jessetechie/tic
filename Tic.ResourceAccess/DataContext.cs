using System.Data;
using Microsoft.Data.Sqlite;

namespace Tic.ResourceAccess;

public interface IDataContext
{
    IDbConnection Connect();
}

public interface IDatabaseInitializer
{
    Task Init();
}

public class DataContext : IDataContext
{
    public DataContext(IEnumerable<IDatabaseInitializer> initializers)
    {
        foreach (var initializer in initializers)
        {
            initializer.Init();
        }
    }
    
    public IDbConnection Connect()
    {
        var connection = new SqliteConnection("Data Source=tic.db");
        connection.Open();
        return connection;
    }
}