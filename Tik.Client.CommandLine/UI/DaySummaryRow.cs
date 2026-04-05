using Tik.Manager;

namespace Tik.Client.CommandLine.UI;

public record DaySummaryRow
{
    public static DaySummaryRow From(DayTaskSummary item) => new DaySummaryRow
    {
        Category = Converter.DisplayValue(item.Category),
        Task = Converter.DisplayValue(item.Task),
        Duration = Converter.DisplayValue(item.Duration),
        Descriptions = string.Join("; ", item.Descriptions.Where(y => !string.IsNullOrWhiteSpace(y)).Select(Converter.DisplayValue))
    };
    
    public string Category { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string Descriptions { get; init; } = string.Empty;
}