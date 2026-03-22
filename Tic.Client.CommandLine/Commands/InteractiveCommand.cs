using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Manager;
using TimeLogsResponseItem = Tic.Manager.TimeLogsResponseItem;

namespace Tic.Client.CommandLine.Commands;

public class InteractiveCommandSettings : CommandSettings;

public class InteractiveCommand(ICommandManager commandManager, IQueryManager queryManager)
    : AsyncCommand<InteractiveCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InteractiveCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            int? selectedLogId = null;
            int? renderStartTop = null;
            var renderedLineCount = 0;
            var statusMessage = string.Empty;
            var logs = Array.Empty<TimeLogsResponseItem>();
            RenderLayout? renderLayout = null;
            var needsFullRender = true;

            while (true)
            {
                if (needsFullRender)
                {
                    logs = await LoadLogs();

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
                    renderLayout = RenderLogs(logs, selectedLogId.Value, statusMessage);
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
                    var newIndex = InteractiveViewLogic.NavigateUp(selectedIndex);
                    if (newIndex != selectedIndex)
                    {
                        if (renderLayout == null || !UpdateSelectedRows(renderLayout, logs, selectedIndex, newIndex))
                        {
                            needsFullRender = true;
                        }
                        selectedLogId = logs[newIndex].Id;
                    }
                    continue;
                }

                if (key == ConsoleKey.DownArrow)
                {
                    var newIndex = InteractiveViewLogic.NavigateDown(selectedIndex, logs.Length);
                    if (newIndex != selectedIndex)
                    {
                        if (renderLayout == null || !UpdateSelectedRows(renderLayout, logs, selectedIndex, newIndex))
                        {
                            needsFullRender = true;
                        }
                        selectedLogId = logs[newIndex].Id;
                    }
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

    private async Task<TimeLogsResponseItem[]> LoadLogs()
    {
        var response = await queryManager.Handle(new TimeLogsQuery());
        return InteractiveViewLogic.OrderLogs(response.Items);
    }

    private static RenderLayout RenderLogs(TimeLogsResponseItem[] logs, int selectedLogId, string statusMessage)
    {
        var startTop = Console.CursorTop;
        var lineCount = 0;
        var widths = InteractiveViewLogic.ComputeColumnWidths(logs);

        AnsiConsole.WriteLine();
        lineCount++;
        AnsiConsole.WriteLine(InteractiveViewLogic.BuildHeaderRow(widths));
        lineCount++;
        AnsiConsole.WriteLine(InteractiveViewLogic.BuildBorderRow(widths));
        lineCount++;

        foreach (var log in logs)
        {
            AnsiConsole.WriteLine(InteractiveViewLogic.BuildLogRow(widths, log, log.Id == selectedLogId));
            lineCount++;
        }

        AnsiConsole.WriteLine();
        lineCount++;
        AnsiConsole.MarkupLine("[grey]Use [bold]Up/Down[/] to move selection, [bold]e[/] to edit, [bold]d[/] to delete, [bold]r[/] to refresh, [bold]q[/] to quit.[/]");
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
            ClearWidth = Math.Max(Console.WindowWidth - 1, 1),
            Widths = widths
        };
    }

    // Returns false if cursor manipulation failed so the caller can fall back to a full re-render.
    private static bool UpdateSelectedRows(RenderLayout layout, TimeLogsResponseItem[] logs, int oldIndex, int newIndex)
    {
        if (Console.IsOutputRedirected)
        {
            return false;
        }

        try
        {
            RenderRow(layout, logs[oldIndex], oldIndex, false);
            RenderRow(layout, logs[newIndex], newIndex, true);
            Console.SetCursorPosition(0, Math.Min(layout.InputTop, Console.BufferHeight - 1));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RenderRow(RenderLayout layout, TimeLogsResponseItem log, int rowIndex, bool isSelected)
    {
        var rowTop = layout.RowStartTop + rowIndex;
        if (rowTop < 0 || rowTop >= Console.BufferHeight)
        {
            return;
        }

        Console.SetCursorPosition(0, rowTop);
        Console.Write(new string(' ', layout.ClearWidth));
        Console.SetCursorPosition(0, rowTop);
        Console.Write(InteractiveViewLogic.BuildLogRow(layout.Widths, log, isSelected));
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

    private static UpdateTimeLogCommand PromptEdit(TimeLogsResponseItem log)
    {
        var date = PromptDate("Date", log.Date);
        var time = PromptTime("Time", log.Time);

        var category = AnsiConsole.Prompt(
            new TextPrompt<string>("Category").DefaultValue(log.Category).AllowEmpty());
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
            Task = task,
            Description = description
        };
    }

    private static DateOnly PromptDate(string label, DateOnly defaultValue)
    {
        while (true)
        {
            var value = AnsiConsole.Prompt(
                new TextPrompt<string>($"{label} (yyyy-MM-dd)")
                    .DefaultValue(defaultValue.ToString("yyyy-MM-dd")));
            if (DateOnly.TryParse(value, out var date))
            {
                return date;
            }
            AnsiConsole.MarkupLine("[red]Invalid date. Use yyyy-MM-dd.[/]");
        }
    }

    private static TimeOnly PromptTime(string label, TimeOnly defaultValue)
    {
        while (true)
        {
            var value = AnsiConsole.Prompt(
                new TextPrompt<string>($"{label} (HH:mm:ss)")
                    .DefaultValue(defaultValue.ToString("HH:mm:ss")));
            if (TimeOnly.TryParse(value, out var time))
            {
                return time;
            }
            AnsiConsole.MarkupLine("[red]Invalid time. Use HH:mm:ss.[/]");
        }
    }

    private static bool PromptDeleteConfirmation(TimeLogsResponseItem log)
    {
        return AnsiConsole.Confirm($"Delete log {log.Id} at {log.Date:yyyy-MM-dd} {log.Time:HH\\:mm\\:ss}?", false);
    }

    private sealed record RenderLayout
    {
        public int LineCount { get; init; }
        public int RowStartTop { get; init; }
        public int InputTop { get; init; }
        public int ClearWidth { get; init; }
        public required ColumnWidths Widths { get; init; }
    }
}
