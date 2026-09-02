using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static IReadOnlyList<GeneratedCodeSample> CreatePerformanceSamples()
    {
        return
        [
            PerformanceInspection(
                "Q227_PerformanceJoinAggregate",
                EvaluatorPerformanceQueries.Q227,
                CreatePerformanceJoinSchemaProvider),
            PerformanceInspection(
                "Q228_PerformanceWideCorrelatedSubquery",
                EvaluatorPerformanceQueries.Q228,
                CreateBasicSchemaProvider),
            PerformanceInspection(
                "Q229_PerformanceWindowCteSetOperation",
                EvaluatorPerformanceQueries.Q229,
                CreateBasicSchemaProvider),
            PerformanceInspection(
                "Q230_PerformanceTableProjection",
                EvaluatorPerformanceQueries.Q230,
                CreateBasicSchemaProvider,
                new CompilationOptions()
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse())
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
            CompilationOptions = compilationOptions ?? new CompilationOptions().WithStabilityAwareScalarReuse()
        };
    }

    private static PerformanceJoinSchemaProvider CreatePerformanceJoinSchemaProvider()
    {
        return new PerformanceJoinSchemaProvider(
            new Dictionary<string, IReadOnlyList<PerformanceJoinEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = [],
                ["#A"] = [],
                ["B"] = [],
                ["#B"] = []
            });
    }

    private sealed class PerformanceJoinSchemaProvider(
        IReadOnlyDictionary<string, IReadOnlyList<PerformanceJoinEntity>> rowsBySchema) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!rowsBySchema.TryGetValue(schema, out var rows))
                throw new NotSupportedException(schema);

            return new PerformanceJoinSchema(schema.TrimStart('#'), rows);
        }
    }

    private sealed class PerformanceJoinSchema(
        string name,
        IReadOnlyList<PerformanceJoinEntity> rows) : SchemaBase(name, CreatePerformanceLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return new PerformanceJoinTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, PerformanceJoinEntity>(
                    name,
                    new PerformanceJoinRowSource(rows));

            throw new NotSupportedException(name);
        }
    }

    private sealed class PerformanceJoinTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(PerformanceJoinEntity.Id), 0, typeof(int)),
            new SchemaColumn(nameof(PerformanceJoinEntity.Name), 1, typeof(string)),
            new SchemaColumn(nameof(PerformanceJoinEntity.City), 2, typeof(string)),
            new SchemaColumn(nameof(PerformanceJoinEntity.Country), 3, typeof(string)),
            new SchemaColumn(nameof(PerformanceJoinEntity.Population), 4, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(PerformanceJoinEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class PerformanceJoinRowSource(
        IReadOnlyList<PerformanceJoinEntity> rows) : RowSourceBase<PerformanceJoinEntity>
    {
        protected override void CollectChunks(IChunkWriter<PerformanceJoinEntity> writer)
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

public sealed class PerformanceJoinEntity
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public int Population { get; init; }
}
