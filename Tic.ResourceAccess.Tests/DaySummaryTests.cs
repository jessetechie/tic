using System.Data;
using Tic.Tests.Shared;

namespace Tic.ResourceAccess.Tests;

public class DaySummaryTests : IDisposable
{
    private readonly SummaryResourceAccess _resource;
    private readonly IDbConnection _connection;

    public DaySummaryTests()
    {
        var dataContext = new TestDataContext("DaySummaryTestDb");
        _resource = new SummaryResourceAccess(dataContext);
        _connection = dataContext.Connect();
        _resource.Init().Wait();
    }
    
    [Fact]
    public async Task can_save_day_summary()
    {
        var command = new SaveDaySummary
        {
            DayTaskSummaries =
            [
                new DayTaskSummary
                {
                    Date = new DateOnly(2025, 1, 1),
                    Duration = new TimeSpan(9, 0, 0),
                    Category = "Category",
                    Task = "Task"
                }
            ]
        };
        var result = await _resource.Handle(command);
        Assert.True(result.IsSuccess);

        var request = new DaySummaryRequest
        {
            Date = new DateOnly(2025, 1, 1)
        };
        var response = await _resource.Handle(request);
        Assert.Equal(command.DayTaskSummaries[0].Date, response.DaySummary.Date);
        Assert.Equal(command.DayTaskSummaries[0].Duration, response.DaySummary.TotalDuration);
        Assert.Single(response.DayTaskSummaries);
        Assert.Equal(command.DayTaskSummaries[0].Date, response.DayTaskSummaries[0].Date);
        Assert.Equal(command.DayTaskSummaries[0].Duration, response.DayTaskSummaries[0].Duration);
        Assert.Equal(command.DayTaskSummaries[0].Category, response.DayTaskSummaries[0].Category);
        Assert.Equal(command.DayTaskSummaries[0].Task, response.DayTaskSummaries[0].Task);
    }
    
    [Fact]
    public async Task can_save_day_summaries()
    {
        var command = new SaveDaySummary
        {
            DayTaskSummaries =
            [
                new DayTaskSummary
                {
                    Date = new DateOnly(2025, 1, 1),
                    Duration = new TimeSpan(4, 0, 0),
                    Category = "Category1",
                    Task = "Task1",
                    Descriptions = ["Description1", "Description2"]
                },
                new DayTaskSummary
                {
                    Date = new DateOnly(2025, 1, 1),
                    Duration = new TimeSpan(4, 0, 0),
                    Category = "Category2",
                    Task = "Task2",
                    Descriptions = ["Description3", "Description4"]
                }
            ]
        };
        var result = await _resource.Handle(command);
        Assert.True(result.IsSuccess);

        var request = new DaySummaryRequest
        {
            Date = new DateOnly(2025, 1, 1)
        };
        var response = await _resource.Handle(request);
        Assert.Equal(command.DayTaskSummaries[0].Date, response.DaySummary.Date);
        Assert.Equal(new TimeSpan(8, 0, 0), response.DaySummary.TotalDuration);
        Assert.Equal(2, response.DayTaskSummaries.Length);
    }
    
    [Fact]
    public async Task can_purge_day_summary()
    {
        var saveCommand = new SaveDaySummary
        {
            DayTaskSummaries =
            [
                new DayTaskSummary
                {
                    Date = new DateOnly(2025, 1, 1),
                    Duration = new TimeSpan(9, 0, 0),
                    Category = "Category",
                    Task = "Task"
                }
            ]
        };
        await _resource.Handle(saveCommand);

        var purgeCommand = new PurgeDaySummary
        {
            Date = new DateOnly(2025, 1, 1)
        };
        var result = await _resource.Handle(purgeCommand);
        Assert.True(result.IsSuccess);

        var request = new DaySummaryRequest
        {
            Date = new DateOnly(2025, 1, 1)
        };
        var response = await _resource.Handle(request);
        Assert.Equal(purgeCommand.Date, response.DaySummary.Date);
        Assert.Equal(TimeSpan.Zero, response.DaySummary.TotalDuration);
        Assert.Empty(response.DayTaskSummaries);
    }
    
    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}