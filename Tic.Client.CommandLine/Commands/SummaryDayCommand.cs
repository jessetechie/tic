using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Client.CommandLine.UI;
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
            if (!Converter.TryParseDate(settings.Date, out var date, out var error))
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
                .Select(x => new DaySummaryRow
                {
                    Category = x.Category,
                    Task = x.Task,
                    Duration = Converter.DisplayValue(x.Duration),
                    Descriptions = string.Join("; ", x.Descriptions.Where(y => !string.IsNullOrWhiteSpace(y)))
                })
                .ToArray();

            var headers = new Dictionary<string, string>
            {
                ["Category"] = "Category",
                ["Task"] = "Task", 
                ["Duration"] = "Duration",
                ["Descriptions"] = "Descriptions"
            };

            var columnOrder = new[] { "Category", "Task", "Duration", "Descriptions" };
            
            var alignments = new Dictionary<string, ColumnAlignment>
            {
                ["Duration"] = ColumnAlignment.Right
            };

            var calculator = new TableBuilder(headers, columnOrder, "  ", alignments);
            calculator.CalculateWidthsFromProperties(rows);

            AnsiConsole.WriteLine($"Date: {Converter.DisplayValue(date)}");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(calculator.CreateHeaderRow());
            AnsiConsole.WriteLine(calculator.CreateSeparatorRow());

            if (rows.Length == 0)
            {
                AnsiConsole.WriteLine("(no summary items)");
            }
            else
            {
                foreach (var row in rows)
                {
                    var values = new Dictionary<string, string>
                    {
                        ["Category"] = row.Category,
                        ["Task"] = row.Task,
                        ["Duration"] = row.Duration,
                        ["Descriptions"] = row.Descriptions
                    };
                    AnsiConsole.WriteLine(calculator.CreateDataRow(values));
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine($"Total Duration: {Converter.DisplayValue(response.DaySummary.TotalDuration)}");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to load day summary: {Markup.Escape(ex.Message)}[/]");
            return -1;
        }
    }
}