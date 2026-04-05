using System.Data;
using Tik.Tests.Shared;

namespace Tik.ResourceAccess.Tests;

public class CategoryResourceAccessTests : IDisposable
{
    private readonly CategoryResourceAccess _resource;
    private readonly IDbConnection _connection;

    public CategoryResourceAccessTests()
    {
        var dataContext = new TestDataContext("CategoryResourceTestDb");
        _resource = new CategoryResourceAccess(dataContext);
        
        //Keeping a connection open for the duration of the test
        //will ensure that each call to the resource uses the same in-memory database.
        //The database persists as long as at least one connection is open.
        _connection = dataContext.Connect();
        _resource.Init().Wait();
    }
    
    [Fact]
    public async Task can_request_categories()
    {
        var request = new CategoriesRequest();
        var response = await _resource.Handle(request);
        Assert.Empty(response.Items);
    }
    
    [Fact]
    public async Task can_add_category()
    {
        var command = new AddCategory
        {
            Name = "Name",
            ForegroundColorHex = "#FFFFFF",
            BackgroundColorHex = "#000000",
            IsInactive = false
        };
        var result = await _resource.Handle(command);
        Assert.True(result.IsSuccess);

        var request = new CategoriesRequest();
        var response = await _resource.Handle(request);
        Assert.Single(response.Items);
        
        var item = response.Items.First();
        Assert.Equal(command.Name, item.Name);
        Assert.Equal(command.ForegroundColorHex, item.ForegroundColorHex);
        Assert.Equal(command.BackgroundColorHex, item.BackgroundColorHex);
        Assert.Equal(command.IsInactive, item.IsInactive);
    }
    
    [Fact]
    public async Task can_edit_category()
    {
        var addCommand = new AddCategory
        {
            Name = "Name",
            ForegroundColorHex = "#FFFFFF",
            BackgroundColorHex = "#000000",
            IsInactive = false
        };
        var addResult = await _resource.Handle(addCommand);
        Assert.True(addResult.IsSuccess);
        
        var request = new CategoriesRequest();
        var response = await _resource.Handle(request);
        var item = response.Items.First();
        
        var editCommand = new EditCategory
        {
            Id = item.Id,
            Name = "New Name",
            ForegroundColorHex = "#000000",
            BackgroundColorHex = "#FFFFFF",
            IsInactive = true
        };
        var editResult = await _resource.Handle(editCommand);
        Assert.True(editResult.IsSuccess);

        var updatedRequest = new CategoriesRequest { IncludeInactive = true };
        var updatedResponse = await _resource.Handle(updatedRequest);
        Assert.Single(updatedResponse.Items);
        
        var updatedItem = updatedResponse.Items.First();
        Assert.Equal(item.Id, updatedItem.Id);
        Assert.Equal(editCommand.Name, updatedItem.Name);
        Assert.Equal(editCommand.ForegroundColorHex, updatedItem.ForegroundColorHex);
        Assert.Equal(editCommand.BackgroundColorHex, updatedItem.BackgroundColorHex);
        Assert.Equal(editCommand.IsInactive, updatedItem.IsInactive);
    }

    [Fact]
    public async Task can_query_categories()
    {
        var addCommand1 = new AddCategory
        {
            Name = "Level1",
            ForegroundColorHex = "#FFFFFF",
            BackgroundColorHex = "#000000",
            IsInactive = false
        };
        var addResult1 = await _resource.Handle(addCommand1);
        Assert.True(addResult1.IsSuccess);

        var addCommand2 = new AddCategory
        {
            Name = "Level1:Level2",
            ForegroundColorHex = "#FFFFFF",
            BackgroundColorHex = "#000000",
            IsInactive = false
        };
        var addResult2 = await _resource.Handle(addCommand2);
        Assert.True(addResult2.IsSuccess);
        
        var request1 = new CategoriesRequest { Query = "Level1" };
        var response1 = await _resource.Handle(request1);
        Assert.Equal(2, response1.Items.Length);
        
        var request2 = new CategoriesRequest { Query = "Level2" };
        var response2 = await _resource.Handle(request2);
        Assert.Single(response2.Items);
    }
    
    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}