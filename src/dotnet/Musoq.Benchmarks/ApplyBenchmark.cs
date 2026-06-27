using System.Threading;
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
public class ApplyBenchmark
{
    public enum ApplyScenario
    {
        CrossApplyTable,
        OuterApplyTable,
        ChainedGroupedAggregateWindow,
        ChainedWindow,
        ChainedMixedDistinctAggregateSort,
        ChainedMixedDistinctMinMaxAggregateSort,
        ChainedMixedDistinctAvgAggregateSort,
        ChainedMixedDistinctMinMaxAggregateWindow,
        ChainedMixedDistinctAvgAggregateWindow,
        ChainedQualifyWindow,
        ChainedGroupedAggregateQualifyWindow
    }

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(100, 1_000)]
    public int RowsCount { get; set; }

    [Params(
        ApplyScenario.CrossApplyTable,
        ApplyScenario.OuterApplyTable,
        ApplyScenario.ChainedGroupedAggregateWindow,
        ApplyScenario.ChainedWindow,
        ApplyScenario.ChainedMixedDistinctAggregateSort,
        ApplyScenario.ChainedMixedDistinctMinMaxAggregateSort,
        ApplyScenario.ChainedMixedDistinctAvgAggregateSort,
        ApplyScenario.ChainedMixedDistinctMinMaxAggregateWindow,
        ApplyScenario.ChainedMixedDistinctAvgAggregateWindow,
        ApplyScenario.ChainedQualifyWindow,
        ApplyScenario.ChainedGroupedAggregateQualifyWindow)]
    public ApplyScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var script = Scenario switch
        {
            ApplyScenario.CrossApplyTable =>
                "select a.Name, b.Name as ChildName from #A.entities() a cross apply #B.entities() b",

            ApplyScenario.OuterApplyTable =>
                "select a.Name, b.Name as OtherName from #A.entities() a outer apply #B.entities() b",

            ApplyScenario.ChainedGroupedAggregateWindow =>
                "select i.Name as Name, Sum(n.Value) as ValueSum, RowNumber() over (order by Sum(n.Value) desc, i.Name) as GroupRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by GroupRowNo",

            ApplyScenario.ChainedWindow =>
                "select i.Name, n.Value as FirstValue, m.Value as SecondValue, RowNumber() over (partition by i.Name order by n.Value, m.Value) as RowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by i.Name, RowNo",

            ApplyScenario.ChainedMixedDistinctAggregateSort =>
                "select i.Name as Name, Sum(n.Value) as RepeatedSum, Sum(distinct n.Value) as DistinctSum from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Sum(distinct n.Value) desc, Sum(n.Value) desc, i.Name",

            ApplyScenario.ChainedMixedDistinctMinMaxAggregateSort =>
                "select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name",

            ApplyScenario.ChainedMixedDistinctAvgAggregateSort =>
                "select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name",

            ApplyScenario.ChainedMixedDistinctMinMaxAggregateWindow =>
                "select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax, RowNumber() over (order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name) as MixedMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedMinMaxRowNo",

            ApplyScenario.ChainedMixedDistinctAvgAggregateWindow =>
                "select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg, RowNumber() over (order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name) as MixedAvgRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedAvgRowNo",

            ApplyScenario.ChainedQualifyWindow =>
                "select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m qualify RowNumber() over (partition by i.Name order by n.Value, m.Value) <= 1 order by i.Name",

            ApplyScenario.ChainedGroupedAggregateQualifyWindow =>
                "select i.Name as Name, Avg(n.Value) as ValueAvg, Min(n.Value) as ValueMin, Max(n.Value) as ValueMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Max(n.Value) >= 2 qualify RowNumber() over (order by Avg(n.Value) desc, Min(n.Value), Max(n.Value) desc) <= 1 order by Name",

            _ => throw new ArgumentOutOfRangeException()
        };

        ISchemaProvider schemaProvider = Scenario is ApplyScenario.CrossApplyTable or ApplyScenario.OuterApplyTable
            ? CreateTableApplySchemaProvider()
            : new ApplyItemsSchemaProvider(CreateApplyRows(RowsCount));

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

    private ApplyMultiSchemaProvider CreateTableApplySchemaProvider()
    {
        var leftRows = CreateTableRows(RowsCount, "left");
        var rightRows = CreateTableRows(Math.Max(1, RowsCount / 4), "right");

        return new ApplyMultiSchemaProvider(
            new Dictionary<string, IReadOnlyList<TableApplyEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = leftRows,
                ["#A"] = leftRows,
                ["B"] = rightRows,
                ["#B"] = rightRows
            });
    }

    private static TableApplyEntity[] CreateTableRows(int count, string prefix)
    {
        return Enumerable.Range(0, count)
            .Select(index => new TableApplyEntity
            {
                Id = index,
                Name = $"{prefix}_{index}",
                City = $"City_{index % 16}",
                Population = index * 10
            })
            .ToArray();
    }

    private static ApplyItemEntity[] CreateApplyRows(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new ApplyItemEntity
            {
                Name = $"item_{index % 64}",
                Numbers = CreateNumberSet(index)
            })
            .ToArray();
    }

    private static int[] CreateNumberSet(int index)
    {
        var width = 3 + index % 5;
        return Enumerable.Range(0, width)
            .Select(offset => (index + offset) % 9 + 1)
            .ToArray();
    }

    private sealed class ApplyItemEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
            new Dictionary<string, int>
            {
                [nameof(Name)] = 0,
                [nameof(Numbers)] = 1
            };

        public static readonly IReadOnlyDictionary<int, Func<ApplyItemEntity, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<ApplyItemEntity, object?>>
            {
                [0] = entity => entity.Name,
                [1] = entity => entity.Numbers
            };

        public string Name { get; init; } = string.Empty;

        [BindablePropertyAsTable]
        public int[] Numbers { get; init; } = [];
    }

    private sealed class ApplyItemsSchemaProvider(IReadOnlyList<ApplyItemEntity> rows) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "apply", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "#apply", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(schema);

            return new ApplyItemsSchema(rows);
        }
    }

    private sealed class ApplyItemsSchema(IReadOnlyList<ApplyItemEntity> rows) : SchemaBase("apply", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return new ApplyItemsTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, ApplyItemEntity>(name, new ApplyItemsRowSource(rows));

            throw new NotSupportedException(name);
        }
    }

    private sealed class ApplyItemsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(ApplyItemEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(ApplyItemEntity.Numbers), 1, typeof(int[]))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(ApplyItemEntity));

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.Single(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class ApplyItemsRowSource(IReadOnlyList<ApplyItemEntity> rows) : RowSourceBase<ApplyItemEntity>
    {
        protected override void CollectChunks(IChunkWriter<ApplyItemEntity> writer)
        {
            writer.Write(rows.ToArray());
        }
    }

    private sealed class TableApplyEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
            new Dictionary<string, int>
            {
                [nameof(Id)] = 0,
                [nameof(Name)] = 1,
                [nameof(City)] = 2,
                [nameof(Population)] = 3
            };

        public static readonly IReadOnlyDictionary<int, Func<TableApplyEntity, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<TableApplyEntity, object?>>
            {
                [0] = entity => entity.Id,
                [1] = entity => entity.Name,
                [2] = entity => entity.City,
                [3] = entity => entity.Population
            };

        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public int Population { get; init; }
    }

    private sealed class ApplyMultiSchemaProvider(
        IReadOnlyDictionary<string, IReadOnlyList<TableApplyEntity>> rowsBySchema) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!rowsBySchema.TryGetValue(schema, out var rows))
                throw new NotSupportedException(schema);

            return new TableApplySchema(schema.TrimStart('#'), rows);
        }
    }

    private sealed class TableApplySchema(string name, IReadOnlyList<TableApplyEntity> rows)
        : SchemaBase(name, CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return new TableApplyTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, TableApplyEntity>(name, new TableApplyRowSource(rows));

            throw new NotSupportedException(name);
        }
    }

    private sealed class TableApplyTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(TableApplyEntity.Id), 0, typeof(int)),
            new SchemaColumn(nameof(TableApplyEntity.Name), 1, typeof(string)),
            new SchemaColumn(nameof(TableApplyEntity.City), 2, typeof(string)),
            new SchemaColumn(nameof(TableApplyEntity.Population), 3, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(TableApplyEntity));

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.Single(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class TableApplyRowSource(IReadOnlyList<TableApplyEntity> rows) : RowSourceBase<TableApplyEntity>
    {
        protected override void CollectChunks(IChunkWriter<TableApplyEntity> writer)
        {
            writer.Write(rows.ToArray());
        }
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new Library());
        return new MethodsAggregator(methodsManager);
    }

    private sealed class Library : LibraryBase;
}
