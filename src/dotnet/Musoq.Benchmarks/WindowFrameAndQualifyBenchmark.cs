using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class WindowFrameAndQualifyBenchmark
{
    public enum FrameQualifyScenario
    {
        FrameRunningSum,
        FrameSlidingWindow3,
        FrameFullPartition,
        FramePartitionedRunningSum,
        QualifyRowNumberTopN,
        QualifyFramedSumFilter,
        QualifyRankTopN,
        FrameCountSliding,
        FrameAvgRunning,
        FrameMultipleFrames,
        QualifyWithWhere
    }

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(1000, 10000, 50000)]
    public int RowsCount { get; set; }

    [Params(
        FrameQualifyScenario.FrameRunningSum,
        FrameQualifyScenario.FrameSlidingWindow3,
        FrameQualifyScenario.FrameFullPartition,
        FrameQualifyScenario.FramePartitionedRunningSum,
        FrameQualifyScenario.QualifyRowNumberTopN,
        FrameQualifyScenario.QualifyFramedSumFilter,
        FrameQualifyScenario.QualifyRankTopN,
        FrameQualifyScenario.FrameCountSliding,
        FrameQualifyScenario.FrameAvgRunning,
        FrameQualifyScenario.FrameMultipleFrames,
        FrameQualifyScenario.QualifyWithWhere)]
    public FrameQualifyScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var script = Scenario switch
        {
            FrameQualifyScenario.FrameRunningSum =>
                "select Name, Sum(ToDecimal(Salary)) over (order by Name rows between unbounded preceding and current row) as RunSum " +
                "from #test.entities()",

            FrameQualifyScenario.FrameSlidingWindow3 =>
                "select Name, Sum(ToDecimal(Salary)) over (order by Name rows between 1 preceding and 1 following) as SlideSum " +
                "from #test.entities()",

            FrameQualifyScenario.FrameFullPartition =>
                "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department rows between unbounded preceding and unbounded following) as Total " +
                "from #test.entities()",

            FrameQualifyScenario.FramePartitionedRunningSum =>
                "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunSum " +
                "from #test.entities()",

            FrameQualifyScenario.QualifyRowNumberTopN =>
                "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn " +
                "from #test.entities() " +
                "qualify RowNumber() over (partition by Department order by Salary desc) <= 3",

            FrameQualifyScenario.QualifyFramedSumFilter =>
                "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunSum " +
                "from #test.entities() " +
                "qualify Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) > 100000",

            FrameQualifyScenario.QualifyRankTopN =>
                "select Name, Department, Rank() over (partition by Department order by Salary desc) as rnk " +
                "from #test.entities() " +
                "qualify Rank() over (partition by Department order by Salary desc) <= 5",

            FrameQualifyScenario.FrameCountSliding =>
                "select Name, Count(Name) over (order by Name rows between 2 preceding and current row) as Cnt " +
                "from #test.entities()",

            FrameQualifyScenario.FrameAvgRunning =>
                "select Name, Avg(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunAvg " +
                "from #test.entities()",

            FrameQualifyScenario.FrameMultipleFrames =>
                "select Name, Department, " +
                "Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunSum, " +
                "Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between 1 preceding and 1 following) as SlideSum " +
                "from #test.entities()",

            FrameQualifyScenario.QualifyWithWhere =>
                "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn " +
                "from #test.entities() where Salary > 50000 " +
                "qualify RowNumber() over (partition by Department order by Salary desc) <= 3",

            _ => throw new ArgumentOutOfRangeException()
        };

        var departments = new[] { "Engineering", "Sales", "Marketing", "Support", "Finance", "HR", "Legal", "Ops", "Product", "Design" };
        var rng = new Random(42);

        var entities = Enumerable.Range(0, RowsCount).Select(i => new FrameEntity
        {
            Name = $"Employee{i:D6}",
            Department = departments[i % departments.Length],
            Salary = rng.Next(30000, 200000)
        }).ToList();

        var schemaProvider = new FrameSchemaProvider(entities);

        _query = InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized());
    }

    [Benchmark]
    public Table RunQuery()
    {
        return _query.Run();
    }

    private sealed class FrameEntity
    {
        public string Name { get; init; } = string.Empty;
        public string Department { get; init; } = string.Empty;
        public int Salary { get; init; }
    }

    private sealed class FrameSchemaProvider(IReadOnlyList<FrameEntity> entities) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new FrameSchema(entities);
    }

    private sealed class FrameSchema(IReadOnlyList<FrameEntity> entities) : SchemaBase("test", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
            => new FrameTable();

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
            => EnsureSourceType<T, FrameEntity>(name, new EntitySource<FrameEntity>(BenchmarkSourceChunks.Create(entities), new Dictionary<string, int>
            {
                { nameof(FrameEntity.Name), 0 },
                { nameof(FrameEntity.Department), 1 },
                { nameof(FrameEntity.Salary), 2 }
            }, new Dictionary<int, Func<FrameEntity, object?>>
            {
                { 0, e => e.Name },
                { 1, e => e.Department },
                { 2, e => e.Salary }
            }));

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodManager);
        }
    }

    private sealed class FrameTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(FrameEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(FrameEntity.Department), 1, typeof(string)),
            new SchemaColumn(nameof(FrameEntity.Salary), 2, typeof(int))
        ];

        public ISchemaColumn GetColumnByName(string name) => Columns.First(c => c.ColumnName == name);

        public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(c => c.ColumnName == name).ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(FrameEntity));
    }

    private sealed class Library : LibraryBase;
}
