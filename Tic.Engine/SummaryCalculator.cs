using Tic.ResourceAccess;
using Tic.Shared;

namespace Tic.Engine;

public interface ISummaryCalculator
{
    Task<CommandResult> Handle(CalculateLogIntervals command);
    Task<CommandResult> Handle(CalculateDaySummary command);
}

public record CalculateLogIntervals
{
    public DateOnly Date { get; init; }
}

public record CalculateDaySummary
{
    public DateOnly Date { get; init; }
}

public class SummaryCalculator(ILogResourceAccess logResourceAccess, IIntervalResourceAccess intervalResourceAccess,
    ISummaryResourceAccess summaryResourceAccess) : ISummaryCalculator
{
    public async Task<CommandResult> Handle(CalculateLogIntervals command)
    {
        var logs = await logResourceAccess.Handle(new TimeLogsRequest
        {
            DateRange = new Tuple<DateOnly, DateOnly>(command.Date, command.Date)
        });
        
        await intervalResourceAccess.Handle(new SaveIntervals
        {
            TimeIntervals = MakeIntervals(logs.Items).ToArray()
        });
        
        return CommandResult.Success;
    }
    
    private static IEnumerable<TimeInterval> MakeIntervals(TimeLogsResponseItem[] logsResponseItems)
    {
        var logs = logsResponseItems
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Time)
            .ToArray();

        var pairs = logs
            .Skip(1)
            .Zip(logs, (second, first) => (First: first, Second: second))
            .ToArray();
        
        foreach (var pair in pairs.Where(x => x.First.Category != string.Empty))
        {
            yield return new TimeInterval
            {
                StartTimeLogId = pair.First.Id,
                EndTimeLogId = pair.Second.Id,
                Date = pair.First.Date,
                Duration = pair.Second.Time - pair.First.Time,
                Category = pair.First.Category,
                Task = pair.First.Task,
                Description = pair.First.Description
            };
        }
    }

    public async Task<CommandResult> Handle(CalculateDaySummary command)
    {
        var intervals = await intervalResourceAccess.Handle(new IntervalsRequest
        {
            DateRange = new Tuple<DateOnly, DateOnly>(command.Date, command.Date)
        });

        await summaryResourceAccess.Handle(new SaveDaySummary
        {
            DayTaskSummaries = Summarize(intervals.Items).ToArray()
        });

        return CommandResult.Success;
    }

    private static IEnumerable<DayTaskSummary> Summarize(TimeInterval[] intervals)
    {
        var summaries = intervals
            .GroupBy(x => new {x.Category, x.Task})
            .Select(x => new DayTaskSummary
            {
                Date = x.First().Date,
                Duration = x.Aggregate(TimeSpan.Zero, (acc, y) => acc + y.Duration),
                Category = x.Key.Category,
                Task = x.Key.Task,
                Descriptions = x.Select(y => y.Description).ToArray()
            });

        return summaries;
    }
}