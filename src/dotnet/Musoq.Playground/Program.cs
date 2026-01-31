using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Playground;

public class Program
{
    public static void Main(string[] args)
    {
        var directScript2 = @"
select a.Id, a.Name
from #test.entities() a
inner join #test.entities() b on a.Id = b.Id";

        var directScript4 = @"
select a.Id, a.Name
from #test.entities() a
inner join #test.entities() b on a.Id = b.Id
inner join #test.entities() c on a.Id = c.Id
inner join #test.entities() d on a.Id = d.Id";

        var cteScript4 = @"
with cte1 as (select Id, Name from #test.entities()),
cte2 as (select Id, Name from #test.entities()),
cte3 as (select Id, Name from #test.entities()),
cte4 as (select Id, Name from #test.entities())
select a.Id, a.Name
from cte1 a
inner join cte2 b on a.Id = b.Id
inner join cte3 c on a.Id = c.Id
inner join cte4 d on a.Id = d.Id";

        var entities = Enumerable.Range(0, 100).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Name{i}",
            Population = i
        }).ToList();

        var provider = new NonEquiSchemaProvider(entities, 0);

        Console.WriteLine("=== Testing Join Performance ===");
        Console.WriteLine();


        Console.WriteLine("--- DIRECT JOIN (2 tables, 10k rows each) ---");
        ExpensiveCteCounter.Reset();
        var options = new CompilationOptions(
            useHashJoin: true,
            useSortMergeJoin: true);

        var query2 = InstanceCreator.CompileForExecution(
            directScript2,
            Guid.NewGuid().ToString(),
            provider,
            new MyLoggerResolver(),
            options);

        var sw = Stopwatch.StartNew();
        var result2 = query2.Run();
        sw.Stop();
        Console.WriteLine(
            $"  Time: {sw.ElapsedMilliseconds}ms, Rows: {result2.Count}, Enumerations: {ExpensiveCteCounter.GetCount()}");


        Console.WriteLine("--- DIRECT JOIN (4 tables, 10k rows each) ---");
        ExpensiveCteCounter.Reset();

        var query4 = InstanceCreator.CompileForExecution(
            directScript4,
            Guid.NewGuid().ToString(),
            provider,
            new MyLoggerResolver(),
            options);

        sw.Restart();
        var result4 = query4.Run();
        sw.Stop();
        Console.WriteLine(
            $"  Time: {sw.ElapsedMilliseconds}ms, Rows: {result4.Count}, Enumerations: {ExpensiveCteCounter.GetCount()}");


        Console.WriteLine("--- CTE JOIN (4 CTEs, 10k rows each) ---");
        ExpensiveCteCounter.Reset();

        var queryCte = InstanceCreator.CompileForExecution(
            cteScript4,
            Guid.NewGuid().ToString(),
            provider,
            new MyLoggerResolver(),
            options);

        sw.Restart();
        var resultCte = queryCte.Run();
        sw.Stop();
        Console.WriteLine(
            $"  Time: {sw.ElapsedMilliseconds}ms, Rows: {resultCte.Count}, Enumerations: {ExpensiveCteCounter.GetCount()}");
    }
}

public static class ExpensiveCteCounter
{
    private static int _counter;

    public static int Increment()
    {
        return Interlocked.Increment(ref _counter);
    }

    public static void Reset()
    {
        _counter = 0;
    }

    public static int GetCount()
    {
        return _counter;
    }
}

public class NonEquiEntity
{
    public string Name { get; set; } = string.Empty;
    public int Population { get; set; }
    public int Id { get; set; }
}

public class NonEquiSchemaProvider : ISchemaProvider
{
    private readonly IEnumerable<NonEquiEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public NonEquiSchemaProvider(IEnumerable<NonEquiEntity> entities, int simulatedWorkIterations = 0)
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    public ISchema GetSchema(string schema)
    {
        return new NonEquiSchema(_entities, _simulatedWorkIterations);
    }
}

public class NonEquiSchema : SchemaBase
{
    private readonly IEnumerable<NonEquiEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public NonEquiSchema(IEnumerable<NonEquiEntity> entities, int simulatedWorkIterations = 0)
        : base("test", CreateLibrary())
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new NonEquiTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new ExpensiveRowSource(_entities, _simulatedWorkIterations);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        var lib = new Library();
        methodManager.RegisterLibraries(lib);
        return new MethodsAggregator(methodManager);
    }
}

public class ExpensiveRowSource : RowSource
{
    private readonly IEnumerable<NonEquiEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public ExpensiveRowSource(IEnumerable<NonEquiEntity> entities, int simulatedWorkIterations)
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            var enumId = ExpensiveCteCounter.Increment();
            Console.WriteLine($"  [Enum {enumId}] Thread {Thread.CurrentThread.ManagedThreadId} starting enumeration");

            if (_simulatedWorkIterations > 0) SimulateWork(_simulatedWorkIterations);

            foreach (var entity in _entities) yield return new EntityResolver(entity);
        }
    }

    private static void SimulateWork(int iterations)
    {
        var result = 0.0;
        for (var i = 0; i < iterations; i++) result += Math.Sin(i) * Math.Cos(i);
        GC.KeepAlive(result);
    }
}

public class EntityResolver : IObjectResolver
{
    private readonly NonEquiEntity _entity;

    public EntityResolver(NonEquiEntity entity)
    {
        _entity = entity;
        Contexts = [entity];
    }

    public object[] Contexts { get; }

    public object this[string name] => name switch
    {
        "Id" => _entity.Id,
        "Name" => _entity.Name,
        "Population" => _entity.Population,
        _ => throw new ArgumentException($"Unknown column: {name}")
    };

    public object this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.Population,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public bool HasColumn(string name)
    {
        return name is "Id" or "Name" or "Population";
    }
}

public class Library : LibraryBase
{
}

public class NonEquiTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new SchemaColumn(nameof(NonEquiEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(NonEquiEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(NonEquiEntity.Population), 2, typeof(int))
    };

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.Single(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(NonEquiEntity));
}

public class MyLoggerResolver : ILoggerResolver
{
    public ILogger ResolveLogger()
    {
        return new NoOpLogger();
    }

    public ILogger<T> ResolveLogger<T>()
    {
        return new NoOpLogger<T>();
    }
}

public class NoOpLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}

public class NoOpLogger<T> : NoOpLogger, ILogger<T>
{
}
