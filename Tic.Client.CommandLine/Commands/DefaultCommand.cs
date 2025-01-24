using Tic.Manager;

namespace Tic.Client.CommandLine.Commands;

public class DefaultCommand(ICommandManager commandManager, IQueryManager queryManager) 
    : InteractiveCommand(commandManager, queryManager);
