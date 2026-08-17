using System.Collections.Concurrent;
using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Runtime;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

public enum RuntimeDynamicBenchmarkScenario
{
    Root,
    Nested
}

[MemoryDiagnoser]
public class RuntimeDynamicExecutionBenchmark
{
    private static readonly Column[] ResultColumns =
    [
        new Column("Value", typeof(double), 0)
    ];

    // Keep enough surviving rows to amortize the fixed CompiledQuery admission cost
    // while still exercising a runtime root filter and projection.
    private const string RootQuery =
        "select Metric from #runtime.events() where RuntimeKey % 2 = 0";
    private const string NestedQuery =
        "select Branch.Measurement from #runtime.events() where Branch is not null";

    private readonly BenchmarkLoggerResolver _loggerResolver = new();
    private IReadOnlyList<BenchmarkRuntimeDynamicRow> _rows = null!;
    private RuntimeDynamicBenchmarkSchemaProvider _provider = null!;
    private CompiledQuery _warmQuery = null!;
    private CompiledQuery _warmHandwrittenQuery = null!;

    [Params(1_000, 10_000)]
    public int RowsCount { get; set; }

    [Params(RuntimeDynamicBenchmarkScenario.Root, RuntimeDynamicBenchmarkScenario.Nested)]
    public RuntimeDynamicBenchmarkScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = Enumerable.Range(0, RowsCount)
            .Select(index => new BenchmarkRuntimeDynamicRow(
                index,
                index % 4 == 0 ? null : new BenchmarkRuntimeDynamicBranch(index + 0.5)))
            .ToArray();
        _provider = new RuntimeDynamicBenchmarkSchemaProvider(_rows);
        _warmQuery = Compile();
        _warmHandwrittenQuery = new CompiledQuery(
            new BenchmarkRuntimeDynamicHandwrittenRunnable(this, _provider, _loggerResolver));

        // Prime both Roslyn-generated and handwritten DLR call sites. The hot cohort
        // measures steady-state row scanning, not first-use binder construction.
        _ = _warmQuery.Run(CancellationToken.None).Count;
        _ = _warmHandwrittenQuery.Run(CancellationToken.None).Count;
    }

    [Benchmark(Description = "Dynamic generated execution: cold compilation")]
    [BenchmarkCategory("ColdCompilation")]
    public CompiledQuery CompileCold() => Compile();

    [Benchmark(Description = "Dynamic generated execution: cache-hit compilation")]
    [BenchmarkCategory("CacheHitCompilation")]
    public CompiledQuery CompileCacheHit() =>
        InstanceCreator.CompileForExecution(
            Query,
            "RuntimeDynamicExecutionBenchmark.CacheHit",
            _provider,
            _loggerResolver,
            new CompilationOptions(ParallelizationMode.None, usePrimitiveTypeValidation: false));

    [Benchmark(Description = "Dynamic generated execution: hot materialization")]
    [BenchmarkCategory("HotExecution")]
    public int GeneratedHotExecution() => _warmQuery.Run().Count;

    [Benchmark(Description = "Handwritten DLR loop: hot materialization")]
    [BenchmarkCategory("HotExecution")]
    public int HandwrittenDlrHotExecution() =>
        _warmHandwrittenQuery.Run(CancellationToken.None).Count;

    internal Table ExecuteHandwrittenDlr(CancellationToken token)
    {
        var table = QueryRows.DeferredTable<BenchmarkRuntimeDynamicResultRow>(
            "result",
            ResultColumns,
            HandwrittenRows,
            token);
        return table;
    }

    private IEnumerable<BenchmarkRuntimeDynamicResultRow> HandwrittenRows(CancellationToken token)
    {
        foreach (var shape in HandwrittenShapes(token))
            yield return new BenchmarkRuntimeDynamicResultRow(shape.Value);
    }

    private IEnumerable<BenchmarkRuntimeDynamicShape> HandwrittenShapes(CancellationToken token)
    {
        // Keep the handwritten loop's iterator lifetime equivalent to the generated
        // shape iterator, which has a phase-boundary finally around the scan.
        try
        {
            foreach (var chunk in CreateSourceChunks(token))
            {
                for (var index = 0; index < chunk.Count; index++)
                {
                    if ((index & 1023) == 0)
                        token.ThrowIfCancellationRequested();

                    var row = chunk[index];
                    if (Scenario == RuntimeDynamicBenchmarkScenario.Root)
                    {
                        if ((int)(object)((dynamic)row).RuntimeKey % 2 == 0)
                            yield return new BenchmarkRuntimeDynamicShape((double)(object)((dynamic)row).Metric);
                        continue;
                    }

                    if ((BenchmarkRuntimeDynamicBranch?)(object)((dynamic)row).Branch is { } branch)
                    {
                        yield return new BenchmarkRuntimeDynamicShape((double)(object)((dynamic)branch).Measurement);
                    }
                }
            }
        }
        finally
        {
        }
    }

    private IEnumerable<IReadOnlyList<BenchmarkRuntimeDynamicRow>> CreateSourceChunks(CancellationToken token)
    {
        var schema = _provider.GetSchema("#runtime");
        var table = new RuntimeDynamicBenchmarkTable();
        var source = schema.GetRowSource<BenchmarkRuntimeDynamicRow>(
            "events",
            new SourceExecutionContext(
                "runtime-dynamic-benchmark",
                SourceExecutionPlan.Empty(SourceIdentity.Empty),
                token,
                table.Columns,
                new Dictionary<string, string>(),
                _loggerResolver.ResolveLogger()),
            Array.Empty<object>());

        return source.Chunks;
    }

    private string Query => Scenario == RuntimeDynamicBenchmarkScenario.Root ? RootQuery : NestedQuery;

    private CompiledQuery Compile() =>
        InstanceCreator.CompileForExecution(
            Query,
            Guid.NewGuid().ToString(),
            _provider,
            _loggerResolver,
            new CompilationOptions(ParallelizationMode.None, usePrimitiveTypeValidation: false));
}

public sealed class BenchmarkRuntimeDynamicHandwrittenRunnable : ITableRunnable, IParameterizedRunnable
{
    private readonly RuntimeDynamicExecutionBenchmark _benchmark;

    public BenchmarkRuntimeDynamicHandwrittenRunnable(
        RuntimeDynamicExecutionBenchmark benchmark,
        ISchemaProvider provider,
        ILoggerResolver loggerResolver)
    {
        _benchmark = benchmark;
        Provider = provider;
        Logger = loggerResolver.ResolveLogger();

        SourceRuntimeSettingsBySourceContextId = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["runtime-dynamic-benchmark:1"] = new Dictionary<string, string>()
        };
        SourceRuntimeSettingDescriptionsBySourceContextId = new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>
        {
            ["runtime-dynamic-benchmark:1"] = Array.Empty<SourceRuntimeSettingDescription>()
        };
        SourceExecutionPlans = new Dictionary<string, SourceExecutionPlan>
        {
            ["runtime-dynamic-benchmark:1"] = SourceExecutionPlan.Empty(SourceIdentity.Empty)
        };
    }

    public ISchemaProvider Provider { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }

    public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }

    public ILogger Logger { get; set; }

    public event QueryPhaseEventHandler PhaseChanged
    {
        add { }
        remove { }
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add { }
        remove { }
    }

    public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();

    public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

    public Table Run(CancellationToken token) => _benchmark.ExecuteHandwrittenDlr(token);
}

public sealed class BenchmarkRuntimeDynamicRow : DynamicObject
{
    private readonly ConcurrentDictionary<string, object?> _values;

    public BenchmarkRuntimeDynamicRow(int key, BenchmarkRuntimeDynamicBranch? branch)
    {
        _values = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["RuntimeKey"] = key,
            ["Metric"] = (double)key,
            ["Branch"] = branch
        };
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result) =>
        _values.TryGetValue(binder.Name, out result);
}

public sealed class BenchmarkRuntimeDynamicBranch(double measurement) : DynamicObject
{
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (binder.Name.Equals("Measurement", StringComparison.Ordinal))
        {
            result = measurement;
            return true;
        }

        result = null;
        return false;
    }
}

public sealed class BenchmarkRuntimeDynamicResultRow(double value) : Row
{
    public double Value { get; } = value;

    public override int Count => 1;

    public override object this[int columnNumber] => columnNumber == 0
        ? Value
        : throw new IndexOutOfRangeException();
}

public sealed class BenchmarkRuntimeDynamicShape(double value)
{
    public double Value { get; } = value;
}

public sealed class RuntimeDynamicBenchmarkSchemaProvider(
    IReadOnlyList<BenchmarkRuntimeDynamicRow> rows) : ISchemaProvider
{
    public ISchema GetSchema(string schema) => new RuntimeDynamicBenchmarkSchema(rows);
}

public sealed class RuntimeDynamicBenchmarkSchema(IReadOnlyList<BenchmarkRuntimeDynamicRow> rows)
    : SchemaBase("runtime", CreateMethods())
{
    private static readonly ISchemaColumn[] Columns =
    [
        new SchemaColumn("RuntimeKey", 0, typeof(int)),
        new SchemaColumn("Metric", 1, typeof(double)),
        new SchemaColumn("Branch", 2, typeof(BenchmarkRuntimeDynamicBranch)),
        new SchemaColumn("Branch.Measurement", 3, typeof(double))
    ];

    private static MethodsAggregator CreateMethods() => new(new MethodsManager());

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters) => new RuntimeDynamicBenchmarkTable();

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext) =>
        methodName.Equals("events", StringComparison.OrdinalIgnoreCase)
            ? [new SchemaMethodInfo(methodName, ConstructorInfo.Empty())]
            : [];

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters) =>
        typeof(T) == typeof(BenchmarkRuntimeDynamicRow)
            ? (RowSource<T>)(object)new BenchmarkRuntimeDynamicRowSource(rows)
            : throw new InvalidOperationException(typeof(T).FullName);
}

public sealed class RuntimeDynamicBenchmarkTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn("RuntimeKey", 0, typeof(int)),
        new SchemaColumn("Metric", 1, typeof(double)),
        new SchemaColumn("Branch", 2, typeof(BenchmarkRuntimeDynamicBranch)),
        new SchemaColumn("Branch.Measurement", 3, typeof(double))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(BenchmarkRuntimeDynamicRow));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.FirstOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

public sealed class BenchmarkRuntimeDynamicRowSource : RowSource<BenchmarkRuntimeDynamicRow>
{
    private readonly IReadOnlyList<IReadOnlyList<BenchmarkRuntimeDynamicRow>> _chunks;

    public BenchmarkRuntimeDynamicRowSource(IReadOnlyList<BenchmarkRuntimeDynamicRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _chunks = [new RowChunk<BenchmarkRuntimeDynamicRow>(rows, 0, rows.Count)];
    }

    public override IEnumerable<IReadOnlyList<BenchmarkRuntimeDynamicRow>> Chunks => _chunks;
}
