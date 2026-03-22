using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Client.CommandLine.UI;
using Tic.Manager;

namespace Tic.Client.CommandLine.Commands;

public class InteractiveCommandSettings : CommandSettings;

public class InteractiveCommand(ICommandManager commandManager, IQueryManager queryManager)
    : AsyncCommand<InteractiveCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InteractiveCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>
        {
            ["Marker"] = " ",
            ["Date"] = "Date",
            ["Time"] = "Time",
            ["Category"] = "Category",
            ["Project"] = "Project",
            ["Task"] = "Task",
            ["Duration"] = "Duration",
            ["Description"] = "Description"
        };

        var columnOrder = new[] { "Marker", "Date", "Time", "Category", "Project", "Task", "Duration", "Description" };

        var alignments = new Dictionary<string, ColumnAlignment>
        {
            ["Duration"] = ColumnAlignment.Right
        };
        
        try
        {
            int? selectedLogId = null;
            int? renderStartTop = null;
            var renderedLineCount = 0;
            var statusMessage = string.Empty;
            var table = new TableBuilder(headers, columnOrder, "  ", alignments);
            var logs = Array.Empty<LogRow>();
            RenderLayout? renderLayout = null;
            var needsFullRender = true;

            while (true)
            {
                if (needsFullRender)
                {
                    logs = await LoadLogs();
                    table.CalculateWidthsFromProperties(logs);
                    
                    if (logs.Length == 0)
                    {
                        ClearPreviousRender(renderStartTop, renderedLineCount);
                        AnsiConsole.MarkupLine("[yellow]No logs found.[/]");
                        return 0;
                    }

                    if (selectedLogId == null || logs.All(x => x.Id != selectedLogId.Value))
                    {
                        selectedLogId = logs[^1].Id;
                    }

                    ClearPreviousRender(renderStartTop, renderedLineCount);
                    renderStartTop = Console.CursorTop;
                    renderLayout = RenderLogs(table, logs, selectedLogId.Value, statusMessage);
                    renderedLineCount = renderLayout.LineCount;
                    statusMessage = string.Empty;
                    needsFullRender = false;
                }

                if (selectedLogId == null)
                {
                    needsFullRender = true;
                    continue;
                }

                var selectedIndex = Array.FindIndex(logs, x => x.Id == selectedLogId.Value);
                if (selectedIndex < 0)
                {
                    selectedLogId = logs[0].Id;
                    needsFullRender = true;
                    continue;
                }

                var key = ReadInteractionKey();

                if (key == ConsoleKey.Q || key == ConsoleKey.Escape)
                {
                    return 0;
                }

                if (key == ConsoleKey.UpArrow)
                {
                    var newIndex = Navigator.Up(selectedIndex);
                    if (newIndex != selectedIndex)
                    {
                        if (renderLayout == null || !UpdateSelectedRows(table, renderLayout, logs, selectedIndex, newIndex))
                        {
                            needsFullRender = true;
                        }
                        selectedLogId = logs[newIndex].Id;
                    }
                    continue;
                }

                if (key == ConsoleKey.DownArrow)
                {
                    var newIndex = Navigator.Down(selectedIndex, logs.Length);
                    if (newIndex != selectedIndex)
                    {
                        if (renderLayout == null || !UpdateSelectedRows(table, renderLayout, logs, selectedIndex, newIndex))
                        {
                            needsFullRender = true;
                        }
                        selectedLogId = logs[newIndex].Id;
                    }
                    continue;
                }

                if (key == ConsoleKey.A)
                {
                    var promptStartTop = Console.CursorTop;
                    var addCommand = PromptAdd();
                    await commandManager.Handle(addCommand);
                    ClearPromptRegion(promptStartTop);
                    statusMessage = "Time log added.";
                    needsFullRender = true;
                    continue;
                }

                if (key == ConsoleKey.R)
                {
                    needsFullRender = true;
                    continue;
                }

                var selectedLog = logs[selectedIndex];

                if (key == ConsoleKey.E)
                {
                    var promptStartTop = Console.CursorTop;
                    var updateCommand = PromptEdit(selectedLog);
                    await commandManager.Handle(updateCommand);
                    ClearPromptRegion(promptStartTop);
                    statusMessage = "Time log updated.";
                    needsFullRender = true;
                    continue;
                }

                if (key == ConsoleKey.D)
                {
                    var promptStartTop = Console.CursorTop;
                    if (!PromptDeleteConfirmation(selectedLog))
                    {
                        ClearPromptRegion(promptStartTop);
                        statusMessage = "Delete canceled.";
                        needsFullRender = true;
                        continue;
                    }

                    await commandManager.Handle(new DeleteTimeLogCommand { Id = selectedLog.Id });
                    ClearPromptRegion(promptStartTop);
                    statusMessage = "Time log deleted.";
                    needsFullRender = true;
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Interactive mode failed: {Markup.Escape(ex.Message)}[/]");
            return -1;
        }
    }

    private async Task<LogRow[]> LoadLogs()
    {
        var response = await queryManager.Handle(new TimeLogsTailQuery());
        return response.Items
            .OrderBy(x => x.Date).ThenBy(x => x.Time)
            .Select(LogRow.From)
            .ToArray();
    }

    private static RenderLayout RenderLogs(TableBuilder table, LogRow[] logs, int selectedLogId, string statusMessage)
    {
        var startTop = Console.CursorTop;
        var lineCount = 0;

        AnsiConsole.WriteLine();
        lineCount++;
        AnsiConsole.WriteLine(table.CreateHeaderRow());
        lineCount++;
        AnsiConsole.WriteLine(table.CreateSeparatorRow());
        lineCount++;

        foreach (var log in logs)
        {
            var values = new Dictionary<string, string>
            {
                ["Marker"] = log.Id == selectedLogId ? ">" : " ",
                ["Date"] = log.Date,
                ["Time"] = log.Time,
                ["Category"] = log.Category,
                ["Project"] = log.Project,
                ["Task"] = log.Task,
                ["Duration"] = log.Duration,
                ["Description"] = log.Description
            };

            AnsiConsole.WriteLine(table.CreateDataRow(values));
            lineCount++;
        }

        AnsiConsole.WriteLine();
        lineCount++;
        AnsiConsole.MarkupLine("[grey]Use [bold]Up/Down[/] to move selection, [bold]a[/] to add, [bold]e[/] to edit, [bold]d[/] to delete, [bold]r[/] to refresh, [bold]q[/] to quit.[/]");
        AnsiConsole.MarkupLine("[grey]Use [bold]Up/Down[/] to move selection, [bold]a[/] to add, [bold]e[/] to edit, [bold]d[/] to delete, [bold]r[/] to refresh, [bold]q[/] to quit.[/]");
        lineCount++;

        if (string.IsNullOrWhiteSpace(statusMessage))
            AnsiConsole.WriteLine();
        else
            AnsiConsole.MarkupLine($"[green]{Markup.Escape(statusMessage)}[/]");
        lineCount++;

        AnsiConsole.WriteLine();
        lineCount++;

        return new RenderLayout
        {
            LineCount = lineCount,
            RowStartTop = startTop + 3,
            InputTop = startTop + lineCount,
            ClearWidth = Math.Max(Console.WindowWidth - 1, 1)
        };
    }

    // Returns false if cursor manipulation failed so the caller can fall back to a full re-render.
    private static bool UpdateSelectedRows(TableBuilder table, RenderLayout layout, LogRow[] logs, int oldIndex, int newIndex)
    {
        if (Console.IsOutputRedirected)
        {
            return false;
        }

        try
        {
            RenderRow(table, layout, logs[oldIndex], oldIndex, false);
            RenderRow(table, layout, logs[newIndex], newIndex, true);
            Console.SetCursorPosition(0, Math.Min(layout.InputTop, Console.BufferHeight - 1));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RenderRow(TableBuilder table, RenderLayout layout, LogRow log, int rowIndex, bool isSelected)
    {
        var rowTop = layout.RowStartTop + rowIndex;
        if (rowTop < 0 || rowTop >= Console.BufferHeight)
        {
            return;
        }

        Console.SetCursorPosition(0, rowTop);
        Console.Write(new string(' ', layout.ClearWidth));
        Console.SetCursorPosition(0, rowTop);
        
        var values = new Dictionary<string, string>
        {
            ["Marker"] = isSelected ? ">" : " ",
            ["Date"] = log.Date,
            ["Time"] = log.Time,
            ["Category"] = log.Category,
            ["Project"] = log.Project,
            ["Task"] = log.Task,
            ["Duration"] = log.Duration,
            ["Description"] = log.Description
        };

        AnsiConsole.WriteLine(table.CreateDataRow(values));
    }

    private static void ClearPreviousRender(int? startTop, int lineCount)
    {
        if (startTop == null || lineCount <= 0 || Console.IsOutputRedirected)
        {
            return;
        }

        try
        {
            var width = Math.Max(Console.WindowWidth - 1, 1);
            var top = Math.Max(startTop.Value, 0);
            for (var i = 0; i < lineCount && top + i < Console.BufferHeight; i++)
            {
                Console.SetCursorPosition(0, top + i);
                Console.Write(new string(' ', width));
            }
            Console.SetCursorPosition(0, top);
        }
        catch
        {
            // no-op fallback
        }
    }

    private static void ClearPromptRegion(int startTop)
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        try
        {
            var width = Math.Max(Console.WindowWidth - 1, 1);
            var endTop = Console.CursorTop;
            if (startTop < 0 || startTop > endTop)
            {
                return;
            }
            for (var row = startTop; row <= endTop && row < Console.BufferHeight; row++)
            {
                Console.SetCursorPosition(0, row);
                Console.Write(new string(' ', width));
            }
            Console.SetCursorPosition(0, startTop);
        }
        catch
        {
            // no-op fallback
        }
    }

    private static ConsoleKey ReadInteractionKey()
    {
        if (Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine("[red]Interactive mode requires an interactive terminal. Standard input is redirected.[/]");
            Environment.Exit(1);
        }

        try
        {
            return Console.ReadKey(intercept: true).Key;
        }
        catch (InvalidOperationException)
        {
            AnsiConsole.MarkupLine("[red]Interactive mode requires an interactive terminal. Unable to read key input.[/]");
            Environment.Exit(1);
        }
        
        return ConsoleKey.Escape;
    }

    private static AddTimeLogCommand PromptAdd()
    {
        var date = PromptDate("Date", Converter.DisplayValue(DateOnly.FromDateTime(DateTime.Now)));
        var time = PromptTime("Time", Converter.DisplayValue(TimeOnly.FromDateTime(DateTime.Now)));

        var category = AnsiConsole.Prompt(
            new TextPrompt<string>("Category").AllowEmpty());
        var project = AnsiConsole.Prompt(
            new TextPrompt<string>("Project").AllowEmpty());
        var task = AnsiConsole.Prompt(
            new TextPrompt<string>("Task").AllowEmpty());
        var description = AnsiConsole.Prompt(
            new TextPrompt<string>("Description").AllowEmpty());

        return new AddTimeLogCommand
        {
            Date = date,
            Time = time,
            Category = category,
            Project = project,
            Task = task,
            Description = description
        };
    }
    
    private static UpdateTimeLogCommand PromptEdit(LogRow log)
    {
        //TODO: Currently we can't edit a log to clear a value.
        //If we leave it blank, this is considered accepting the default value.
        //There is a PR to allow setting .EditableDefaultValue(true)
        //and it has been merged, but a release has not been made.
        //https://github.com/spectreconsole/spectre.console/pull/2016
        //This will put the existing value in the text entry for editing.
        //Theoretically this will allow setting an empty value.
        //In the meantime, we can set a placeholder empty value, like "-".
        
        var date = PromptDate("Date", log.Date);
        var time = PromptTime("Time", log.Time);

        var category = AnsiConsole.Prompt(
            new TextPrompt<string>("Category").DefaultValue(log.Category).AllowEmpty());
        var project = AnsiConsole.Prompt(
            new TextPrompt<string>("Project").DefaultValue(log.Project).AllowEmpty());
        var task = AnsiConsole.Prompt(
            new TextPrompt<string>("Task").DefaultValue(log.Task).AllowEmpty());
        var description = AnsiConsole.Prompt(
            new TextPrompt<string>("Description").DefaultValue(log.Description).AllowEmpty());

        return new UpdateTimeLogCommand
        {
            Id = log.Id,
            Date = date,
            Time = time,
            Category = category,
            Project = project,
            Task = task,
            Description = description
        };
    }

    private static DateOnly PromptDate(string label, string defaultValue)
    {
        while (true)
        {
            var value = AnsiConsole.Prompt(
                new TextPrompt<string>($"{label} (yyyy-MM-dd)")
                    .DefaultValue(defaultValue));
            if (Converter.TryParseDate(value, out var date, out var error))
            {
                return date;
            }
            AnsiConsole.MarkupLine($"[red]{error}[/]");
        }
    }
    
    private static TimeOnly PromptTime(string label, string defaultValue)
    {
        while (true)
        {
            var value = AnsiConsole.Prompt(
                new TextPrompt<string>($"{label} (HH:mm)")
                    .DefaultValue(defaultValue));
            if (Converter.TryParseTime(value, out var time, out var error))
            {
                return time;
            }
            AnsiConsole.MarkupLine($"[red]{error}[/]");
        }
    }

    private static bool PromptDeleteConfirmation(LogRow log)
    {
        return AnsiConsole.Confirm($"Delete log {log.Id} at {log.Date} {log.Time}?", false);
    }

    private sealed record RenderLayout
    {
        public int LineCount { get; init; }
        public int RowStartTop { get; init; }
        public int InputTop { get; init; }
        public int ClearWidth { get; init; }
    }
}
