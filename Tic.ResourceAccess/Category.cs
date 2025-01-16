using Dapper;
using Tic.Shared;

namespace Tic.ResourceAccess;

public interface ICategoryResourceAccess
{
    Task<CommandResult> Handle(AddCategory addCategory);
    Task<CommandResult> Handle(EditCategory editCategory);
    Task<CategoriesResponse> Handle(CategoriesRequest categoriesRequest);
}

public record Category
{
    public string Name { get; init; } = string.Empty;
    public string ForegroundColorHex { get; init; } = string.Empty;
    public string BackgroundColorHex { get; init; } = string.Empty;
    public bool IsInactive { get; init; }
}

public record AddCategory : Category;

public record EditCategory : Category
{
    public int Id { get; init; }
}

public record CategoriesRequest
{
    public string Query { get; init; } = string.Empty;
    public bool IncludeInactive { get; init; }
}

public record CategoriesResponse
{
    public CategoriesResponseItem[] Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public record CategoriesResponseItem : Category
{
    public int Id { get; init; }
}

public class CategoryResourceAccess : ICategoryResourceAccess, IDatabaseInitializer
{
    private readonly IDataContext _dataContext;

    public CategoryResourceAccess(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<CommandResult> Handle(AddCategory addCategory)
    {
        const string insertCategory = """
            INSERT INTO Categories (Name, ForegroundColorHex, BackgroundColorHex, IsInactive)
            VALUES (@Name, @ForegroundColorHex, @BackgroundColorHex, @IsInactive);
            """;
        
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(insertCategory, addCategory);

        return CommandResult.Success;
    }
    
    public async Task<CommandResult> Handle(EditCategory editCategory)
    {
        const string updateCategory = """
            UPDATE Categories
            SET Name = @Name, ForegroundColorHex = @ForegroundColorHex, BackgroundColorHex = @BackgroundColorHex, IsInactive = @IsInactive
            WHERE Id = @Id;
            """;
        
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(updateCategory, editCategory);
        
        return CommandResult.Success;
    }
    
    public async Task<CategoriesResponse> Handle(CategoriesRequest categoriesRequest)
    {
        var builder = ApplySpecification(categoriesRequest);
        
        var selectTemplate = builder.AddTemplate("""
            SELECT Id, Name, ForegroundColorHex, BackgroundColorHex, IsInactive
            FROM Categories
            /**where**/
            """);
        
        var countTemplate = builder.AddTemplate("SELECT COUNT(*) FROM Categories /**where**/");
        
        using var connection = _dataContext.Connect();
        var categories = await connection.QueryAsync<CategoriesResponseItem>(selectTemplate.RawSql, selectTemplate.Parameters);
        var totalCount = await connection.ExecuteScalarAsync<int>(countTemplate.RawSql, countTemplate.Parameters);
        
        return new CategoriesResponse
        {
            Items = categories.ToArray(),
            TotalCount = totalCount
        };
    }
    
    private static SqlBuilder ApplySpecification(CategoriesRequest categoriesRequest)
    {
        var builder = new SqlBuilder();
        if (!categoriesRequest.IncludeInactive)
        {
            builder = builder.Where("IsInactive = 0");
        }

        if (!string.IsNullOrWhiteSpace(categoriesRequest.Query))
        {
            builder = builder.Where("Name LIKE @Query", new { Query = $"%{categoriesRequest.Query}%" });
        }

        builder.OrderBy("Name");
        return builder;
    }
    
    public async Task Init()
    {
        const string createCategoriesTable = """
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ForegroundColorHex TEXT NOT NULL,
                BackgroundColorHex TEXT NOT NULL,
                IsInactive INTEGER NOT NULL
            );
            """;
        
        using var connection = _dataContext.Connect();
        await connection.ExecuteAsync(createCategoriesTable);
    }
}