using Spectre.Console.Cli;
using Tic.Client.CommandLine;
using Tic.Client.CommandLine.Commands;

var app = Bootstrapper.BuildCommandApp();
app.SetDefaultCommand<DefaultCommand>();
app.Configure(config =>
{
    config.PropagateExceptions();

    config.AddBranch("log", log =>
    {
        log.AddCommand<LogAddCommand>("add")
            .WithDescription("Add a time log entry.");
        log.AddCommand<LogListCommand>("list")
            .WithDescription("List time log entries.");
    });

    config.AddBranch("summary", summary =>
    {
        summary.AddCommand<SummaryDayCommand>("day")
            .WithDescription("Display summary details for a day.");
    });

    config.AddCommand<InteractiveCommand>("interactive")
        .WithDescription("Start an interactive command line interface.");
});

app.Run(args);
return 0;