using Tik.Engine;
using Tik.ResourceAccess;

namespace Tik.Manager;

public interface ICommandManager
{
    Task Handle(AddCategoryCommand command);
    Task Handle(EditCategoryCommand command);
    
    Task Handle(AddTimeLogCommand command);
    Task Handle(UpdateTimeLogCommand command);
    Task Handle(DeleteTimeLogCommand command);
}

public record AddCategoryCommand
{
    public string Name { get; init; } = string.Empty;
    public string ForegroundColorHex { get; init; } = string.Empty;
    public string BackgroundColorHex { get; init; } = string.Empty;
}

public record EditCategoryCommand
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ForegroundColorHex { get; init; } = string.Empty;
    public string BackgroundColorHex { get; init; } = string.Empty;
    public bool IsInactive { get; init; }
}

public abstract record TimeLogCommand
{
    public DateOnly Date { get; init; }
    public TimeOnly Time { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Project { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record AddTimeLogCommand : TimeLogCommand;

public record UpdateTimeLogCommand : TimeLogCommand
{
    public int Id { get; init; }
}

public record DeleteTimeLogCommand
{
    public int Id { get; init; }
}

public class CommandManager(
    ICategoryResourceAccess categoryResourceAccess,
    ILogResourceAccess logResourceAccess,
    IIntervalResourceAccess intervalResourceAccess,
    ISummaryCalculator summaryCalculator)
    : ICommandManager
{
    public async Task Handle(AddCategoryCommand command)
    {
        var result = await categoryResourceAccess.Handle(new AddCategory
        {
            Name = command.Name,
            ForegroundColorHex = command.ForegroundColorHex,
            BackgroundColorHex = command.BackgroundColorHex,
            IsInactive = false
        });
        
        if (!result.IsSuccess)
        {
            throw new Exception("Failed to add category");
        }
    }
    
    public async Task Handle(EditCategoryCommand command)
    {
        var result = await categoryResourceAccess.Handle(new EditCategory
        {
            Id = command.Id,
            Name = command.Name,
            ForegroundColorHex = command.ForegroundColorHex,
            BackgroundColorHex = command.BackgroundColorHex,
            IsInactive = command.IsInactive
        });
        
        if (!result.IsSuccess)
        {
            throw new Exception("Failed to edit category");
        }
    }
    
    public async Task Handle(AddTimeLogCommand command)
    {
        var result = await logResourceAccess.Handle(new AddTimeLog
        {
            Date = command.Date,
            Time = command.Time,
            Category = command.Category,
            Project = command.Project,
            Task = command.Task,
            Description = command.Description
        });
        
        if (!result.IsSuccess)
        {
            throw new Exception("Failed to add time log");
        }
        
        await summaryCalculator.Handle(new CalculateLogIntervals
        {
            Date = command.Date
        });
        
        await summaryCalculator.Handle(new CalculateDaySummary
        {
            Date = command.Date
        });
    }
    
    public async Task Handle(UpdateTimeLogCommand command)
    {
        var result = await logResourceAccess.Handle(new EditTimeLog
        {
            Id = command.Id,
            Date = command.Date,
            Time = command.Time,
            Category = command.Category,
            Project = command.Project,
            Task = command.Task,
            Description = command.Description
        });
        
        if (!result.IsSuccess)
        {
            throw new Exception("Failed to update time log");
        }
        
        await summaryCalculator.Handle(new CalculateLogIntervals
        {
            Date = command.Date
        });
        
        await summaryCalculator.Handle(new CalculateDaySummary
        {
            Date = command.Date
        });
    }
    
    public async Task Handle(DeleteTimeLogCommand command)
    {
        var logs = await logResourceAccess.Handle(new TimeLogsRequest
        {
            Ids = [command.Id]
        });
        
        if (logs.Items.Length == 0)
        {
            throw new Exception("Time log not found");
        }

        var log = logs.Items.Single();
        
        var result = await logResourceAccess.Handle(new DeleteTimeLog
        {
            Id = command.Id
        });
        
        if (!result.IsSuccess)
        {
            throw new Exception("Failed to delete time log");
        }
        
        await intervalResourceAccess.Handle(new DeleteIntervals
        {
            LogIds = [log.Id]
        });
        
        await summaryCalculator.Handle(new CalculateLogIntervals
        {
            Date = log.Date
        });
        
        await summaryCalculator.Handle(new CalculateDaySummary
        {
            Date = log.Date
        });
    }
}