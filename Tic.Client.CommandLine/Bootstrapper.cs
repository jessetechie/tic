using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Tic.Engine;
using Tic.Manager;
using Tic.ResourceAccess;

namespace Tic.Client.CommandLine;

public static class Bootstrapper
{
    public static CommandApp BuildCommandApp()
    {
        var registrar = BuildRegistrar();
        return new CommandApp(registrar);
    }

    private static TypeRegistrar BuildRegistrar()
    {
        var services = new ServiceCollection();
        
        services.Scan(scan => scan
            .FromAssemblyOf<SchemaFactory>()
            .AddClasses(classes => classes.AssignableTo<IDatabaseInitializer>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        );
        
        services.AddSingleton<IDataContext, DataContext>();
        services.AddSingleton<ILogResourceAccess, LogResourceAccess>();
        services.AddSingleton<ICategoryResourceAccess, CategoryResourceAccess>();
        services.AddSingleton<IIntervalResourceAccess, IntervalResourceAccess>();
        services.AddSingleton<ISummaryResourceAccess, SummaryResourceAccess>();
        services.AddSingleton<ISummaryCalculator, SummaryCalculator>();
        services.AddSingleton<ICommandManager, CommandManager>();
        services.AddSingleton<IQueryManager, QueryManager>();
        services.AddSingleton<SchemaFactory>();

        var registrar = new TypeRegistrar(services);
        var resolver = registrar.Build();
        
        var schemaFactory = (SchemaFactory)resolver.Resolve(typeof(SchemaFactory))!;
        schemaFactory.Init().Wait();
        
        return registrar;
    }
}

public sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar
{
    public void Register(Type service, Type implementation)
    {
        builder.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        builder.AddSingleton(service, implementation);
    }
    
    public void RegisterLazy(Type service, Func<object> factory)
    {
        builder.AddSingleton(service, factory);
    }
    
    public ITypeResolver Build()
    {
        return new TypeResolver(builder.BuildServiceProvider());
    }
}

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type)
    {
        return type == null ? null : provider.GetService(type);
    }
    
    public void Dispose()
    {
        if (provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}