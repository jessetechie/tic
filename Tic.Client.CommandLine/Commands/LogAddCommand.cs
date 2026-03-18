using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Manager;

namespace Tic.Client.CommandLine.Commands;

public sealed class LogAddCommandSettings : CommandSettings
{
    [CommandOption("--date")]
    public string? Date { get; init; }

    [CommandOption("--time")]
    public string? Time { get; init; }

    [CommandOption("--category")]
    public string? Category { get; init; }

    [CommandOption("--task")]
    public string? Task { get; init; }

    [CommandOption("--description")]
    public string? Description { get; init; }
}

public sealed class LogAddCommand(ICommandManager commandManager) : AsyncCommand<LogAddCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, LogAddCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryParseDate(settings.Date, out var date, out var dateError))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(dateError)}[/]");
                return -1;
            }

            if (!TryParseTime(settings.Time, out var time, out var timeError))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(timeError)}[/]");
                return -1;
            }

            await commandManager.Handle(new AddTimeLogCommand
            {
                Date = date,
                Time = time,
                Category = settings.Category ?? string.Empty,
                Task = settings.Task ?? string.Empty,
                Description = settings.Description ?? string.Empty
            });

            AnsiConsole.MarkupLine("[green]Time log added.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to add time log: {Markup.Escape(ex.Message)}[/]");
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

    private static bool TryParseTime(string? value, out TimeOnly time, out string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            time = TimeOnly.FromDateTime(DateTime.Now);
            error = string.Empty;
            return true;
        }

        if (TimeOnly.TryParse(value, out time))
        {
            error = string.Empty;
            return true;
        }

        error = "Invalid --time value. Use a valid time like 09:30 or 09:30:00.";
        return false;
    }
}

