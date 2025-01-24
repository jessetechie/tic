namespace Tic.ResourceAccess;

public interface ISchemaFactory
{
    Task Init();
}

public interface IDatabaseInitializer
{
    Task Init();
}

public class SchemaFactory(IEnumerable<IDatabaseInitializer> initializers) : ISchemaFactory
{
    public async Task Init()
    {
        foreach (var initializer in initializers)
        {
            await initializer.Init();
        }
    }
}