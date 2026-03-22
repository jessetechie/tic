using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Client.CommandLine.UI;
using Tic.Manager;

namespace Tic.Client.CommandLine.Commands;

public sealed class LogListCommandSettings : CommandSettings
{
    [CommandOption("--date")]
    public string? Date { get; init; }

    [CommandOption("--category")]
    public string? Category { get; init; }

    [CommandOption("--project")]
    public string? Project { get; init; }
    
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
            if (!Converter.TryParseDate(settings.Date, out var date, out var dateError))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(dateError)}[/]");
                return -1;
            }

            var rows = await LoadLogs(new TimeLogsQuery
            {
                DateRange = new Tuple<DateOnly, DateOnly>(date, date),
                Categories = string.IsNullOrWhiteSpace(settings.Category) ? [] : [settings.Category],
                Projects = string.IsNullOrWhiteSpace(settings.Project) ? [] : [settings.Project],
                Tasks = string.IsNullOrWhiteSpace(settings.Task) ? [] : [settings.Task]
            });

            var headers = new Dictionary<string, string>
            {
                ["Id"] = "Id",
                ["Date"] = "Date",
                ["Time"] = "Time",
                ["Category"] = "Category",
                ["Project"] = "Project",
                ["Task"] = "Task",
                ["Duration"] = "Duration",
                ["Description"] = "Description"
            };

            var columnOrder = new[] { "Id", "Date", "Time", "Category", "Project", "Task", "Duration", "Description" };
            
            var alignments = new Dictionary<string, ColumnAlignment>
            {
                ["Id"] = ColumnAlignment.Right,
                ["Duration"] = ColumnAlignment.Right
            };

            var table = new TableBuilder(headers, columnOrder, "  ", alignments);
            table.CalculateWidthsFromProperties(rows);

            AnsiConsole.WriteLine($"Date: {Converter.DisplayValue(date)}");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(table.CreateHeaderRow());
            AnsiConsole.WriteLine(table.CreateSeparatorRow());

            if (rows.Length == 0)
            {
                AnsiConsole.WriteLine("(no logs)");
            }
            else
            {
                foreach (var row in rows)
                {
                    var values = new Dictionary<string, string>
                    {
                        ["Id"] = row.IdDisplay,
                        ["Date"] = row.Date,
                        ["Time"] = row.Time,
                        ["Category"] = row.Category,
                        ["Project"] = row.Project,
                        ["Task"] = row.Task,
                        ["Duration"] = row.Duration,
                        ["Description"] = row.Description
                    };
                    AnsiConsole.WriteLine(table.CreateDataRow(values));
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

    private async Task<LogRow[]> LoadLogs(TimeLogsQuery query)
    {
        var response = await queryManager.Handle(query);

        var rows = response.Items
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Time)
            .Select(LogRow.From)
            .ToArray();

        return rows;
    }
}