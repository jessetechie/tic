using System.Data;
using Tik.ResourceAccess;
using Tik.Tests.Shared;

namespace Tik.Engine.Tests;

public class SummaryCalculatorEngineTests : IDisposable
{
    private readonly LogResourceAccess _logResourceAccess;
    private readonly SummaryResourceAccess _summaryResourceAccess;
    private readonly IDbConnection _connection;
    private readonly IntervalResourceAccess _intervalResourceAccess;

    public SummaryCalculatorEngineTests()
    {
        var dataContext = new TestDataContext("SummaryCalculatorTestDb");
        _logResourceAccess = new LogResourceAccess(dataContext);
        _intervalResourceAccess = new IntervalResourceAccess(dataContext);
        _summaryResourceAccess = new SummaryResourceAccess(dataContext);
        _connection = dataContext.Connect();
        _logResourceAccess.Init().Wait();
        _intervalResourceAccess.Init().Wait();
        _summaryResourceAccess.Init().Wait();
    }
    
    [Fact]
    public async Task can_calculate_day_summary_with_no_time_logs()
    {
        var date = new DateOnly(2025, 1, 1);
        await CalculateSummaries(date);
    }

    [Fact]
    public async Task can_calculate_day_summary_with_one_time_log_added()
    {
        var logResult = await _logResourceAccess.Handle(new AddTimeLog
        {
            Date = new DateOnly(2025, 1, 1),
            Time = new TimeOnly(8, 0, 0),
            Category = "Category1",
            Task = "Task1",
            Description = "Description"
        });
        Assert.True(logResult.IsSuccess);
        
        var date = new DateOnly(2025, 1, 1);
        await CalculateSummaries(date);
        
        var summaryResponse = await _summaryResourceAccess.Handle(new DaySummaryRequest
        {
            Date = date
        });
        
        Assert.Equal(date, summaryResponse.DaySummary.Date);
        Assert.Equal(new TimeSpan(0, 0, 0), summaryResponse.DaySummary.TotalDuration);
        Assert.Empty(summaryResponse.DayTaskSummaries);
    }

    [Fact]
    public async Task can_calculate_day_summary_with_time_logs_added()
    {
        var addTimeLogs = new[]
        {
            new AddTimeLog
            {
                Date = new DateOnly(2025, 1, 1),
                Time = new TimeOnly(8, 0, 0),
                Category = "Category1",
                Task = "Task1",
                Description = "Description"
            },
            new AddTimeLog
            {
                Date = new DateOnly(2025, 1, 1),
                Time = new TimeOnly(9, 0, 0),
                Category = "Category1",
                Task = "Task2",
                Description = "Description"
            },
            new AddTimeLog
            {
                Date = new DateOnly(2025, 1, 1),
                Time = new TimeOnly(12, 0, 0),
                Category = "",
                Task = "",
                Description = "Lunch"
            },
            new AddTimeLog
            {
                Date = new DateOnly(2025, 1, 1),
                Time = new TimeOnly(13, 0, 0),
                Category = "Category1",
                Task = "Task1",
                Description = "Description"
            },
            new AddTimeLog
            {
                Date = new DateOnly(2025, 1, 1),
                Time = new TimeOnly(15, 0, 0),
                Category = "Category1",
                Task = "Task2",
                Description = "Description"
            },
            new AddTimeLog
            {
                Date = new DateOnly(2025, 1, 1),
                Time = new TimeOnly(17, 0, 0),
                Category = "Category2",
                Task = "Task1",
                Description = "Description"
            },
            new AddTimeLog
            {
                Date = new DateOnly(2025, 1, 1),
                Time = new TimeOnly(17, 30, 0),
                Category = "",
                Task = "",
                Description = "Stop"
            }
        };
        
        foreach (var addTimeLog in addTimeLogs)
        {
            var logResult = await _logResourceAccess.Handle(addTimeLog);
            Assert.True(logResult.IsSuccess);
        }
        
        var date = new DateOnly(2025, 1, 1);
        await CalculateSummaries(date);
        
        var summaryResponse = await _summaryResourceAccess.Handle(new DaySummaryRequest
        {
            Date = date
        });
        
        Assert.Equal(date, summaryResponse.DaySummary.Date);
        Assert.Equal(new TimeSpan(8, 30, 0), summaryResponse.DaySummary.TotalDuration);
        Assert.Equal(3, summaryResponse.DayTaskSummaries.Length);
        
        var category1Task1 = summaryResponse.DayTaskSummaries.First(x => x is { Category: "Category1", Task: "Task1" });
        Assert.Equal(new TimeSpan(3, 0, 0), category1Task1.Duration);
        Assert.Equal(2, category1Task1.Descriptions.Length);
        Assert.Contains(category1Task1.Descriptions, x => x == "Description");
        
        var category1Task2 = summaryResponse.DayTaskSummaries.First(x => x is { Category: "Category1", Task: "Task2" });
        Assert.Equal(new TimeSpan(5, 0, 0), category1Task2.Duration);
        Assert.Contains(category1Task1.Descriptions, x => x == "Description");
        
        var category2Task1 = summaryResponse.DayTaskSummaries.First(x => x is { Category: "Category2", Task: "Task1" });
        Assert.Equal(new TimeSpan(0, 30, 0), category2Task1.Duration);
        Assert.Contains(category1Task1.Descriptions, x => x == "Description");
    }

    [Fact]
    public async Task can_recalculate_day_summary_with_time_logs_edited()
    {
        await can_calculate_day_summary_with_time_logs_added();

        var timeLogs = await _logResourceAccess.Handle(new TimeLogsRequest
        {
            DateRange = new Tuple<DateOnly, DateOnly>(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 1))
        });

        var category1Task1Logs = timeLogs.Items
            .Where(x => x is { Category: "Category1", Task: "Task1" })
            .ToArray();
        
        var category2Task1Logs = timeLogs.Items
            .Where(x => x is { Category: "Category2", Task: "Task1" })
            .ToArray();
        
        var timeLog1 = category1Task1Logs.ElementAt(0);
        var editCommand1 = new EditTimeLog
        {
            Id = timeLog1.Id,
            Date = timeLog1.Date,
            Time = new TimeOnly(8, 30, 0),
            Category = timeLog1.Category,
            Task = timeLog1.Task,
            Description = timeLog1.Description
        };
        
        await _logResourceAccess.Handle(editCommand1);
        
        var timeLog2 = category1Task1Logs.ElementAt(1);
        var editCommand2 = new EditTimeLog
        {
            Id = timeLog2.Id,
            Date = timeLog2.Date,
            Time = new TimeOnly(12, 30, 0),
            Category = timeLog2.Category,
            Task = timeLog2.Task,
            Description = timeLog2.Description
        };
        
        await _logResourceAccess.Handle(editCommand2);
        
        var timeLog3 = category2Task1Logs.ElementAt(0);
        var editCommand3 = new EditTimeLog
        {
            Id = timeLog3.Id,
            Date = timeLog3.Date,
            Time = new TimeOnly(16, 0, 0),
            Category = timeLog3.Category,
            Task = timeLog3.Task,
            Description = timeLog3.Description
        };
        
        await _logResourceAccess.Handle(editCommand3);

        var date = new DateOnly(2025, 1, 1);
        await CalculateSummaries(date);
        
        var summaryResponse = await _summaryResourceAccess.Handle(new DaySummaryRequest
        {
            Date = date
        });
        
        Assert.Equal(date, summaryResponse.DaySummary.Date);
        Assert.Equal(new TimeSpan(8, 30, 0), summaryResponse.DaySummary.TotalDuration);
        Assert.Equal(3, summaryResponse.DayTaskSummaries.Length);
        
        var category1Task1 = summaryResponse.DayTaskSummaries.First(x => x is { Category: "Category1", Task: "Task1" });
        Assert.Equal(new TimeSpan(3, 0, 0), category1Task1.Duration);
        
        var category1Task2 = summaryResponse.DayTaskSummaries.First(x => x is { Category: "Category1", Task: "Task2" });
        Assert.Equal(new TimeSpan(4, 0, 0), category1Task2.Duration);
        
        var category2Task1 = summaryResponse.DayTaskSummaries.First(x => x is { Category: "Category2", Task: "Task1" });
        Assert.Equal(new TimeSpan(1, 30, 0), category2Task1.Duration);
    }

    private async Task CalculateSummaries(DateOnly date)
    {
        var engine = new SummaryCalculator(_logResourceAccess, _intervalResourceAccess, _summaryResourceAccess);
        
        var result1 = await engine.Handle(new CalculateLogIntervals
        {
            Date = date
        });
        Assert.True(result1.IsSuccess);
        
        var result2 = await engine.Handle(new CalculateDaySummary
        {
            Date = date
        });
        Assert.True(result2.IsSuccess);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}