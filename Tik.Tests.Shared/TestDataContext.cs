using System.Data;
using Microsoft.Data.Sqlite;
using Tik.ResourceAccess;

namespace Tik.Tests.Shared;

public class TestDataContext(string databaseName) : IDataContext
{
    private readonly string _connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";

    public IDbConnection Connect()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}