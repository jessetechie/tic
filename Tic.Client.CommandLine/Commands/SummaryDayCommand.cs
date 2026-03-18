using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Manager;

namespace Tic.Client.CommandLine.Commands;

public sealed class SummaryDayCommandSettings : CommandSettings
{
    [CommandOption("--date")]
    public string? Date { get; init; }
}

public sealed class SummaryDayCommand(IQueryManager queryManager) : AsyncCommand<SummaryDayCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SummaryDayCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryParseDate(settings.Date, out var date, out var error))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(error)}[/]");
                return -1;
            }

            var response = await queryManager.Handle(new DaySummaryQuery
            {
                Date = date
            });

            var rows = response.DayTaskSummaries
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Task)
                .Select(x => new SummaryRow
                {
                    Category = x.Category,
                    Task = x.Task,
                    Duration = FormatDuration(x.Duration),
                    Descriptions = string.Join("; ", x.Descriptions.Where(y => !string.IsNullOrWhiteSpace(y)))
                })
                .ToArray();

            var categoryWidth = Math.Max("Category".Length, rows.Select(x => x.Category.Length).DefaultIfEmpty(0).Max());
            var taskWidth = Math.Max("Task".Length, rows.Select(x => x.Task.Length).DefaultIfEmpty(0).Max());
            var durationWidth = Math.Max("Duration".Length, rows.Select(x => x.Duration.Length).DefaultIfEmpty(0).Max());

            AnsiConsole.WriteLine($"Date: {date:yyyy-MM-dd}");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine($"{"Category".PadRight(categoryWidth)}  {"Task".PadRight(taskWidth)}  {"Duration".PadLeft(durationWidth)}  Descriptions");
            AnsiConsole.WriteLine($"{new string('-', categoryWidth)}  {new string('-', taskWidth)}  {new string('-', durationWidth)}  ------------");

            if (rows.Length == 0)
            {
                AnsiConsole.WriteLine("(no summary items)");
            }
            else
            {
                foreach (var row in rows)
                {
                    AnsiConsole.WriteLine($"{row.Category.PadRight(categoryWidth)}  {row.Task.PadRight(taskWidth)}  {row.Duration.PadLeft(durationWidth)}  {row.Descriptions}");
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine($"Total Duration: {FormatDuration(response.DaySummary.TotalDuration)}");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to load day summary: {Markup.Escape(ex.Message)}[/]");
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

    private sealed record SummaryRow
    {
        public string Category { get; init; } = string.Empty;
        public string Task { get; init; } = string.Empty;
        public string Duration { get; init; } = string.Empty;
        public string Descriptions { get; init; } = string.Empty;
    }
}

