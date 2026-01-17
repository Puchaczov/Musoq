using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmark to measure performance of regex-based plugin methods: Match, RegexReplace, RegexMatches.
///     Tests the impact of regex caching for repeated pattern usage.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class RegexPluginBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _baselineQuery = null!;
    private CompiledQuery _matchQuery = null!;
    private CompiledQuery _regexMatchesQuery = null!;
    private CompiledQuery _regexReplaceQuery = null!;

    [GlobalSetup]
    public void Setup()
    {
        var testData = Enumerable.Range(0, 1000).Select(i => new TestEntity
        {
            Id = i,
            Name = $"User{i}",
            City = $"City{i % 50}",
            Email = $"user{i}@example.com",
            Description = $"This is item number {i} with value {i * 10} and code ABC-{i:D4}"
        }).ToList();

        var schemaProvider = new TestSchemaProvider(testData);


        _baselineQuery = InstanceCreator.CompileForExecution(
            @"select Name from #test.entities() where Name = 'User500'",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _matchQuery = InstanceCreator.CompileForExecution(
            @"select Name from #test.entities() where Match('\d{3}', Description)",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _regexReplaceQuery = InstanceCreator.CompileForExecution(
            @"select RegexReplace(Description, 'ABC-\d{4}', 'CODE-XXXX') from #test.entities()",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _regexMatchesQuery = InstanceCreator.CompileForExecution(
            @"select RegexMatches('\d+', Description) from #test.entities()",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);
    }

    [Benchmark(Baseline = true)]
    public void Baseline_EqualityFilter_1000Rows()
    {
        _baselineQuery.Run();
    }

    [Benchmark]
    public void Match_PatternMatching_1000Rows()
    {
        _matchQuery.Run();
    }

    [Benchmark]
    public void RegexReplace_PatternReplacement_1000Rows()
    {
        _regexReplaceQuery.Run();
    }

    [Benchmark]
    public void RegexMatches_FindAllMatches_1000Rows()
    {
        _regexMatchesQuery.Run();
    }
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class TestSchemaProvider : ISchemaProvider
{
    private readonly List<TestEntity> _entities;

    public TestSchemaProvider(List<TestEntity> entities)
    {
        _entities = entities;
    }

    public ISchema GetSchema(string schema)
    {
        return new TestSchema(_entities);
    }
}

public class TestSchema : SchemaBase
{
    private readonly List<TestEntity> _entities;

    public TestSchema(List<TestEntity> entities) : base("test", CreateMethods())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new TestTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new TestRowSource(_entities);
    }

    private static MethodsAggregator CreateMethods()
    {
        var methodManager = new MethodsManager();
        methodManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodManager);
    }
}

public class TestTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new SchemaColumn("Id", 0, typeof(int)),
        new SchemaColumn("Name", 1, typeof(string)),
        new SchemaColumn("City", 2, typeof(string)),
        new SchemaColumn("Email", 3, typeof(string)),
        new SchemaColumn("Description", 4, typeof(string))
    };

    public SchemaTableMetadata Metadata => new(typeof(TestEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }
}

public class TestRowSource : RowSource
{
    private readonly List<TestEntity> _entities;

    public TestRowSource(List<TestEntity> entities)
    {
        _entities = entities;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _entities) yield return new TestObjectResolver(entity);
        }
    }
}

public class TestObjectResolver : IObjectResolver
{
    private readonly TestEntity _entity;

    public TestObjectResolver(TestEntity entity)
    {
        _entity = entity;
    }

    public object[] Contexts => Array.Empty<object>();

    public bool HasColumn(string name)
    {
        return name switch
        {
            "Id" or "Name" or "City" or "Email" or "Description" => true,
            _ => false
        };
    }

    public object this[string name] => name switch
    {
        "Id" => _entity.Id,
        "Name" => _entity.Name,
        "City" => _entity.City,
        "Email" => _entity.Email,
        "Description" => _entity.Description,
        _ => throw new KeyNotFoundException($"Column '{name}' not found")
    };

    public object this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.City,
        3 => _entity.Email,
        4 => _entity.Description,
        _ => throw new IndexOutOfRangeException($"Index {index} is out of range")
    };
}

public class SchemaColumn : ISchemaColumn
{
    public SchemaColumn(string columnName, int columnIndex, Type columnType)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        ColumnType = columnType;
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public Type ColumnType { get; }
}