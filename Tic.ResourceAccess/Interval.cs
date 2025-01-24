using Dapper;
using Tic.Shared;

namespace Tic.ResourceAccess;

public interface IIntervalResourceAccess
{
    Task<CommandResult> Handle(SaveIntervals command);
    Task<CommandResult> Handle(DeleteInterval command);
    Task<IntervalsResponse> Handle(IntervalsRequest request);
}

public record TimeInterval
{
    public int StartTimeLogId { get; init; }
    public int EndTimeLogId { get; init; }
    public DateOnly Date { get; init; }
    public TimeSpan Duration { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record SaveIntervals
{
    public TimeInterval[] TimeIntervals { get; init; } = [];
}

public record DeleteInterval
{
    public int StartTimeLogId { get; init; }
}

public record IntervalsRequest
{
    public Tuple<DateOnly, DateOnly> DateRange { get; init; } = new(DateOnly.MinValue, DateOnly.MaxValue);
    public string[] Categories { get; init; } = [];
    public string[] Tasks { get; init; } = [];
    public int[] TimeLogIds { get; init; } = [];
}

public record IntervalsResponse
{
    public TimeInterval[] Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public class IntervalResourceAccess : IIntervalResourceAccess, IDatabaseInitializer
{
    private readonly IDataContext _dataContext;

    public IntervalResourceAccess(IDataContext dataContext)
    {
        SqlMapper.AddTypeHandler(new DateOnlyHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
        _dataContext = dataContext;
    }

    public async Task<CommandResult> Handle(SaveIntervals command)
    {
        const string saveInterval = """
            INSERT INTO Intervals (StartTimeLogId, EndTimeLogId, Date, Duration, Category, Task, Description)
            VALUES (@StartTimeLogId, @EndTimeLogId, @Date, @Duration, @Category, @Task, @Description)
            ON CONFLICT (StartTimeLogId) DO UPDATE SET 
                EndTimeLogId = @EndTimeLogId, 
                Date = @Date, 
                Duration = @Duration, 
                Category = @Category, 
                Task = @Task, 
                Description = @Description;
        """;
        
        using var connection = _dataContext.Connect();

        foreach (var interval in command.TimeIntervals)
        {
            await connection.ExecuteAsync(saveInterval, interval);
        }

        return CommandResult.Success;
    }

    public async Task<CommandResult> Handle(DeleteInterval command)
    {
        const string deleteInterval = """
            DELETE FROM Intervals
            WHERE StartTimeLogId = @StartTimeLogId;
        """;
        
        using var connection = _dataContext.Connect();
        
        await connection.ExecuteAsync(deleteInterval, command);
        
        return CommandResult.Success;
    }

    public async Task<IntervalsResponse> Handle(IntervalsRequest request)
    {
        var builder = ApplySpecification(request);

        var selectTemplate = builder.AddTemplate("""
            SELECT StartTimeLogId, EndTimeLogId, Date, Duration, Category, Task, Description
            FROM Intervals
            /**where**/
            """);
        
        var countTemplate = builder.AddTemplate("""
            SELECT COUNT(*)
            FROM Intervals
            /**where**/
            """);
        
        using var connection = _dataContext.Connect();

        var intervals = await connection.QueryAsync<TimeInterval>(selectTemplate.RawSql, selectTemplate.Parameters);
        var totalCount = await connection.ExecuteScalarAsync<int>(countTemplate.RawSql, countTemplate.Parameters);

        return new IntervalsResponse
        {
            Items = intervals.ToArray(),
            TotalCount = totalCount
        };
    }

    private static SqlBuilder ApplySpecification(IntervalsRequest request)
    {
        var builder = new SqlBuilder();

        if (request.DateRange.Item1 == request.DateRange.Item2)
        {
            builder = builder.Where("Date = @Date", new { Date = request.DateRange.Item1 });
        }
        else if (request.DateRange.Item1 != DateOnly.MinValue || request.DateRange.Item2 != DateOnly.MaxValue)
        {
            builder.Where("Date BETWEEN @StartDate AND @EndDate", new
            {
                StartDate = request.DateRange.Item1,
                EndDate = request.DateRange.Item2
            });
        }

        if (request.Categories.Length > 0)
        {
            builder = builder.Where("Category IN @Categories", new { request.Categories });
        }
        
        if (request.Tasks.Length > 0)
        {
            builder = builder.Where("Task IN @Tasks", new { request.Tasks });
        }
        
        if (request.TimeLogIds.Length > 0)
        {
            builder = builder.Where("StartTimeLogId IN @TimeLogIds", new { request.TimeLogIds });
        }

        return builder;
    }
    
    public async Task Init()
    {
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS Intervals (
                StartTimeLogId INTEGER PRIMARY KEY,
                EndTimeLogId INTEGER,
                Date DATE,
                Duration TEXT,
                Category TEXT,
                Task TEXT,
                Description TEXT
            );
            """);
    }
}