using Tik.Manager;

namespace Tik.Client.CommandLine.UI;

public record LogRow
{
    public static LogRow From(TimeLogsResponseItem item) => new LogRow
    {
        Id = item.Id,
        IdDisplay = Converter.DisplayValue(item.Id),
        Date = Converter.DisplayValue(item.Date),
        Time = Converter.DisplayValue(item.Time),
        Category = Converter.DisplayValue(item.Category),
        Project = Converter.DisplayValue(item.Project),
        Task = Converter.DisplayValue(item.Task),
        Duration = Converter.DisplayValue(item.Duration),
        Description = Converter.DisplayValue(item.Description)
    };
    
    public int Id { get; init; }
    public string IdDisplay { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Project { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}