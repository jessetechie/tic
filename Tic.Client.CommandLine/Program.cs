// See https://aka.ms/new-console-template for more information

using Spectre.Console;

Console.WriteLine("Hello, World!");

AnsiConsole.Background = Color.Aqua;
AnsiConsole.Markup("[bold red]Hello[/] [yellow]World[/]");

AnsiConsole.Markup("[bold yellow on blue]Hello[/]");
AnsiConsole.Markup("[default on blue]World[/]");

AnsiConsole.Markup("Hello :globe_showing_europe_africa:!");

