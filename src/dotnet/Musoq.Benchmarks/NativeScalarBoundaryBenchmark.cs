using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Benchmarks.Components;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Plugins;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class NativeScalarBoundaryBenchmark
{
    private const string Sql = "select Position, Amount::int as Rounded, " +
        "Concat(Label, Label, Label, Label, Label, Label) as Joined from #input.rows() order by Position";
    private CompiledTypedProfileQuery<OutputRow> _query = null!;
    private Provider _provider = null!;

    [GlobalSetup]
    public void Setup()
    {
        var input = Enumerable.Range(0, 10000).Select(index => new InputRow(index,
            (index % 4) switch { 0 => 1.5m, 1 => 2.5m, 2 => (decimal?)null, _ => -1.5m }, "x")).ToArray();
        _provider = new Provider(input);
        _query = Compile();
        var rows = _query.RunWithProfile(CancellationToken.None).Rows.ToArray();
        if (rows.Length != input.Length)
            throw new InvalidOperationException("Native scalar benchmark returned an incorrect row count.");
        for (var index = 0; index < rows.Length; index++)
        {
            var rounded = input[index].Amount.HasValue ? Convert.ToInt32(input[index].Amount!.Value) : (int?)null;
            if (rows[index] != new OutputRow(index, rounded, "xxxxxx"))
                throw new InvalidOperationException("Native scalar benchmark differs from the independent value oracle.");
        }
    }

    [Benchmark]
    public long ConsumeNumericCastsAndVariadicCalls()
    {
        long checksum = 0;
        foreach (var row in _query.RunWithProfile(CancellationToken.None).Rows)
            checksum += row.Position + row.Rounded.GetValueOrDefault() + row.Joined.Length;
        return checksum;
    }

    [Benchmark]
    public CompiledTypedProfileQuery<OutputRow> CompileNumericCastsAndVariadicCalls() => Compile();

    private CompiledTypedProfileQuery<OutputRow> Compile()
        => InstanceCreator.CompileForTypedProfile<OutputRow>(Sql, Guid.NewGuid().ToString(),
            _provider, new BenchmarkLoggerResolver(), new CompilationOptions(parallelizationMode: ParallelizationMode.None));

    public readonly record struct InputRow(int Position, decimal? Amount, string Label);
    public readonly record struct OutputRow(int Position, int? Rounded, string Joined);
    private sealed class Provider(InputRow[] rows) : ISchemaProvider
    {
        public ISchema GetSchema(string name) => new InputSchema(rows);
    }
    private sealed class InputSchema(InputRow[] rows) : SchemaBase("input", CreateMethods())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext context, params object?[] arguments)
            => new InputTable();
        public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context, TArguments arguments)
            => EnsureSourceType<T, InputRow>(name, new InputSource(rows));
        private static MethodsAggregator CreateMethods()
        {
            var methods = new MethodsManager();
            methods.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methods);
        }
    }
    private sealed class InputSource(InputRow[] rows) : RowSource<InputRow>
    {
        public override IEnumerable<IReadOnlyList<InputRow>> Chunks => new[] { rows };
    }
    private sealed class InputTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [new SchemaColumn("Position", 0, typeof(int)),
            new SchemaColumn("Amount", 1, typeof(decimal?)), new SchemaColumn("Label", 2, typeof(string))];
        public SchemaTableMetadata Metadata { get; } = new(typeof(InputRow));
        public ISchemaColumn? GetColumnByName(string name) => Columns.SingleOrDefault(column => column.ColumnName == name);
        public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
