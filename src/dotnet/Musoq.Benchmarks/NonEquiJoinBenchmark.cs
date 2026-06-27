using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class NonEquiJoinBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(1000, 2000)] public int RowsCount { get; set; }

    [Params(true, false)] public bool UseSortMergeJoin { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var script = @"
                select 
                    1
                from #test.entities() a
                inner join #test.entities() b on a.Population > b.Population";

        var entities = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Name{i}",
            Population = i
        }).ToList();

        var schemaProvider = new NonEquiSchemaProvider(entities);

        _query = InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized(new CompilationOptions(useSortMergeJoin: UseSortMergeJoin)));
    }

    [Benchmark]
    public Table RunQuery()
    {
        return _query.Run();
    }

    private sealed class NonEquiSchemaProvider(IReadOnlyList<NonEquiEntity> entities) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new NonEquiSchema(entities);
        }
    }

    private sealed class NonEquiSchema(IReadOnlyList<NonEquiEntity> entities) : SchemaBase("test", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new NonEquiTable();
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            return EnsureSourceType<T, NonEquiEntity>(name, new EntitySource<NonEquiEntity>(BenchmarkSourceChunks.Create(entities), new Dictionary<string, int>
            {
                { nameof(NonEquiEntity.Id), 0 },
                { nameof(NonEquiEntity.Name), 1 },
                { nameof(NonEquiEntity.Population), 2 }
            }, new Dictionary<int, Func<NonEquiEntity, object?>>
            {
                { 0, e => e.Id },
                { 1, e => e.Name },
                { 2, e => e.Population }
            }));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            var lib = new Library();
            methodManager.RegisterLibraries(lib);
            return new MethodsAggregator(methodManager);
        }
    }
}
