using Tik.Manager;

namespace Tik.Client.CommandLine.Commands;

public class DefaultCommand(ICommandManager commandManager, IQueryManager queryManager) 
    : InteractiveCommand(commandManager, queryManager);
