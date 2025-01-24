using Spectre.Console;
using Spectre.Console.Cli;
using Tic.Manager;

namespace Tic.Client.CommandLine.Commands;

public class InteractiveCommandSettings : CommandSettings;

public class InteractiveCommand(ICommandManager commandManager, IQueryManager queryManager) 
    : AsyncCommand<InteractiveCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InteractiveCommandSettings settings)
    {
        var logsResponse = await queryManager.Handle(new TimeLogsQuery
        {
            DateRange = new Tuple<DateOnly, DateOnly>(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                DateOnly.FromDateTime(DateTime.Today))
        });
        
        foreach (var log in logsResponse.Items)
        {
            AnsiConsole.MarkupLine($"[bold]{log.Time}[/] [green]{log.Description}[/]");
        }
        
        var fruit = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]What would you like to do?[/]")
                .AddChoices("Add Logs", "Edit Logs"));
     
        AnsiConsole.WriteLine($"I agree. {fruit} is tasty!");
        
        return 0;
    }

}
