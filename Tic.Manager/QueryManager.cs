using Tic.ResourceAccess;

namespace Tic.Manager;

public interface IQueryManager
{
    Task<QueryResults<CategoriesResponseItem>> Handle(CategoriesQuery query);
    Task<QueryResults<TimeLogsResponseItem>> Handle(TimeLogsQuery query);
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

public record CategoriesResponseItem : Tic.ResourceAccess.CategoriesResponseItem;

public record TimeLogsQuery
{
    public int[] Ids { get; init; } = [];
    public Tuple<DateOnly, DateOnly> DateRange { get; init; } = new(DateOnly.MinValue, DateOnly.MaxValue);
    public string[] Categories { get; init; } = [];
    public string[] Tasks { get; init; } = [];
}

public record TimeLogsResponseItem : Tic.ResourceAccess.TimeLogsResponseItem
{
    public TimeSpan Duration { get; init; }
}

public record DaySummaryQuery
{
    public DateOnly Date { get; init; }
}

public record DaySummaryResponse : Tic.ResourceAccess.DaySummaryResponse;

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
            Tasks = query.Tasks
        });
        
        var intervalsResponse = await intervalResourceAccess.Handle(new IntervalsRequest
        {
            DateRange = query.DateRange
        });
        
        return new QueryResults<TimeLogsResponseItem>
        {
            Items = logsResponse.Items
                .Select(x => new TimeLogsResponseItem
                {
                    Id = x.Id,
                    Date = x.Date,
                    Time = x.Time,
                    Category = x.Category,
                    Task = x.Task,
                    Description = x.Description,
                    Duration = intervalsResponse.Items
                        .Where(y => y.StartTimeLogId == x.Id)
                        .Select(y => y.Duration)
                        .FirstOrDefault()
                })
                .ToArray()
        };
    }

    public async Task<DaySummaryResponse> Handle(DaySummaryQuery query)
    {
        var response = await summaryResourceAccess.Handle(new DaySummaryRequest
        {
            Date = query.Date
        });
        
        return new DaySummaryResponse
        {
            DaySummary = response.DaySummary,
            DayTaskSummaries = response.DayTaskSummaries
        };
    }
}