using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Client.CommandLine.UI;
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

    [CommandOption("--project")]
    public string? Project { get; init; }
    
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
            if (!Converter.TryParseDate(settings.Date, out var date, out var dateError))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(dateError)}[/]");
                return -1;
            }

            if (!Converter.TryParseTime(settings.Time, out var time, out var timeError))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(timeError)}[/]");
                return -1;
            }

            await commandManager.Handle(new AddTimeLogCommand
            {
                Date = date,
                Time = time,
                Category = settings.Category ?? string.Empty,
                Project = settings.Project ?? string.Empty,
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
}

