using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Manager;

namespace Tic.Client.CommandLine.Commands;

public sealed class LogListCommandSettings : CommandSettings
{
    [CommandOption("--date")]
    public string? Date { get; init; }

    [CommandOption("--category")]
    public string? Category { get; init; }

    [CommandOption("--task")]
    public string? Task { get; init; }
}

public sealed class LogListCommand(IQueryManager queryManager) : AsyncCommand<LogListCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, LogListCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryParseDate(settings.Date, out var date, out var dateError))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(dateError)}[/]");
                return -1;
            }

            var response = await queryManager.Handle(new TimeLogsQuery
            {
                DateRange = new Tuple<DateOnly, DateOnly>(date, date),
                Categories = string.IsNullOrWhiteSpace(settings.Category) ? [] : [settings.Category],
                Tasks = string.IsNullOrWhiteSpace(settings.Task) ? [] : [settings.Task]
            });

            var rows = response.Items
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Time)
                .Select(x => new LogRow
                {
                    Id = x.Id.ToString(),
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    Time = x.Time.ToString("HH:mm:ss"),
                    Category = string.IsNullOrWhiteSpace(x.Category) ? "" : x.Category,
                    Task = string.IsNullOrWhiteSpace(x.Task) ? "" : x.Task,
                    Duration = FormatDuration(x.Duration),
                    Description = string.IsNullOrWhiteSpace(x.Description) ? "" : x.Description
                })
                .ToArray();

            var idWidth = Math.Max("Id".Length, rows.Select(x => x.Id.Length).DefaultIfEmpty(0).Max());
            var dateWidth = Math.Max("Date".Length, rows.Select(x => x.Date.Length).DefaultIfEmpty(0).Max());
            var timeWidth = Math.Max("Time".Length, rows.Select(x => x.Time.Length).DefaultIfEmpty(0).Max());
            var categoryWidth = Math.Max("Category".Length, rows.Select(x => x.Category.Length).DefaultIfEmpty(0).Max());
            var taskWidth = Math.Max("Task".Length, rows.Select(x => x.Task.Length).DefaultIfEmpty(0).Max());
            var durationWidth = Math.Max("Duration".Length, rows.Select(x => x.Duration.Length).DefaultIfEmpty(0).Max());

            AnsiConsole.WriteLine($"Date: {date:yyyy-MM-dd}");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine($"{"Id".PadLeft(idWidth)}  {"Date".PadRight(dateWidth)}  {"Time".PadRight(timeWidth)}  {"Category".PadRight(categoryWidth)}  {"Task".PadRight(taskWidth)}  {"Duration".PadLeft(durationWidth)}  Description");
            AnsiConsole.WriteLine($"{new string('-', idWidth)}  {new string('-', dateWidth)}  {new string('-', timeWidth)}  {new string('-', categoryWidth)}  {new string('-', taskWidth)}  {new string('-', durationWidth)}  -----------");

            if (rows.Length == 0)
            {
                AnsiConsole.WriteLine("(no logs)");
            }
            else
            {
                foreach (var row in rows)
                {
                    AnsiConsole.WriteLine($"{row.Id.PadLeft(idWidth)}  {row.Date.PadRight(dateWidth)}  {row.Time.PadRight(timeWidth)}  {row.Category.PadRight(categoryWidth)}  {row.Task.PadRight(taskWidth)}  {row.Duration.PadLeft(durationWidth)}  {row.Description}");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to list time logs: {Markup.Escape(ex.Message)}[/]");
            return -1;
        }
    }

    private static bool TryParseDate(string? value, out DateOnly date, out string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = DateOnly.FromDateTime(DateTime.Today);
            error = string.Empty;
            return true;
        }

        if (DateOnly.TryParse(value, out date))
        {
            error = string.Empty;
            return true;
        }

        error = "Invalid --date value. Use a valid date like 2026-03-21.";
        return false;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalHours = (int)duration.TotalHours;
        return $"{totalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private sealed record LogRow
    {
        public string Id { get; init; } = string.Empty;
        public string Date { get; init; } = string.Empty;
        public string Time { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Task { get; init; } = string.Empty;
        public string Duration { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }
}

