using Tik.ResourceAccess;

namespace Tik.Manager;

public interface IQueryManager
{
    Task<QueryResults<CategoriesResponseItem>> Handle(CategoriesQuery query);
    Task<QueryResults<TimeLogsResponseItem>> Handle(TimeLogsQuery query);
    Task<QueryResults<TimeLogsResponseItem>> Handle(TimeLogsTailQuery query);
    Task<DaySummaryResponse> Handle(DaySummaryQuery query);
}

public record QueryResults<T>
{
    public T[] Items { get; init; } = [];
}

public record CategoriesQuery
{
    public string Query { get; init; } = string.Empty;
    public bool IncludeInactive { get; init; }
}

public record CategoriesResponseItem : Tik.ResourceAccess.CategoriesResponseItem;

public record TimeLogsQuery
{
    public int[] Ids { get; init; } = [];
    public Tuple<DateOnly, DateOnly> DateRange { get; init; } = new(DateOnly.MinValue, DateOnly.MaxValue);
    public string[] Categories { get; init; } = [];
    public string[] Projects { get; init; } = [];
    public string[] Tasks { get; init; } = [];
}

public record TimeLogsTailQuery
{
    public int Count { get; init; } = 20;
}

public record TimeLogsResponseItem : Tik.ResourceAccess.TimeLogsResponseItem
{
    public TimeLogsResponseItem() { }
    public TimeLogsResponseItem(Tik.ResourceAccess.TimeLogsResponseItem source, TimeInterval? interval)
    {
        Id = source.Id;
        Date = source.Date;
        Time = source.Time;
        Category = source.Category;
        Project = source.Project;
        Task = source.Task;
        Description = source.Description;
        Duration = interval?.Duration ?? TimeSpan.Zero;
    }
    
    public TimeSpan Duration { get; init; }
}

public record DaySummaryQuery
{
    public DateOnly Date { get; init; }
}

public record DaySummaryResponse : Tik.ResourceAccess.DaySummaryResponse
{
    public new DayTaskSummary[] DayTaskSummaries { get; } = [];
    public DaySummaryResponse(Tik.ResourceAccess.DaySummaryResponse source)
    {
        DayTaskSummaries = source.DayTaskSummaries.Select(x => new DayTaskSummary(x)).ToArray();
    }
}

public record DayTaskSummary : Tik.ResourceAccess.DayTaskSummary
{
    public DayTaskSummary(Tik.ResourceAccess.DayTaskSummary source)
    {
        Date = source.Date;
        Duration = source.Duration;
        Category = source.Category;
        Task = source.Task;
        Descriptions = source.Descriptions;
    }
}

public class QueryManager(ICategoryResourceAccess categoryResourceAccess,
    ILogResourceAccess logResourceAccess, IIntervalResourceAccess intervalResourceAccess, 
    ISummaryResourceAccess summaryResourceAccess) : IQueryManager
{
    public async Task<QueryResults<CategoriesResponseItem>> Handle(CategoriesQuery query)
    {
        var response = await categoryResourceAccess.Handle(new CategoriesRequest
        {
            Query = query.Query,
            IncludeInactive = query.IncludeInactive
        });
        
        return new QueryResults<CategoriesResponseItem>
        {
            Items = response.Items
                .Select(x => new CategoriesResponseItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    ForegroundColorHex = x.ForegroundColorHex,
                    BackgroundColorHex = x.BackgroundColorHex,
                    IsInactive = x.IsInactive
                })
                .ToArray()
        };
    }
    
    public async Task<QueryResults<TimeLogsResponseItem>> Handle(TimeLogsQuery query)
    {
        var logsResponse = await logResourceAccess.Handle(new TimeLogsRequest
        {
            Ids = query.Ids,
            DateRange = query.DateRange,
            Categories = query.Categories,
            Projects = query.Projects,
            Tasks = query.Tasks
        });
        
        var intervalsResponse = await intervalResourceAccess.Handle(new IntervalsRequest
        {
            DateRange = query.DateRange
        });
        
        return new QueryResults<TimeLogsResponseItem>
        {
            Items = logsResponse.Items
                .Select(x => new TimeLogsResponseItem(x, intervalsResponse.Items
                    .FirstOrDefault(y => y.StartTimeLogId == x.Id)))
                .ToArray()
        };
    }

    public async Task<QueryResults<TimeLogsResponseItem>> Handle(TimeLogsTailQuery query)
    {
        var logsResponse = await logResourceAccess.Handle(new TimeLogsTailRequest
        {
            Count = query.Count
        });
        
        var intervalsResponse = await intervalResourceAccess.Handle(new IntervalsRequest
        {
            DateRange = logsResponse.Items.Length > 0
                ? new Tuple<DateOnly, DateOnly>(logsResponse.Items.Min(x => x.Date), logsResponse.Items.Max(x => x.Date))
                : new Tuple<DateOnly, DateOnly>(DateOnly.MinValue, DateOnly.MaxValue)
        });
        
        return new QueryResults<TimeLogsResponseItem>
        {
            Items = logsResponse.Items
                .Select(x => new TimeLogsResponseItem(x, intervalsResponse.Items
                    .FirstOrDefault(y => y.StartTimeLogId == x.Id)))
                .ToArray()

        };
    }

    public async Task<DaySummaryResponse> Handle(DaySummaryQuery query)
    {
        var response = await summaryResourceAccess.Handle(new DaySummaryRequest
        {
            Date = query.Date
        });

        return new DaySummaryResponse(response);
    }
}