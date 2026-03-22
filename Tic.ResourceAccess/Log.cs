using Dapper;
using Tic.Shared;

namespace Tic.ResourceAccess;

public interface ILogResourceAccess
{
    Task<CommandResult> Handle(AddTimeLog addTimeLog);
    Task<CommandResult> Handle(EditTimeLog editTimeLog);
    Task<CommandResult> Handle(DeleteTimeLog deleteTimeLog);
    Task<TimeLogsResponse> Handle(TimeLogsRequest timeLogsRequest);
    Task<TimeLogsResponse> Handle(TimeLogsTailRequest timeLogsTailRequest);
}

public abstract record TimeLog
{
    public DateOnly Date { get; init; }
    public TimeOnly Time { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Project { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record AddTimeLog : TimeLog;

public record EditTimeLog : TimeLog
{
    public int Id { get; init; }
}

public record DeleteTimeLog
{
    public int Id { get; init; }
}

public record TimeLogsRequest
{
    public int[] Ids { get; init; } = [];
    public Tuple<DateOnly, DateOnly> DateRange { get; init; } = new(DateOnly.MinValue, DateOnly.MaxValue);
    public string[] Categories { get; init; } = [];
    public string[] Projects { get; init; } = [];
    public string[] Tasks { get; init; } = [];
}

public record TimeLogsTailRequest
{
    public int Count { get; init; } = 20;
}

public record TimeLogsResponse
{
    public TimeLogsResponseItem[] Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public record TimeLogsResponseItem : TimeLog
{
    public int Id { get; init; }
}

public class LogResourceAccess : ILogResourceAccess, IDatabaseInitializer
{
    private readonly IDataContext _dataContext;

    public LogResourceAccess(IDataContext dataContext)
    {
        SqlMapper.AddTypeHandler(new DateOnlyHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyHandler());
        _dataContext = dataContext;
    }
    
    public async Task<CommandResult> Handle(AddTimeLog addTimeLog)
    {
        const string insertLog = """
            INSERT INTO TimeLogs (Date, Time, Category, Project, Task, Description)
            VALUES (@Date, @Time, @Category, @Project, @Task, @Description);
            """;
        
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(insertLog, addTimeLog);
        
        return CommandResult.Success;        
    }

    public async Task<CommandResult> Handle(EditTimeLog editTimeLog)
    {
        const string updateLog = """
            UPDATE TimeLogs
            SET Date = @Date, Time = @Time, Category = @Category, 
                Project = @Project, Task = @Task, Description = @Description
            WHERE Id = @Id;
            """;
        
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(updateLog, editTimeLog);
        
        return CommandResult.Success;
    }

    public async Task<CommandResult> Handle(DeleteTimeLog deleteTimeLog)
    {
        const string deleteLog = "DELETE FROM TimeLogs WHERE Id = @Id;";
        
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(deleteLog, deleteTimeLog);
        
        return CommandResult.Success;
    }

    public async Task<TimeLogsResponse> Handle(TimeLogsRequest timeLogsRequest)
    {
        var builder = ApplySpecification(timeLogsRequest);

        var selectTemplate = builder.AddTemplate("""
            SELECT Id, Date, Time, Category, Project, Task, Description
            FROM TimeLogs
            /**where**/
            """);
        
        var countTemplate = builder.AddTemplate("SELECT COUNT(*) FROM TimeLogs /**where**/;");
        
        using var connection = _dataContext.Connect();
        var logs = await connection.QueryAsync<TimeLogsResponseItem>(selectTemplate.RawSql, selectTemplate.Parameters);
        var totalCount = await connection.ExecuteScalarAsync<int>(countTemplate.RawSql, countTemplate.Parameters);
        
        return new TimeLogsResponse
        {
            Items = logs.ToArray(),
            TotalCount = totalCount
        };
    }

    private static SqlBuilder ApplySpecification(TimeLogsRequest timeLogsRequest)
    {
        var builder = new SqlBuilder();
        
        if (timeLogsRequest.Ids.Length > 0)
        {
            builder = builder.Where("Id IN @Ids", new { timeLogsRequest.Ids });
        }
        
        if (timeLogsRequest.DateRange.Item1 == timeLogsRequest.DateRange.Item2)
        {
            builder = builder.Where("Date = @Date", new { Date = timeLogsRequest.DateRange.Item1 });
        }
        else if (timeLogsRequest.DateRange.Item1 != DateOnly.MinValue || timeLogsRequest.DateRange.Item2 != DateOnly.MaxValue)
        {
            builder = builder.Where("Date BETWEEN @StartDate AND @EndDate", new
            {
                StartDate = timeLogsRequest.DateRange.Item1,
                EndDate = timeLogsRequest.DateRange.Item2
            });
        }
        
        if (timeLogsRequest.Categories.Length > 0)
        {
            builder = builder.Where("Category IN @Categories", new { timeLogsRequest.Categories });
        }

        if (timeLogsRequest.Projects.Length > 0)
        {
            builder = builder.Where("Project IN @Projects", new { timeLogsRequest.Projects });
        }
        
        if (timeLogsRequest.Tasks.Length > 0)
        {
            builder = builder.Where("Task IN @Tasks", new { timeLogsRequest.Tasks });
        }
        
        return builder;
    }
    
    public async Task<TimeLogsResponse> Handle(TimeLogsTailRequest timeLogsTailRequest)
    {
        var builder = new SqlBuilder();
        var parameters = new { Count = timeLogsTailRequest.Count };
        var selectTemplate = builder.AddTemplate("""
            SELECT Id, Date, Time, Category, Project, Task, Description
            FROM TimeLogs
            ORDER BY Date DESC, Time DESC
            LIMIT @Count
            """, parameters);
        
        var countTemplate = builder.AddTemplate("SELECT COUNT(*) FROM TimeLogs ORDER BY Date DESC, Time DESC LIMIT @Count;",
            parameters);
        
        using var connection = _dataContext.Connect();
        var logs = await connection.QueryAsync<TimeLogsResponseItem>(selectTemplate.RawSql, selectTemplate.Parameters);
        var totalCount = await connection.ExecuteScalarAsync<int>(countTemplate.RawSql, countTemplate.Parameters);
        
        return new TimeLogsResponse
        {
            Items = logs.ToArray(),
            TotalCount = totalCount
        };
    }

    public async Task Init()
    {
        const string createLogsTable = """
            CREATE TABLE IF NOT EXISTS TimeLogs (
                Id INTEGER PRIMARY KEY,
                Date TEXT NOT NULL,
                Time TEXT NOT NULL,
                Category TEXT NOT NULL,
                Project TEXT NOT NULL,
                Task TEXT NOT NULL,
                Description TEXT NOT NULL
            );
            """;

        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(createLogsTable);
    }
}
