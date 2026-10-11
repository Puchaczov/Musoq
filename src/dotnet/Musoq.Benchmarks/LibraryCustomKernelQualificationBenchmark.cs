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

/// <summary>Measures full typed result consumption; BenchmarkDotNet generates derived execution runners.</summary>
[MemoryDiagnoser]
public class LibraryCustomKernelQualificationBenchmark
{
    private CompiledTypedProfileQuery<Result> _query = null!;
    private long _expected;

    [Params(1000, 10000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, RowCount).Select(static index => new Input(index % 16,
            GetAmount(index), index % 3 + 1)).ToArray();
        const string sql = "select Partition, WeightedPositiveCount(Amount, Weight) as Present " +
            "from #rows.items() group by Partition";
        _query = InstanceCreator.CompileForTypedProfile<Result>(sql, Guid.NewGuid().ToString(),
            new Provider(rows), new BenchmarkLoggerResolver(), new CompilationOptions(parallelizationMode: ParallelizationMode.None));
        _expected = rows.Where(static row => row.Amount > 0m).Sum(static row => (long)row.Weight);
        var expected = rows.GroupBy(static row => row.Partition).ToDictionary(static group => group.Key,
            static group => group.Where(static row => row.Amount > 0m).Sum(static row => (long)row.Weight));
        var actual = _query.RunWithProfile(CancellationToken.None).Rows.ToArray();
        if (actual.Length != expected.Count || actual.Any(row => row.Present != expected[row.Partition]) || Consume() != _expected)
            throw new InvalidOperationException("Custom kernel differs from independent weighted-positive counts.");
    }

    private static decimal? GetAmount(int index)
    {
        if (index % 7 == 0)
            return null;
        return index % 11 - 5m;
    }

    [Benchmark]
    public long Consume()
    {
        var total = 0L;
        foreach (var row in _query.RunWithProfile(CancellationToken.None).Rows)
            total += row.Present;
        return total;
    }

    public sealed record Input(int Partition, decimal? Amount, int Weight);
    public readonly record struct Result(int Partition, long Present);

    public sealed class CustomLibrary : LibraryBase
    {
        [AggregateFunction(typeof(Kernel), Name = "WeightedPositiveCount")]
        public long WeightedPositiveCount(decimal? amount, int weight) => AggregateFunction.NotInvoked<long>();
    }

    public static class Kernel
    {
        public struct State { public long Count; }
        public static void Reset(ref State state) => state = default;
        public static void Set(ref State state, decimal? amount, int weight)
        {
            if (amount.HasValue && amount.Value > 0m)
                state.Count = checked(state.Count + weight);
        }
        public static long Get(in State state) => state.Count;
    }

    private sealed class Provider(Input[] rows) : ISchemaProvider
    {
        public ISchema GetSchema(string name) => new InputSchema(rows);
    }

    private sealed class InputSchema(Input[] rows) : SchemaBase("rows", CreateMethods())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext context, params object?[] arguments)
            => new InputTable();
        public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context, TArguments arguments)
            => EnsureSourceType<T, Input>(name, new InputSource(rows));
        private static MethodsAggregator CreateMethods()
        {
            var methods = new MethodsManager();
            methods.RegisterLibraries(new CustomLibrary());
            return new MethodsAggregator(methods);
        }
    }

    private sealed class InputSource(Input[] rows) : RowSource<Input>
    {
        private readonly IReadOnlyList<IReadOnlyList<Input>> _chunks = BenchmarkSourceChunks.Create(rows);
        public override IEnumerable<IReadOnlyList<Input>> Chunks => _chunks;
    }

    private sealed class InputTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [new SchemaColumn("Partition", 0, typeof(int)),
            new SchemaColumn("Amount", 1, typeof(decimal?)), new SchemaColumn("Weight", 2, typeof(int))];
        public SchemaTableMetadata Metadata => new(typeof(Input));
        public ISchemaColumn? GetColumnByName(string name) => Columns.FirstOrDefault(column => column.ColumnName == name);
        public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
