using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
/// Measures dynamic LIKE after a correlated property APPLY. The two methods isolate
/// a pattern bound by the inner row from a pattern stable for one outer row.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DynamicLikeApplyBenchmark : IDisposable
{
    private const int DefaultOuterRowCount = 1_024;
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private readonly DynamicLikeApplyRecorder _recorder = new();
    private CompiledQuery _innerPattern = null!;
    private CompiledQuery _outerPattern = null!;

    [Params(1, 8, 64)]
    public int FanOut { get; set; }

    internal int OuterRowCount { get; set; } = DefaultOuterRowCount;

    public string InnerPatternResultHash { get; private set; } = string.Empty;

    public string OuterPatternResultHash { get; private set; } = string.Empty;

    public int ExpectedResultCount => OuterRowCount * FanOut;

    [GlobalSetup]
    public void Setup()
    {
        var rows = CreateRows(OuterRowCount, FanOut, _recorder);
        var provider = new DynamicLikeApplySchemaProvider(rows, _recorder);
        var options = BenchmarkCompilationOptions.Materialized(
            new CompilationOptions(ParallelizationMode.None));
        _innerPattern = Compile(InnerPatternQuery, provider, options);
        _outerPattern = Compile(OuterPatternQuery, provider, options);

        ResetCounters();
        using (var result = _innerPattern.Run())
        {
            ValidateResult(result);
            InnerPatternResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
        }

        ResetCounters();
        using (var result = _outerPattern.Run())
        {
            ValidateResult(result);
            OuterPatternResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
        }

        ResetCounters();
    }

    [Benchmark(Baseline = true)]
    public Table DynamicLikeApply_InnerPattern() => _innerPattern.Run();

    [Benchmark]
    public Table DynamicLikeApply_OuterPattern() => _outerPattern.Run();

    public int SourceInvocations => _recorder.SourceInvocations;

    public int ChildrenReads => _recorder.ChildrenReads;

    public int PatternReads => _recorder.PatternReads;

    public void ResetCounters() => _recorder.Reset();

    internal static (QueryInspectionResult Inner, QueryInspectionResult Outer) InspectQueries()
    {
        var recorder = new DynamicLikeApplyRecorder();
        var provider = new DynamicLikeApplySchemaProvider(CreateRows(4, 2, recorder), recorder);
        var logger = new BenchmarkLoggerResolver();
        var options = BenchmarkCompilationOptions.Materialized(
            new CompilationOptions(ParallelizationMode.None));
        return (
            InstanceCreator.CompileForInspection(
                InnerPatternQuery,
                $"DynamicLikeApplyInnerInspection_{Guid.NewGuid():N}",
                provider,
                logger,
                options),
            InstanceCreator.CompileForInspection(
                OuterPatternQuery,
                $"DynamicLikeApplyOuterInspection_{Guid.NewGuid():N}",
                provider,
                logger,
                options));
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _innerPattern?.Dispose();
        _outerPattern?.Dispose();
    }

    private CompiledQuery Compile(
        string query,
        ISchemaProvider provider,
        CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            query,
            $"DynamicLikeApply_{Guid.NewGuid():N}",
            provider,
            _loggerResolver,
            options);
    }

    private void ValidateResult(Table result)
    {
        if (result.Count != ExpectedResultCount)
        {
            throw new InvalidOperationException(
                $"Expected {ExpectedResultCount} rows, received {result.Count}.");
        }
    }

    internal static DynamicLikeApplyOuter[] CreateRows(
        int outerRowCount,
        int fanOut,
        DynamicLikeApplyRecorder recorder)
    {
        return Enumerable.Range(0, outerRowCount)
            .Select(outerIndex =>
            {
                var outerIdentity = outerIndex.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
                var input = $"outer-{outerIdentity}-value";
                var children = Enumerable.Range(0, fanOut)
                    .Select(childIndex => new DynamicLikeApplyChild
                    {
                        Id = childIndex,
                        Pattern = childIndex % 2 == 0 ? input : $"outer-{outerIdentity}-%",
                        Value = $"child-{outerIdentity}-{childIndex:D2}"
                    })
                    .ToArray();
                return new DynamicLikeApplyOuter(
                    recorder,
                    outerIndex,
                    input,
                    $"child-{outerIdentity}-%",
                    children);
            })
            .ToArray();
    }

    internal const string InnerPatternQuery =
        "select m1.Id, m2.Id as ChildId from #dynamicLike.items() m1 " +
        "cross apply m1.Children m2 where m1.Input like m2.Pattern";

    internal const string OuterPatternQuery =
        "select m1.Id, m2.Id as ChildId from #dynamicLike.items() m1 " +
        "cross apply m1.Children m2 where m2.Value like m1.Pattern";
}

public sealed class DynamicLikeApplyOuter
{
    private readonly DynamicLikeApplyChild[] _children;
    private readonly string _pattern;
    private readonly DynamicLikeApplyRecorder _recorder;

    internal DynamicLikeApplyOuter(
        DynamicLikeApplyRecorder recorder,
        int id,
        string input,
        string pattern,
        DynamicLikeApplyChild[] children)
    {
        _recorder = recorder;
        _children = children;
        _pattern = pattern;
        Id = id;
        Input = input;
    }

    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(Id)] = 0,
            [nameof(Input)] = 1,
            [nameof(Pattern)] = 2,
            [nameof(Children)] = 3
        };

    public static readonly IReadOnlyDictionary<int, Func<DynamicLikeApplyOuter, object?>> IndexToObjectAccessMap =
        new Dictionary<int, Func<DynamicLikeApplyOuter, object?>>
        {
            [0] = row => row.Id,
            [1] = row => row.Input,
            [2] = row => row.Pattern,
            [3] = row => row.Children
        };

    public int Id { get; }

    public string Input { get; }

    public string Pattern
    {
        get
        {
            _recorder.RecordPatternRead();
            return _pattern;
        }
    }

    [BindablePropertyAsTable]
    public DynamicLikeApplyChild[] Children
    {
        get
        {
            _recorder.RecordChildrenRead();
            return _children;
        }
    }
}

public sealed class DynamicLikeApplyChild
{
    public int Id { get; init; }

    public string Pattern { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

internal sealed class DynamicLikeApplyRecorder
{
    private int _childrenReads;
    private int _patternReads;
    private int _sourceInvocations;

    public int ChildrenReads => Volatile.Read(ref _childrenReads);

    public int PatternReads => Volatile.Read(ref _patternReads);

    public int SourceInvocations => Volatile.Read(ref _sourceInvocations);

    public void RecordChildrenRead() => Interlocked.Increment(ref _childrenReads);

    public void RecordPatternRead() => Interlocked.Increment(ref _patternReads);

    public void RecordSourceInvocation() => Interlocked.Increment(ref _sourceInvocations);

    public void Reset()
    {
        Interlocked.Exchange(ref _childrenReads, 0);
        Interlocked.Exchange(ref _patternReads, 0);
        Interlocked.Exchange(ref _sourceInvocations, 0);
    }
}

internal sealed class DynamicLikeApplySchemaProvider(
    IReadOnlyList<DynamicLikeApplyOuter> rows,
    DynamicLikeApplyRecorder recorder) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema, "dynamicLike", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(schema, "#dynamicLike", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new DynamicLikeApplySchema(rows, recorder);
    }
}

internal sealed class DynamicLikeApplySchema(
    IReadOnlyList<DynamicLikeApplyOuter> rows,
    DynamicLikeApplyRecorder recorder) : SchemaBase("dynamicLike", CreateLibrary())
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
            return new DynamicLikeApplyTable();

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSourceType<T, DynamicLikeApplyOuter>(
                name,
                new DynamicLikeApplyRowSource(rows, recorder));
        }

        throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(manager);
    }
}

internal sealed class DynamicLikeApplyTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(DynamicLikeApplyOuter.Id), 0, typeof(int)),
        new SchemaColumn(nameof(DynamicLikeApplyOuter.Input), 1, typeof(string)),
        new SchemaColumn(nameof(DynamicLikeApplyOuter.Pattern), 2, typeof(string)),
        new SchemaColumn(nameof(DynamicLikeApplyOuter.Children), 3, typeof(DynamicLikeApplyChild[]))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(DynamicLikeApplyOuter));

    public ISchemaColumn GetColumnByName(string name) =>
        Columns.Single(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

internal sealed class DynamicLikeApplyRowSource(
    IReadOnlyList<DynamicLikeApplyOuter> rows,
    DynamicLikeApplyRecorder recorder) : RowSourceBase<DynamicLikeApplyOuter>
{
    protected override void CollectChunks(IChunkWriter<DynamicLikeApplyOuter> writer)
    {
        recorder.RecordSourceInvocation();
        writer.Write(rows.ToArray());
    }
}
