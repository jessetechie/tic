using System.Data;
using Tic.Tests.Shared;

namespace Tic.ResourceAccess.Tests;

public class LogResourceAccessTests : IDisposable
{
    private readonly LogResourceAccess _resource;
    private readonly IDbConnection _connection;

    public LogResourceAccessTests()
    {
        var dataContext = new TestDataContext("LogResourceTestDb");
        _resource = new LogResourceAccess(dataContext);

        //Keeping a connection open for the duration of the test
        //will ensure that each call to the resource uses the same in-memory database.
        //The database persists as long as at least one connection is open.
        _connection = dataContext.Connect();
        _resource.Init().Wait();
    }
    
    [Fact]
    public async Task can_request_time_logs()
    {
        var request = new TimeLogsRequest();
        var response = await _resource.Handle(request);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task can_add_time_log()
    {
        var command = new AddTimeLog
        {
            Date = new DateOnly(2025, 1, 1),
            Time = new TimeOnly(9, 0, 0),
            Category = "Category",
            Task = "Task",
            Description = "Description"
        };
        var result = await _resource.Handle(command);
        Assert.True(result.IsSuccess);

        var request = new TimeLogsRequest();
        var response = await _resource.Handle(request);
        Assert.Single(response.Items);
        
        var item = response.Items.First();
        Assert.Equal(command.Date, item.Date);
        Assert.Equal(command.Time, item.Time);
        Assert.Equal(command.Category, item.Category);
        Assert.Equal(command.Task, item.Task);
        Assert.Equal(command.Description, item.Description);
    }
    
    [Fact]
    public async Task can_edit_time_log()
    {
        var addCommand = new AddTimeLog
        {
            Date = new DateOnly(2025, 1, 1),
            Time = new TimeOnly(9, 0, 0),
            Category = "Category",
            Task = "Task",
            Description = "Description"
        };
        var addResult = await _resource.Handle(addCommand);
        Assert.True(addResult.IsSuccess);

        var request = new TimeLogsRequest();
        var response = await _resource.Handle(request);
        var item = response.Items.First();
        
        var editCommand = new EditTimeLog
        {
            Id = item.Id,
            Date = new DateOnly(2025, 1, 1),
            Time = new TimeOnly(10, 0, 0),
            Category = "Category",
            Task = "Task",
            Description = "Description"
        };
        var editResult = await _resource.Handle(editCommand);
        Assert.True(editResult.IsSuccess);

        var updatedRequest = new TimeLogsRequest();
        var updatedResponse = await _resource.Handle(updatedRequest);
        Assert.Single(updatedResponse.Items);
        
        var updatedItem = updatedResponse.Items.First();
        Assert.Equal(item.Id, updatedItem.Id);
        Assert.Equal(editCommand.Date, updatedItem.Date);
        Assert.Equal(editCommand.Time, updatedItem.Time);
        Assert.Equal(editCommand.Category, updatedItem.Category);
        Assert.Equal(editCommand.Task, updatedItem.Task);
        Assert.Equal(editCommand.Description, updatedItem.Description);
    }
    
    [Fact]
    public async Task can_delete_time_log()
    {
        var addCommand = new AddTimeLog
        {
            Date = new DateOnly(2025, 1, 1),
            Time = new TimeOnly(9, 0, 0),
            Category = "Category",
            Task = "Task",
            Description = "Description"
        };
        var addResult = await _resource.Handle(addCommand);
        Assert.True(addResult.IsSuccess);

        var request = new TimeLogsRequest();
        var response = await _resource.Handle(request);
        var item = response.Items.First();
        
        var deleteCommand = new DeleteTimeLog
        {
            Id = item.Id
        };
        var deleteResult = await _resource.Handle(deleteCommand);
        Assert.True(deleteResult.IsSuccess);

        var updatedRequest = new TimeLogsRequest();
        var updatedResponse = await _resource.Handle(updatedRequest);
        Assert.Empty(updatedResponse.Items);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}