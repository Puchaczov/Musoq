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
///     Benchmark to measure the impact of Common Subexpression Elimination (CSE).
///     Compares queries with duplicate expressions vs queries without.
///     CSE should reduce execution time by caching computed values that are used multiple times
///     in the same row context (e.g., same expression in WHERE and SELECT).
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class CommonSubexpressionEliminationBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _queryCaseWhenNoDuplicate = null!;
    private CompiledQuery _queryCaseWhenWithDuplicateInSelect = null!;
    private CompiledQuery _queryCaseWhenWithDuplicateInWhere = null!;
    private CompiledQuery _queryWithDuplicateExpressions = null!;
    private CompiledQuery _queryWithNestedDuplicates = null!;
    private CompiledQuery _queryWithoutDuplicateExpressions = null!;
    private CompiledQuery _queryWithTripleDuplicates = null!;

    [Params(10_000, 100_000)] public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var testData = CreateTestData(RowsCount);
        var schemaProvider = new CseTestSchemaProvider(testData);


        _queryWithDuplicateExpressions = InstanceCreator.CompileForExecution(
            @"SELECT ExpensiveMethod(Value), Name 
              FROM #test.entities() 
              WHERE ExpensiveMethod(Value) > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryWithoutDuplicateExpressions = InstanceCreator.CompileForExecution(
            @"SELECT Value * 2, Name 
              FROM #test.entities() 
              WHERE ExpensiveMethod(Value) > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryWithTripleDuplicates = InstanceCreator.CompileForExecution(
            @"SELECT ExpensiveMethod(Value), ExpensiveMethod(Value) + 10, Name 
              FROM #test.entities() 
              WHERE ExpensiveMethod(Value) > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryWithNestedDuplicates = InstanceCreator.CompileForExecution(
            @"SELECT ExpensiveMethod(Value) * 2, ExpensiveMethod(Value) / 2 
              FROM #test.entities() 
              WHERE ExpensiveMethod(Value) > 50 AND ExpensiveMethod(Value) < 1000",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryCaseWhenWithDuplicateInSelect = InstanceCreator.CompileForExecution(
            @"SELECT ExpensiveMethod(Value), 
                     CASE WHEN ExpensiveMethod(Value) > 200 THEN 'High' ELSE 'Low' END
              FROM #test.entities()",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryCaseWhenWithDuplicateInWhere = InstanceCreator.CompileForExecution(
            @"SELECT Name, 
                     CASE WHEN ExpensiveMethod(Value) > 200 THEN 'High' ELSE 'Low' END
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryCaseWhenNoDuplicate = InstanceCreator.CompileForExecution(
            @"SELECT Name, 
                     CASE WHEN ExpensiveMethod(Value) > 200 THEN 'High' ELSE 'Low' END
              FROM #test.entities()",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);
    }

    /// <summary>
    ///     Baseline: No duplicate expressions.
    ///     ExpensiveMethod called once per row.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void Query_NoDuplicates()
    {
        _queryWithoutDuplicateExpressions.Run();
    }

    /// <summary>
    ///     Same expression in WHERE and SELECT (2x calls per row).
    ///     With CSE, should approach baseline performance.
    /// </summary>
    [Benchmark]
    public void Query_DuplicateInWhereAndSelect()
    {
        _queryWithDuplicateExpressions.Run();
    }

    /// <summary>
    ///     Same expression 3 times (WHERE + 2x in SELECT).
    ///     Without CSE: 3x the computation.
    ///     With CSE: Should be ~same as baseline.
    /// </summary>
    [Benchmark]
    public void Query_TripleDuplicates()
    {
        _queryWithTripleDuplicates.Run();
    }

    /// <summary>
    ///     Expression appears 4 times in different contexts.
    ///     Tests that CSE handles complex scenarios.
    /// </summary>
    [Benchmark]
    public void Query_NestedDuplicates()
    {
        _queryWithNestedDuplicates.Run();
    }

    /// <summary>
    ///     CASE WHEN baseline: ExpensiveMethod only inside CASE WHEN.
    ///     No CSE benefit expected - expression doesn't appear outside CASE WHEN.
    /// </summary>
    [Benchmark]
    public void Query_CaseWhen_NoDuplicate()
    {
        _queryCaseWhenNoDuplicate.Run();
    }

    /// <summary>
    ///     CASE WHEN with duplicate: ExpensiveMethod in SELECT and inside CASE WHEN.
    ///     With CSE: cached value passed to CaseWhen method as parameter.
    ///     Should be faster than NoDuplicate since expression is cached.
    /// </summary>
    [Benchmark]
    public void Query_CaseWhen_DuplicateInSelect()
    {
        _queryCaseWhenWithDuplicateInSelect.Run();
    }

    /// <summary>
    ///     CASE WHEN with duplicate: ExpensiveMethod in WHERE and inside CASE WHEN.
    ///     With CSE: cached value passed to CaseWhen method as parameter.
    /// </summary>
    [Benchmark]
    public void Query_CaseWhen_DuplicateInWhere()
    {
        _queryCaseWhenWithDuplicateInWhere.Run();
    }

    private static List<CseTestEntity> CreateTestData(int count)
    {
        return Enumerable.Range(0, count).Select(i => new CseTestEntity
        {
            Id = i,
            Name = $"Name{i}",
            Value = i % 500,
            Category = $"Category{i % 10}"
        }).ToList();
    }
}

/// <summary>
///     Library with intentionally expensive methods to measure CSE impact.
/// </summary>
public class CseTestLibrary : LibraryBase
{
    /// <summary>
    ///     Simulates an expensive computation that should be cached by CSE.
    ///     Uses CPU-intensive operations to make the performance difference measurable.
    /// </summary>
    [BindableMethod]
    public int ExpensiveMethod(int value)
    {
        double result = value;
        for (var i = 0; i < 500; i++) result = Math.Sqrt(result * result + i) + Math.Sin(i) * Math.Cos(i);
        return (int)result;
    }

    /// <summary>
    ///     Another expensive method for testing multiple CSE candidates.
    /// </summary>
    [BindableMethod]
    public string ExpensiveStringMethod(string value)
    {
        var result = value;
        for (var i = 0; i < 100; i++) result = result.ToUpper().ToLower();
        return result.ToUpper();
    }

    /// <summary>
    ///     Cheap method for comparison (should not benefit much from CSE).
    /// </summary>
    [BindableMethod]
    public int CheapMethod(int value)
    {
        return value * 2;
    }
}

public class CseTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class CseTestSchemaProvider : ISchemaProvider
{
    private readonly IReadOnlyCollection<CseTestEntity> _data;

    public CseTestSchemaProvider(IReadOnlyCollection<CseTestEntity> data)
    {
        _data = data;
    }

    public ISchema GetSchema(string schema)
    {
        return new CseTestSchema(_data);
    }
}

public class CseTestSchema : SchemaBase
{
    private readonly IReadOnlyCollection<CseTestEntity> _data;

    public CseTestSchema(IReadOnlyCollection<CseTestEntity> data)
        : base("test", CreateMethods())
    {
        _data = data;
    }

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        manager.RegisterLibraries(new CseTestLibrary());
        return new MethodsAggregator(manager);
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new CseTestTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new CseTestRowSource(_data);
    }
}

public class CseTestTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new SchemaColumn(nameof(CseTestEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(CseTestEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(CseTestEntity.Value), 2, typeof(int)),
        new SchemaColumn(nameof(CseTestEntity.Category), 3, typeof(string))
    };

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata => new(typeof(CseTestEntity));
}

public class CseTestRowSource : RowSource
{
    private readonly IReadOnlyCollection<CseTestEntity> _data;

    public CseTestRowSource(IReadOnlyCollection<CseTestEntity> data)
    {
        _data = data;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _data) yield return new CseTestEntityResolver(entity);
        }
    }
}

public class CseTestEntityResolver : IObjectResolver
{
    private readonly CseTestEntity _entity;

    public CseTestEntityResolver(CseTestEntity entity)
    {
        _entity = entity;
        Contexts = [entity];
    }

    public object[] Contexts { get; }

    public bool HasColumn(string name)
    {
        return name switch
        {
            nameof(CseTestEntity.Id) => true,
            nameof(CseTestEntity.Name) => true,
            nameof(CseTestEntity.Value) => true,
            nameof(CseTestEntity.Category) => true,
            _ => false
        };
    }

    public object this[string name] => name switch
    {
        nameof(CseTestEntity.Id) => _entity.Id,
        nameof(CseTestEntity.Name) => _entity.Name,
        nameof(CseTestEntity.Value) => _entity.Value,
        nameof(CseTestEntity.Category) => _entity.Category,
        _ => throw new KeyNotFoundException($"Column {name} not found")
    };

    public object this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.Value,
        3 => _entity.Category,
        _ => throw new IndexOutOfRangeException()
    };
}

// Note: SchemaColumn is defined in RegexPluginBenchmark.cs - reusing it here
