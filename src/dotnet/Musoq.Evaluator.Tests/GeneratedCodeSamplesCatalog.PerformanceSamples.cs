using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static IReadOnlyList<GeneratedCodeSample> CreatePerformanceSamples()
    {
        return
        [
            PerformanceInspection(
                "Q227_PerformanceReflectedJoinAggregate",
                """
                select a.City as City, Count(b.Name) as MatchCount
                from #A.entities() a
                inner join #B.entities() b on a.City = b.City
                group by a.City
                """,
                CreatePerformanceReflectedSchemaProvider),
            PerformanceInspection(
                "Q228_PerformanceWideCorrelatedSubquery",
                """
                SELECT a.Name,
                       CASE WHEN EXISTS (
                           SELECT b.City FROM #B.entities() b
                           WHERE b.Name = a.Name
                             AND b.City = a.City
                             AND b.Country = a.Country
                             AND b.Population = a.Population
                             AND b.Month = a.Month
                             AND b.Money = a.Money
                             AND b.Id = a.Id
                             AND b.NullableValue = a.NullableValue
                       ) THEN 'Y' ELSE 'N' END AS ExistsResult,
                       CASE WHEN NOT EXISTS (
                           SELECT b.City FROM #B.entities() b
                           WHERE b.Name = a.Name
                             AND b.City = a.City
                             AND b.Country = a.Country
                             AND b.Population = a.Population
                             AND b.Month = a.Month
                             AND b.Money = a.Money
                             AND b.Id = a.Id
                             AND b.NullableValue = a.NullableValue
                       ) THEN 'Y' ELSE 'N' END AS NotExistsResult,
                       (
                           SELECT b.City FROM #B.entities() b
                           WHERE b.Name = a.Name
                             AND b.City = a.City
                             AND b.Country = a.Country
                             AND b.Population = a.Population
                             AND b.Month = a.Month
                             AND b.Money = a.Money
                             AND b.Id = a.Id
                             AND b.NullableValue = a.NullableValue
                       ) AS Lookup
                FROM #A.entities() a
                ORDER BY a.Name
                """,
                CreateBasicSchemaProvider),
            PerformanceInspection(
                "Q229_PerformanceWindowCteSetOperation",
                """
                with ranked as (
                    select Name, Country
                    from #A.entities()
                )
                select Name, Country,
                       RowNumber() over (partition by Country order by Name) as BranchRank
                from ranked
                union (Name, Country, BranchRank)
                    select Name, Country,
                           RowNumber() over (partition by Country order by Name) as BranchRank
                    from #B.entities()
                order by Country, BranchRank, Name
                """,
                CreateBasicSchemaProvider),
            PerformanceInspection(
                "Q230_PerformanceTableProjection",
                """
                select Name, City, Population
                from #A.entities()
                where Population > 0
                """,
                CreateBasicSchemaProvider,
                new CompilationOptions().WithTableResultMaterialization())
        ];
    }

    private static GeneratedCodeSample PerformanceInspection(
        string name,
        string query,
        Func<ISchemaProvider> createSchemaProvider,
        CompilationOptions? compilationOptions = null)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "Performance",
            Format = GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode,
            CreateSchemaProvider = createSchemaProvider,
            CompilationOptions = compilationOptions ?? new CompilationOptions()
        };
    }

    private static PerformanceReflectedSchemaProvider CreatePerformanceReflectedSchemaProvider()
    {
        return new PerformanceReflectedSchemaProvider(
            new Dictionary<string, IReadOnlyList<PerformanceReflectedEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = [],
                ["#A"] = [],
                ["B"] = [],
                ["#B"] = []
            });
    }

    private sealed class PerformanceReflectedEntity
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public int Population { get; init; }
    }

    private sealed class PerformanceReflectedSchemaProvider(
        IReadOnlyDictionary<string, IReadOnlyList<PerformanceReflectedEntity>> rowsBySchema) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!rowsBySchema.TryGetValue(schema, out var rows))
                throw new NotSupportedException(schema);

            return new PerformanceReflectedSchema(schema.TrimStart('#'), rows);
        }
    }

    private sealed class PerformanceReflectedSchema(
        string name,
        IReadOnlyList<PerformanceReflectedEntity> rows) : SchemaBase(name, CreatePerformanceLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return new PerformanceReflectedTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, PerformanceReflectedEntity>(
                    name,
                    new PerformanceReflectedRowSource(rows));

            throw new NotSupportedException(name);
        }
    }

    private sealed class PerformanceReflectedTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(PerformanceReflectedEntity.Id), 0, typeof(int)),
            new SchemaColumn(nameof(PerformanceReflectedEntity.Name), 1, typeof(string)),
            new SchemaColumn(nameof(PerformanceReflectedEntity.City), 2, typeof(string)),
            new SchemaColumn(nameof(PerformanceReflectedEntity.Country), 3, typeof(string)),
            new SchemaColumn(nameof(PerformanceReflectedEntity.Population), 4, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(PerformanceReflectedEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class PerformanceReflectedRowSource(
        IReadOnlyList<PerformanceReflectedEntity> rows) : RowSourceBase<PerformanceReflectedEntity>
    {
        protected override void CollectChunks(IChunkWriter<PerformanceReflectedEntity> writer)
        {
            writer.Write(rows.ToArray());
        }
    }

    private static MethodsAggregator CreatePerformanceLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new PerformanceLibrary());
        return new MethodsAggregator(methodsManager);
    }

    private sealed class PerformanceLibrary : LibraryBase;
}
