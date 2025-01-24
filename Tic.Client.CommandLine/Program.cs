using Spectre.Console.Cli;
using Tic.Client.CommandLine;
using Tic.Client.CommandLine.Commands;

var app = Bootstrapper.BuildCommandApp();
app.SetDefaultCommand<DefaultCommand>();
app.Configure(config =>
{
    config.PropagateExceptions();
    config.AddCommand<InteractiveCommand>("interactive")
        .WithDescription("Start an interactive command line interface.");
});

app.Run(args);
return 0;