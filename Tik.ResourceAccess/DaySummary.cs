using Dapper;
using Tik.Shared;

namespace Tik.ResourceAccess;

public interface ISummaryResourceAccess
{
    Task<CommandResult> Handle(SaveDaySummary command);
    Task<DaySummaryResponse> Handle(DaySummaryRequest request);
    Task<CommandResult> Handle(PurgeDaySummary command);
}

public record DaySummary
{
    public DateOnly Date { get; init; }
    public TimeSpan TotalDuration { get; init; }
}

public record DayTaskSummary
{
    public DateOnly Date { get; init; }
    public TimeSpan Duration { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public string[] Descriptions { get; init; } = [];
}

public record SaveDaySummary
{
    public DayTaskSummary[] DayTaskSummaries { get; init; } = [];
}

public record DaySummaryRequest
{
    public DateOnly Date { get; init; }
}

public record DaySummaryResponse
{
    public DaySummary DaySummary { get; init; } = new();
    public DayTaskSummary[] DayTaskSummaries { get; init; } = [];
}

public class PurgeDaySummary
{
    public DateOnly Date { get; init; }
}

public class SummaryResourceAccess : ISummaryResourceAccess, IDatabaseInitializer
{
    private readonly IDataContext _dataContext;

    public SummaryResourceAccess(IDataContext dataContext)
    {
        SqlMapper.AddTypeHandler(new DateOnlyHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
        SqlMapper.AddTypeHandler(new StringArrayHandler());
        _dataContext = dataContext;
    }

    public async Task<CommandResult> Handle(SaveDaySummary command)
    {
        const string saveDaySummary = """
            INSERT INTO DaySummaries (Date, TotalDuration)
            VALUES (@Date, @TotalDuration)
            ON CONFLICT (Date) DO UPDATE
                SET TotalDuration = @TotalDuration
            """;

        const string saveDayTaskSummaries = """
            INSERT INTO DayTaskSummaries (Date, Category, Task, Duration, Descriptions)
            VALUES (@Date, @Category, @Task, @Duration, @Descriptions)
            ON CONFLICT (Date, Category, Task) DO UPDATE
                SET Duration = @Duration, Descriptions = @Descriptions
            """;
        
        using var connection = _dataContext.Connect();

        foreach (var grp in command.DayTaskSummaries.GroupBy(x => x.Date))
        {
            var duration = grp.Select(x => x.Duration).Aggregate((a, b) => a + b);
            await connection.ExecuteAsync(saveDaySummary, new { Date = grp.Key, TotalDuration = duration });
            
            foreach (var item in grp)
            {
                await connection.ExecuteAsync(saveDayTaskSummaries, item);
            }
        }
        
        return CommandResult.Success;
    }
    
    public async Task<CommandResult> Handle(PurgeDaySummary command)
    {
        const string deleteDaySummary = "DELETE FROM DaySummaries WHERE Date = @Date;";
        const string deleteDayTaskSummaries = "DELETE FROM DayTaskSummaries WHERE Date = @Date;";
        
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(deleteDaySummary, command);
        await connection.ExecuteAsync(deleteDayTaskSummaries, command);
        
        return CommandResult.Success;
    }
    
    public async Task<DaySummaryResponse> Handle(DaySummaryRequest request)
    {
        const string getDaySummary = "SELECT Date, TotalDuration FROM DaySummaries WHERE Date = @Date;";
        const string getDayTaskSummaries = "SELECT Date, Category, Task, Duration, Descriptions FROM DayTaskSummaries WHERE Date = @Date;";
        
        using var connection = _dataContext.Connect();
        var daySummary = await connection.QuerySingleOrDefaultAsync<DaySummary>(getDaySummary, request);
        var dayTaskSummaries = await connection.QueryAsync<DayTaskSummary>(getDayTaskSummaries, request);

        return new DaySummaryResponse
        {
            DaySummary = daySummary ?? new DaySummary { Date = request.Date },
            DayTaskSummaries = dayTaskSummaries.ToArray()
        };
    }
    
    public async Task Init()
    {
        const string createTables = """
            CREATE TABLE IF NOT EXISTS DaySummaries (
                Date DATE PRIMARY KEY,
                TotalDuration TEXT
            );
            
            CREATE TABLE IF NOT EXISTS DayTaskSummaries (
                Date TEXT,
                Category TEXT,
                Task TEXT,
                Duration TEXT,
                Descriptions TEXT,
                PRIMARY KEY (Date, Category, Task)
            );
            """;
        
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(createTables);
    }
}