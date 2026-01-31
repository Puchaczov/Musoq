using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmark comparing query execution with all optimizations enabled vs disabled.
///     This helps measure the cumulative performance impact of optimizations like CSE.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class OptimizationsToggleBenchmark
{
    // Compilation options
    private static readonly CompilationOptions OptimizationsEnabled = new(
        ParallelizationMode.Full);

    private static readonly CompilationOptions OptimizationsDisabled = new(
        ParallelizationMode.None,
        false,
        false,
        false);

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _aggregateQueryOptimized = null!;
    private CompiledQuery _aggregateQueryUnoptimized = null!;
    private CompiledQuery _caseWhenQueryOptimized = null!;
    private CompiledQuery _caseWhenQueryUnoptimized = null!;
    private CompiledQuery _complexQueryOptimized = null!;
    private CompiledQuery _complexQueryUnoptimized = null!;
    private CompiledQuery _heavyMixedQueryOptimized = null!;
    private CompiledQuery _heavyMixedQueryUnoptimized = null!;
    private CompiledQuery _mixedColumnMethodQueryOptimized = null!;
    private CompiledQuery _mixedColumnMethodQueryUnoptimized = null!;
    private CompiledQuery _simpleQueryOptimized = null!;
    private CompiledQuery _simpleQueryUnoptimized = null!;

    [Params(10_000, 100_000)] public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var testData = CreateTestData(RowsCount);
        var schemaProvider = new OptBenchSchemaProvider(testData);


        const string simpleQuery = @"
            SELECT ExpensiveCompute(Value), ExpensiveCompute(Value) + 10
            FROM #test.entities() 
            WHERE ExpensiveCompute(Value) > 100";

        _simpleQueryOptimized = InstanceCreator.CompileForExecution(
            simpleQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsEnabled);

        _simpleQueryUnoptimized = InstanceCreator.CompileForExecution(
            simpleQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsDisabled);


        const string complexQuery = @"
            SELECT 
                ExpensiveCompute(Value) as Computed,
                ExpensiveCompute(Value) * 2 as DoubleVal,
                ExpensiveCompute(Value) / 2 as HalfVal,
                StringTransform(Name) as NameTransformed,
                StringTransform(Name) + '_suffix' as NameWithSuffix
            FROM #test.entities() 
            WHERE ExpensiveCompute(Value) > 50 
              AND ExpensiveCompute(Value) < 500
              AND StringTransform(Name) IS NOT NULL";

        _complexQueryOptimized = InstanceCreator.CompileForExecution(
            complexQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsEnabled);

        _complexQueryUnoptimized = InstanceCreator.CompileForExecution(
            complexQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsDisabled);


        const string caseWhenQuery = @"
            SELECT 
                ExpensiveCompute(Value) as Computed,
                CASE 
                    WHEN ExpensiveCompute(Value) > 300 THEN 'High'
                    WHEN ExpensiveCompute(Value) > 100 THEN 'Medium'
                    ELSE 'Low'
                END as Category
            FROM #test.entities()
            WHERE ExpensiveCompute(Value) > 50";

        _caseWhenQueryOptimized = InstanceCreator.CompileForExecution(
            caseWhenQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsEnabled);

        _caseWhenQueryUnoptimized = InstanceCreator.CompileForExecution(
            caseWhenQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsDisabled);


        const string aggregateQuery = @"
            SELECT 
                Category,
                Sum(ExpensiveCompute(Value)) as TotalComputed,
                Avg(ExpensiveCompute(Value)) as AvgComputed,
                Count(ExpensiveCompute(Value)) as CountComputed
            FROM #test.entities()
            WHERE ExpensiveCompute(Value) > 0
            GROUP BY Category";

        _aggregateQueryOptimized = InstanceCreator.CompileForExecution(
            aggregateQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsEnabled);

        _aggregateQueryUnoptimized = InstanceCreator.CompileForExecution(
            aggregateQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsDisabled);


        const string mixedColumnMethodQuery = @"
            SELECT 
                Value,
                ExpensiveCompute(Value),
                Value + ExpensiveCompute(Value),
                Value * ExpensiveCompute(Value),
                Name,
                StringTransform(Name),
                Name + '_' + StringTransform(Name)
            FROM #test.entities() 
            WHERE Value > 100 
              AND ExpensiveCompute(Value) > 50
              AND Name IS NOT NULL";

        _mixedColumnMethodQueryOptimized = InstanceCreator.CompileForExecution(
            mixedColumnMethodQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsEnabled);

        _mixedColumnMethodQueryUnoptimized = InstanceCreator.CompileForExecution(
            mixedColumnMethodQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsDisabled);


        const string heavyMixedQuery = @"
            SELECT 
                Id,
                Value,
                Value * 2,
                Value + 100,
                ExpensiveCompute(Value),
                ExpensiveCompute(Value) * 2,
                ExpensiveCompute(Value) + Value,
                Name,
                StringTransform(Name),
                Category,
                CASE 
                    WHEN Value > 500 AND ExpensiveCompute(Value) > 1000 THEN 'VeryHigh'
                    WHEN Value > 200 AND ExpensiveCompute(Value) > 500 THEN 'High'
                    WHEN Value > 100 THEN 'Medium'
                    ELSE 'Low'
                END as Classification,
                Value + ExpensiveCompute(Value) + Value * 2
            FROM #test.entities() 
            WHERE Value > 50 
              AND ExpensiveCompute(Value) > 0
              AND Value < 900";

        _heavyMixedQueryOptimized = InstanceCreator.CompileForExecution(
            heavyMixedQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsEnabled);

        _heavyMixedQueryUnoptimized = InstanceCreator.CompileForExecution(
            heavyMixedQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            OptimizationsDisabled);
    }

    #region Test Data Creation

    private static List<OptBenchEntity> CreateTestData(int count)
    {
        var random = new Random(42);
        var categories = new[] { "A", "B", "C", "D", "E" };

        return Enumerable.Range(0, count)
            .Select(i => new OptBenchEntity
            {
                Id = i,
                Name = $"Entity_{i}",
                Value = random.Next(1, 1000),
                Category = categories[i % categories.Length]
            })
            .ToList();
    }

    #endregion

    #region Simple Query Benchmarks

    [Benchmark]
    public void SimpleQuery_AllOptimizationsEnabled()
    {
        _simpleQueryOptimized.Run();
    }

    [Benchmark]
    public void SimpleQuery_AllOptimizationsDisabled()
    {
        _simpleQueryUnoptimized.Run();
    }

    #endregion

    #region Complex Query Benchmarks

    [Benchmark]
    public void ComplexQuery_AllOptimizationsEnabled()
    {
        _complexQueryOptimized.Run();
    }

    [Benchmark]
    public void ComplexQuery_AllOptimizationsDisabled()
    {
        _complexQueryUnoptimized.Run();
    }

    #endregion

    #region CASE WHEN Query Benchmarks

    [Benchmark]
    public void CaseWhenQuery_AllOptimizationsEnabled()
    {
        _caseWhenQueryOptimized.Run();
    }

    [Benchmark]
    public void CaseWhenQuery_AllOptimizationsDisabled()
    {
        _caseWhenQueryUnoptimized.Run();
    }

    #endregion

    #region Aggregate Query Benchmarks

    [Benchmark]
    public void AggregateQuery_AllOptimizationsEnabled()
    {
        _aggregateQueryOptimized.Run();
    }

    [Benchmark]
    public void AggregateQuery_AllOptimizationsDisabled()
    {
        _aggregateQueryUnoptimized.Run();
    }

    #endregion

    #region Mixed Column and Method Query Benchmarks

    [Benchmark]
    public void MixedColumnMethodQuery_AllOptimizationsEnabled()
    {
        _mixedColumnMethodQueryOptimized.Run();
    }

    [Benchmark]
    public void MixedColumnMethodQuery_AllOptimizationsDisabled()
    {
        _mixedColumnMethodQueryUnoptimized.Run();
    }

    #endregion

    #region Heavy Mixed Query Benchmarks

    [Benchmark]
    public void HeavyMixedQuery_AllOptimizationsEnabled()
    {
        _heavyMixedQueryOptimized.Run();
    }

    [Benchmark]
    public void HeavyMixedQuery_AllOptimizationsDisabled()
    {
        _heavyMixedQueryUnoptimized.Run();
    }

    #endregion
}

#region Schema Implementation for OptimizationsToggleBenchmark

public class OptBenchEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class OptBenchSchemaProvider : ISchemaProvider
{
    private readonly List<OptBenchEntity> _data;

    public OptBenchSchemaProvider(List<OptBenchEntity> data)
    {
        _data = data;
    }

    public ISchema GetSchema(string schema)
    {
        return new OptBenchSchema(_data);
    }
}

public class OptBenchSchema : SchemaBase
{
    private readonly List<OptBenchEntity> _data;

    public OptBenchSchema(List<OptBenchEntity> data)
        : base("test", CreateMethods())
    {
        _data = data;
    }

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        manager.RegisterLibraries(new OptBenchLibrary());
        return new MethodsAggregator(manager);
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new OptBenchTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new OptBenchRowSource(_data);
    }
}

public class OptBenchTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new SchemaColumn(nameof(OptBenchEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(OptBenchEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(OptBenchEntity.Value), 2, typeof(int)),
        new SchemaColumn(nameof(OptBenchEntity.Category), 3, typeof(string))
    };

    public SchemaTableMetadata Metadata => new(typeof(OptBenchEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }
}

public class OptBenchRowSource : RowSource
{
    private readonly List<OptBenchEntity> _data;

    public OptBenchRowSource(List<OptBenchEntity> data)
    {
        _data = data;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _data) yield return new OptBenchEntityResolver(entity);
        }
    }
}

public class OptBenchEntityResolver : IObjectResolver
{
    private readonly OptBenchEntity _entity;

    public OptBenchEntityResolver(OptBenchEntity entity)
    {
        _entity = entity;
        Contexts = [entity];
    }

    public object[] Contexts { get; }

    public object? this[string name] => name switch
    {
        nameof(OptBenchEntity.Id) => _entity.Id,
        nameof(OptBenchEntity.Name) => _entity.Name,
        nameof(OptBenchEntity.Value) => _entity.Value,
        nameof(OptBenchEntity.Category) => _entity.Category,
        _ => null
    };

    public object? this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.Value,
        3 => _entity.Category,
        _ => null
    };

    public bool HasColumn(string name)
    {
        return name is
            nameof(OptBenchEntity.Id) or
            nameof(OptBenchEntity.Name) or
            nameof(OptBenchEntity.Value) or
            nameof(OptBenchEntity.Category);
    }
}

public class OptBenchLibrary : LibraryBase
{
    /// <summary>
    ///     Simulates an expensive computation (e.g., complex math, parsing, etc.)
    /// </summary>
    [BindableMethod]
    public decimal ExpensiveCompute(int value)
    {
        decimal result = value;
        for (var i = 0; i < 100; i++) result = result * 1.1m + (decimal)Math.Sin(i);
        return Math.Round(result, 2);
    }

    /// <summary>
    ///     Simulates an expensive string transformation
    /// </summary>
    [BindableMethod]
    public string? StringTransform(string? input)
    {
        if (input == null) return null;


        var result = input;
        for (var i = 0; i < 50; i++) result = result.ToUpper().ToLower();
        return result.ToUpperInvariant() + "_transformed";
    }
}

#endregion
