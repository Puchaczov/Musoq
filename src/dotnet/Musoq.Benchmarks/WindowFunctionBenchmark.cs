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
public class WindowFunctionBenchmark
{
    public enum WindowScenario
    {
        RowNumber_NoPartition,
        RowNumber_Partitioned,
        Rank_Partitioned,
        DenseRank_Partitioned,
        Lag_Partitioned,
        Lead_Partitioned,
        SumAggregate_WholePartition,
        SumAggregate_Running,
        MultipleWindows,
        CountAggregate_WholePartition,
        AvgAggregate_Running
    }

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(1000, 10000, 50000)]
    public int RowsCount { get; set; }

    [Params(
        WindowScenario.RowNumber_NoPartition,
        WindowScenario.RowNumber_Partitioned,
        WindowScenario.Rank_Partitioned,
        WindowScenario.DenseRank_Partitioned,
        WindowScenario.Lag_Partitioned,
        WindowScenario.Lead_Partitioned,
        WindowScenario.SumAggregate_WholePartition,
        WindowScenario.SumAggregate_Running,
        WindowScenario.MultipleWindows,
        WindowScenario.CountAggregate_WholePartition,
        WindowScenario.AvgAggregate_Running)]
    public WindowScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var script = Scenario switch
        {
            WindowScenario.RowNumber_NoPartition =>
                "select Name, RowNumber() over (order by Salary desc) as rn from #test.entities()",

            WindowScenario.RowNumber_Partitioned =>
                "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn from #test.entities()",

            WindowScenario.Rank_Partitioned =>
                "select Name, Department, Rank() over (partition by Department order by Salary desc) as rn from #test.entities()",

            WindowScenario.DenseRank_Partitioned =>
                "select Name, Department, DenseRank() over (partition by Department order by Salary desc) as rn from #test.entities()",

            WindowScenario.Lag_Partitioned =>
                "select Name, Department, Lag(Salary, 1) over (partition by Department order by Salary desc) as prev from #test.entities()",

            WindowScenario.Lead_Partitioned =>
                "select Name, Department, Lead(Salary, 1) over (partition by Department order by Salary desc) as nxt from #test.entities()",

            WindowScenario.SumAggregate_WholePartition =>
                "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department) as total from #test.entities()",

            WindowScenario.SumAggregate_Running =>
                "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Salary) as running from #test.entities()",

            WindowScenario.MultipleWindows =>
                @"select Name, Department,
                    RowNumber() over (partition by Department order by Salary desc) as rn,
                    Sum(ToDecimal(Salary)) over (partition by Department) as total,
                    Lag(Salary, 1) over (partition by Department order by Salary desc) as prev
                from #test.entities()",

            WindowScenario.CountAggregate_WholePartition =>
                "select Name, Department, Count(Name) over (partition by Department) as cnt from #test.entities()",

            WindowScenario.AvgAggregate_Running =>
                "select Name, Department, Avg(ToDecimal(Salary)) over (partition by Department order by Salary) as running_avg from #test.entities()",

            _ => throw new ArgumentOutOfRangeException()
        };

        var departments = new[] { "Engineering", "Sales", "Marketing", "Support", "Finance", "HR", "Legal", "Ops", "Product", "Design" };
        var rng = new Random(42);

        var entities = Enumerable.Range(0, RowsCount).Select(i => new WinEntity
        {
            Name = $"Employee{i}",
            Department = departments[i % departments.Length],
            Salary = rng.Next(30000, 200000)
        }).ToList();

        var schemaProvider = new WinSchemaProvider(entities);

        _query = InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);
    }

    [Benchmark]
    public Table RunQuery()
    {
        return _query.Run();
    }

    private sealed class WinEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int Salary { get; set; }
    }

    private sealed class WinSchemaProvider : ISchemaProvider
    {
        private readonly IEnumerable<WinEntity> _entities;

        public WinSchemaProvider(IEnumerable<WinEntity> entities) => _entities = entities;

        public ISchema GetSchema(string schema) => new WinSchema(_entities);
    }

    private sealed class WinSchema : SchemaBase
    {
        private readonly IEnumerable<WinEntity> _entities;

        public WinSchema(IEnumerable<WinEntity> entities) : base("test", CreateLibrary())
            => _entities = entities;

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
            => new WinTable();

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
            => new EntitySource<WinEntity>(_entities, new Dictionary<string, int>
            {
                { nameof(WinEntity.Name), 0 },
                { nameof(WinEntity.Department), 1 },
                { nameof(WinEntity.Salary), 2 }
            }, new Dictionary<int, Func<WinEntity, object>>
            {
                { 0, e => e.Name },
                { 1, e => e.Department },
                { 2, e => e.Salary }
            });

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodManager);
        }
    }

    private sealed class WinTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(WinEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(WinEntity.Department), 1, typeof(string)),
            new SchemaColumn(nameof(WinEntity.Salary), 2, typeof(int))
        ];

        public ISchemaColumn GetColumnByName(string name) => Columns.First(c => c.ColumnName == name);

        public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(c => c.ColumnName == name).ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(WinEntity));
    }

    private sealed class Library : LibraryBase
    {
    }
}
