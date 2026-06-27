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

[MemoryDiagnoser]
public class WindowFunctionBenchmark
{
    public enum WindowScenario
    {
        RowNumberNoPartition,
        RowNumberPartitioned,
        RankPartitioned,
        DenseRankPartitioned,
        LagPartitioned,
        LeadPartitioned,
        NtilePartitioned,
        FirstValuePartitioned,
        NthValuePartitioned,
        SumAggregateWholePartition,
        SumAggregateRunning,
        SumAggregateBounded,
        AvgAggregateBounded,
        MinMaxAggregateBounded,
        MultipleWindows,
        MultipleSharedSpecWindows,
        RowNumberHighCardinalityPartitions,
        RowNumberLowCardinalityPartitions,
        RowNumberMultiKeyOrder,
        TypedPluginRunningProduct,
        TypedPluginWithArguments,
        CountAggregateWholePartition,
        AvgAggregateRunning
    }

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(1000, 10000, 50000)]
    public int RowsCount { get; set; }

    [Params(
        WindowScenario.RowNumberNoPartition,
        WindowScenario.RowNumberPartitioned,
        WindowScenario.RankPartitioned,
        WindowScenario.DenseRankPartitioned,
        WindowScenario.LagPartitioned,
        WindowScenario.LeadPartitioned,
        WindowScenario.NtilePartitioned,
        WindowScenario.FirstValuePartitioned,
        WindowScenario.NthValuePartitioned,
        WindowScenario.SumAggregateWholePartition,
        WindowScenario.SumAggregateRunning,
        WindowScenario.SumAggregateBounded,
        WindowScenario.AvgAggregateBounded,
        WindowScenario.MinMaxAggregateBounded,
        WindowScenario.MultipleWindows,
        WindowScenario.MultipleSharedSpecWindows,
        WindowScenario.RowNumberHighCardinalityPartitions,
        WindowScenario.RowNumberLowCardinalityPartitions,
        WindowScenario.RowNumberMultiKeyOrder,
        WindowScenario.TypedPluginRunningProduct,
        WindowScenario.TypedPluginWithArguments,
        WindowScenario.CountAggregateWholePartition,
        WindowScenario.AvgAggregateRunning)]
    public WindowScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var script = Scenario switch
        {
            WindowScenario.RowNumberNoPartition =>
                "select Name, RowNumber() over (order by Salary desc) as rn from #test.entities()",

            WindowScenario.RowNumberPartitioned =>
                "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn from #test.entities()",

            WindowScenario.RankPartitioned =>
                "select Name, Department, Rank() over (partition by Department order by Salary desc) as rn from #test.entities()",

            WindowScenario.DenseRankPartitioned =>
                "select Name, Department, DenseRank() over (partition by Department order by Salary desc) as rn from #test.entities()",

            WindowScenario.LagPartitioned =>
                "select Name, Department, Lag(Salary, 1) over (partition by Department order by Salary desc) as prev from #test.entities()",

            WindowScenario.LeadPartitioned =>
                "select Name, Department, Lead(Salary, 1) over (partition by Department order by Salary desc) as nxt from #test.entities()",

            WindowScenario.NtilePartitioned =>
                "select Name, Department, Ntile(10) over (partition by Department order by Salary desc) as bucket from #test.entities()",

            WindowScenario.FirstValuePartitioned =>
                "select Name, Department, FirstValue(Salary) over (partition by Department order by Salary desc) as top_salary from #test.entities()",

            WindowScenario.NthValuePartitioned =>
                "select Name, Department, NthValue(Salary, 3) over (partition by Department order by Salary desc rows between unbounded preceding and unbounded following) as third_salary from #test.entities()",

            WindowScenario.SumAggregateWholePartition =>
                "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department) as total from #test.entities()",

            WindowScenario.SumAggregateRunning =>
                "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Salary) as running from #test.entities()",

            WindowScenario.SumAggregateBounded =>
                "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between 3 preceding and 3 following) as bounded_sum from #test.entities()",

            WindowScenario.AvgAggregateBounded =>
                "select Name, Department, Avg(ToDecimal(Salary)) over (partition by Department order by Name rows between 5 preceding and current row) as bounded_avg from #test.entities()",

            WindowScenario.MinMaxAggregateBounded =>
                "select Name, Department, Min(Salary) over (partition by Department order by Name rows between 4 preceding and 4 following) as rolling_min, Max(Salary) over (partition by Department order by Name rows between 4 preceding and 4 following) as rolling_max from #test.entities()",

            WindowScenario.MultipleWindows =>
                @"select Name, Department,
                    RowNumber() over (partition by Department order by Salary desc) as rn,
                    Sum(ToDecimal(Salary)) over (partition by Department) as total,
                    Lag(Salary, 1) over (partition by Department order by Salary desc) as prev
                from #test.entities()",

            WindowScenario.MultipleSharedSpecWindows =>
                @"select Name, Department,
                    RowNumber() over (partition by Department order by Salary desc, Name) as rn,
                    Rank() over (partition by Department order by Salary desc, Name) as rnk,
                    DenseRank() over (partition by Department order by Salary desc, Name) as dense_rnk,
                    Lag(Salary, 1) over (partition by Department order by Salary desc, Name) as prev
                from #test.entities()",

            WindowScenario.RowNumberHighCardinalityPartitions =>
                "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn from #test.entities()",

            WindowScenario.RowNumberLowCardinalityPartitions =>
                "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn from #test.entities()",

            WindowScenario.RowNumberMultiKeyOrder =>
                "select Name, Department, RowNumber() over (partition by Department order by Salary desc, Name) as rn from #test.entities()",

            WindowScenario.TypedPluginRunningProduct =>
                "select Name, Department, RunningProduct(Salary) over (partition by Department order by Name) as product from #test.entities()",

            WindowScenario.TypedPluginWithArguments =>
                "select Name, Department, ScaledRunningProduct(Salary, 2) over (partition by Department order by Name) as product from #test.entities()",

            WindowScenario.CountAggregateWholePartition =>
                "select Name, Department, Count(Name) over (partition by Department) as cnt from #test.entities()",

            WindowScenario.AvgAggregateRunning =>
                "select Name, Department, Avg(ToDecimal(Salary)) over (partition by Department order by Salary) as running_avg from #test.entities()",

            _ => throw new ArgumentOutOfRangeException()
        };

        var departments = CreateDepartments(RowsCount, Scenario);
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
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized());
    }

    [Benchmark]
    public Table RunQuery()
    {
        return _query.Run();
    }

    private sealed class WinEntity
    {
        public string Name { get; init; } = string.Empty;
        public string Department { get; init; } = string.Empty;
        public int Salary { get; init; }
    }

    private sealed class WinSchemaProvider(IReadOnlyList<WinEntity> entities) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new WinSchema(entities);
    }

    private sealed class WinSchema(IReadOnlyList<WinEntity> entities) : SchemaBase("test", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
            => new WinTable();

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
            => EnsureSourceType<T, WinEntity>(name, new EntitySource<WinEntity>(BenchmarkSourceChunks.Create(entities), new Dictionary<string, int>
            {
                { nameof(WinEntity.Name), 0 },
                { nameof(WinEntity.Department), 1 },
                { nameof(WinEntity.Salary), 2 }
            }, new Dictionary<int, Func<WinEntity, object?>>
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

    private static string[] CreateDepartments(int rowsCount, WindowScenario scenario)
    {
        var departmentCount = scenario switch
        {
            WindowScenario.RowNumberHighCardinalityPartitions => Math.Max(1, rowsCount / 2),
            WindowScenario.RowNumberLowCardinalityPartitions => 2,
            _ => 10
        };

        return Enumerable.Range(0, departmentCount)
            .Select(static index => $"Department{index:D5}")
            .ToArray();
    }

    public sealed class Library : LibraryBase
    {
        [WindowFunction(Name = "RunningProduct")]
        public IWindowFunction<int, decimal> WindowRunningProduct()
        {
            return new RunningProductWindowFunction();
        }

        [WindowFunction(Name = "ScaledRunningProduct")]
        public ScaledRunningProductWindowFunction WindowScaledRunningProduct()
        {
            return new ScaledRunningProductWindowFunction();
        }
    }

    private sealed class RunningProductWindowFunction : IWindowFunction<int, decimal>
    {
        private decimal _product = 1m;
        private bool _hasValue;

        public void PartitionStart()
        {
            _product = 1m;
            _hasValue = false;
        }

        public void Accumulate(int value)
        {
            _product = (_product * ((value % 11) + 1)) % 1_000_000_007m;
            _hasValue = true;
        }

        public decimal GetValue()
        {
            return _hasValue ? _product : 0m;
        }
    }

    public sealed class ScaledRunningProductWindowFunction :
        IWindowFunction<int, decimal>,
        IWindowFunctionArguments<int>
    {
        private decimal _product = 1m;
        private int _scale = 1;
        private bool _hasValue;

        public void PartitionStart()
        {
            _product = 1m;
            _hasValue = false;
        }

        public void Accumulate(int value)
        {
            _product = (_product * ((value % 11) + 1)) % 1_000_000_007m;
            _hasValue = true;
        }

        public decimal GetValue()
        {
            return _hasValue ? _product * _scale : 0m;
        }

        public void SetArguments(int scale)
        {
            _scale = scale;
        }
    }
}
