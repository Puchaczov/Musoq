using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Benchmarks.Components;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TypedRelationalContextBenchmark
{
    private CompiledTypedProfileQuery<OutputRow> _compiled = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, 10_000).Select(static index => new InputRow(index, index % 7)).ToArray();
        var provider = new Provider(rows);
        _compiled = InstanceCreator.CompileForTypedProfile<OutputRow>("""
            select a.SourcePosition() as Position, b.SourcePosition() as OtherPosition,
                   a.Amount + b.Amount as Amount
            from #input.rows() a left outer join #input.rows() b on a.Position = b.Position
            order by Position
            """, Guid.NewGuid().ToString(), provider, new BenchmarkLoggerResolver(),
            new CompilationOptions(parallelizationMode: ParallelizationMode.None));
        var expected = rows.Sum(static row => (long)row.Position * 2 + row.Amount * 2);
        if (ConsumeJoinedContexts() != expected)
            throw new InvalidOperationException("Context benchmark differs from the independent reference.");
    }

    [Benchmark]
    public long ConsumeJoinedContexts()
    {
        long checksum = 0;
        foreach (var row in _compiled.RunWithProfile(CancellationToken.None).Rows)
            checksum += row.Position + row.OtherPosition.GetValueOrDefault() + row.Amount.GetValueOrDefault();
        return checksum;
    }

    public readonly record struct InputRow(int Position, int Amount);
    public readonly record struct OutputRow(int Position, int? OtherPosition, int? Amount);

    public sealed class ContextLibrary : LibraryBase
    {
        [BindableMethod]
        public int SourcePosition([InjectSpecificSource(typeof(InputRow))] InputRow row) => row.Position;
    }

    private sealed class Provider(InputRow[] rows) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new ContextSchema(rows);
    }

    private sealed class ContextSchema(InputRow[] rows) : SchemaBase("input", CreateMethods())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
            => new ContextTable();

        public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments parameters)
            => EnsureSourceType<T, InputRow>(name, new ContextSource(rows));

        private static MethodsAggregator CreateMethods()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new ContextLibrary());
            return new MethodsAggregator(manager);
        }
    }

    private sealed class ContextSource(InputRow[] rows) : RowSource<InputRow>
    {
        private readonly IReadOnlyList<IReadOnlyList<InputRow>> _chunks = BenchmarkSourceChunks.Create(rows);
        public override IEnumerable<IReadOnlyList<InputRow>> Chunks => _chunks;
    }

    private sealed class ContextTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [new SchemaColumn("Position", 0, typeof(int)), new SchemaColumn("Amount", 1, typeof(int))];
        public SchemaTableMetadata Metadata => new(typeof(InputRow));
        public ISchemaColumn? GetColumnByName(string name)
            => Columns.FirstOrDefault(column => column.ColumnName == name);
        public ISchemaColumn[] GetColumnsByName(string name)
            => Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
