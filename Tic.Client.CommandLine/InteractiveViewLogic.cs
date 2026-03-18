using Tic.Manager;

namespace Tic.Client.CommandLine.Commands;

internal static class InteractiveViewLogic
{
    internal static int NavigateUp(int selectedIndex) =>
        Math.Max(0, selectedIndex - 1);

    internal static int NavigateDown(int selectedIndex, int count) =>
        Math.Min(count - 1, selectedIndex + 1);

    internal static TimeLogsResponseItem[] OrderLogs(IEnumerable<TimeLogsResponseItem> logs) =>
        logs.OrderBy(x => x.Date).ThenBy(x => x.Time).ToArray();

    internal static string DisplayValue(DateOnly date) =>
        date.ToString("yyyy-MM-dd");
    
    internal static string DisplayValue(TimeOnly time) =>
        time.ToString("HH:mm");
    
    internal static string DisplayValue(TimeSpan duration)
    {
        var totalHours = (int)duration.TotalHours;
        return $"{totalHours:00}:{duration.Minutes:00}";
    }

    internal static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value;

    internal static ColumnWidths ComputeColumnWidths(TimeLogsResponseItem[] logs) =>
        new()
        {
            CategoryWidth = Math.Max("Category".Length, logs.Max(x => DisplayValue(x.Category).Length)),
            TaskWidth = Math.Max("Task".Length, logs.Max(x => DisplayValue(x.Task).Length)),
            DurationWidth = "Duration".Length
        };

    internal static string BuildHeaderRow(ColumnWidths widths)
    {
        return $"  {"Date",-10}  {"Time",-5}  {"Category".PadRight(widths.CategoryWidth)}  {"Task".PadRight(widths.TaskWidth)}  {"Duration".PadLeft(widths.DurationWidth)}  Description";
    }

    internal static string BuildBorderRow(ColumnWidths widths)
    {
        return $"  ----------  -----  {new string('-', widths.CategoryWidth)}  {new string('-', widths.TaskWidth)}  {new string('-', widths.DurationWidth)}  -----------";
    }
    
    internal static string BuildLogRow(ColumnWidths widths, TimeLogsResponseItem log, bool isSelected)
    {
        var marker = isSelected ? '>' : ' ';
        return $"{marker} {DisplayValue(log.Date)}  {DisplayValue(log.Time)}  {DisplayValue(log.Category).PadRight(widths.CategoryWidth)}  {DisplayValue(log.Task).PadRight(widths.TaskWidth)}  {DisplayValue(log.Duration).PadLeft(widths.DurationWidth)}  {DisplayValue(log.Description)}";
    }
}

internal sealed record ColumnWidths
{
    public int CategoryWidth { get; init; }
    public int TaskWidth { get; init; }
    public int DurationWidth { get; init; }
}

