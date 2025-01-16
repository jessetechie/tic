namespace Tic.Shared;

public class CommandResult
{
    public static CommandResult Success => new();
    public static CommandResult Error(params string[] messages) => new() { Messages = messages };
    public string[] Messages { get; init; } = [];
    
    public bool IsSuccess => Messages.Length == 0;
}