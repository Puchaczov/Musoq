using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.Generated;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static ISchemaProvider CreateLoopInvariantSampleSchemaProvider()
    {
        return new LoopInvariantSampleSchemaProvider();
    }

    private sealed class LoopInvariantSampleSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "licm", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "#licm", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(schema);

            return new LoopInvariantSampleSchema();
        }
    }

    private sealed class LoopInvariantSampleSchema : SchemaBase
    {
        public LoopInvariantSampleSchema()
            : base("licm", CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "outers", StringComparison.OrdinalIgnoreCase))
                return new LoopInvariantSampleTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "outers", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, LoopInvariantSampleOuter>(name, new LoopInvariantSampleRowSource());

            throw new NotSupportedException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new LoopInvariantSampleLibrary());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class LoopInvariantSampleTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(LoopInvariantSampleOuter.Id), 0, typeof(int)),
            new SchemaColumn(nameof(LoopInvariantSampleOuter.Value), 1, typeof(int)),
            new SchemaColumn(nameof(LoopInvariantSampleOuter.VolatileValue), 2, typeof(int), (string?)null, ColumnStability.Volatile),
            new SchemaColumn(nameof(LoopInvariantSampleOuter.Middles), 3, typeof(LoopInvariantSampleMiddle[])),
            new SchemaColumn(nameof(LoopInvariantSampleOuter.EmptyMiddles), 4, typeof(LoopInvariantSampleMiddle[]))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(LoopInvariantSampleOuter));

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private sealed class LoopInvariantSampleRowSource : RowSourceBase<LoopInvariantSampleOuter>
    {
        protected override void CollectChunks(IChunkWriter<LoopInvariantSampleOuter> writer)
        {
            writer.Write(
            [
                CreateOuter(1),
                CreateOuter(2)
            ]);
        }

        private static LoopInvariantSampleOuter CreateOuter(int id)
        {
            return new LoopInvariantSampleOuter
            {
                Id = id,
                Middles = Enumerable.Range(1, 3)
                    .Select(middleId => new LoopInvariantSampleMiddle
                    {
                        Id = id * 100 + middleId,
                        Leaves = Enumerable.Range(1, 4)
                            .Select(leafId => new LoopInvariantSampleLeaf
                            {
                                Id = id * 1000 + middleId * 10 + leafId
                            })
                            .ToArray()
                    })
                    .ToArray(),
                EmptyMiddles = []
            };
        }
    }

}

public sealed class LoopInvariantSampleLibrary : LibraryBase
{
    [BindableMethod]
    public int StableOf(int value)
    {
        LoopInvariantSampleCounters.StableOfCalls++;
        return value + 1;
    }

    [BindableMethod]
    public int StablePair(int outerValue, int middleValue)
    {
        LoopInvariantSampleCounters.StablePairCalls++;
        return outerValue + middleValue;
    }

    [BindableMethod]
    [NonDeterministic]
    public int VolatileOf(int value)
    {
        LoopInvariantSampleCounters.VolatileOfCalls++;
        return value + 2;
    }
}
