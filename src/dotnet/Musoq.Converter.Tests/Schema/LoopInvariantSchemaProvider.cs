using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Converter.Tests.Schema;

public sealed class LoopInvariantSchemaProvider : ISchemaProvider
{
    private readonly LoopInvariantSchema _schema = new();

    public ISchema GetSchema(string schema)
    {
        if (!LoopInvariantSchema.MatchesName(schema))
            throw new NotSupportedException(schema);

        return _schema;
    }
}

public sealed class LoopInvariantSchema : SchemaBase
{
    public const string SchemaName = "licm";

    public LoopInvariantSchema()
        : base(SchemaName, CreateLibrary())
    {
    }

    public static bool MatchesName(string schema)
    {
        return string.Equals(schema, SchemaName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(schema, $"#{SchemaName}", StringComparison.OrdinalIgnoreCase);
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "outers", StringComparison.OrdinalIgnoreCase))
            return new LoopInvariantTable();

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "outers", StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, LoopInvariantOuter>(name, new LoopInvariantRowSource());

        throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LoopInvariantLibrary());
        return new MethodsAggregator(methodsManager);
    }

}

public sealed class LoopInvariantTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(LoopInvariantOuter.Id), 0, typeof(int)),
        new SchemaColumn(nameof(LoopInvariantOuter.Value), 1, typeof(int)),
        new SchemaColumn(nameof(LoopInvariantOuter.VolatileValue), 2, typeof(int), ColumnStability.Volatile),
        new SchemaColumn(nameof(LoopInvariantOuter.Middles), 3, typeof(LoopInvariantMiddle[])),
        new SchemaColumn(nameof(LoopInvariantOuter.EmptyMiddles), 4, typeof(LoopInvariantMiddle[]))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(LoopInvariantOuter));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

public sealed class LoopInvariantRowSource : RowSourceBase<LoopInvariantOuter>
{
    protected override void CollectChunks(IChunkWriter<LoopInvariantOuter> writer)
    {
        writer.Write(LoopInvariantData.CreateRows());
    }
}

public sealed class LoopInvariantOuter
{
    public int Id { get; init; }

    public int Value => LoopInvariantCounters.ReadOuterValue(Id);

    [NonDeterministic]
    public int VolatileValue => LoopInvariantCounters.ReadOuterVolatileValue(Id);

    public LoopInvariantMiddle[] Middles { get; init; } = [];

    public LoopInvariantMiddle[] EmptyMiddles { get; init; } = [];
}

public sealed class LoopInvariantMiddle
{
    public int Id { get; init; }

    public int Value => LoopInvariantCounters.ReadMiddleValue(Id);

    [NonDeterministic]
    public int VolatileValue => LoopInvariantCounters.ReadMiddleVolatileValue(Id);

    public LoopInvariantLeaf[] Leaves { get; init; } = [];
}

public sealed class LoopInvariantLeaf
{
    public int Id { get; init; }

    public int Value => LoopInvariantCounters.ReadLeafValue(Id);

    [NonDeterministic]
    public int VolatileValue => LoopInvariantCounters.ReadLeafVolatileValue(Id);
}

public sealed class LoopInvariantLibrary : LibraryBase
{
    [BindableMethod]
    public int StableOf(int value) => LoopInvariantCounters.ReadStableOuterFunction(value);

    [BindableMethod]
    public int StablePair(int outerValue, int middleValue) =>
        LoopInvariantCounters.ReadStablePairFunction(outerValue, middleValue);

    [BindableMethod]
    [NonDeterministic]
    public int VolatileOf(int value) => LoopInvariantCounters.ReadVolatileFunction(value);
}

public static class LoopInvariantCounters
{
    private static int _outerValue;
    private static int _outerVolatileValue;
    private static int _middleValue;
    private static int _middleVolatileValue;
    private static int _leafValue;
    private static int _leafVolatileValue;
    private static int _stableOuterFunction;
    private static int _stablePairFunction;
    private static int _volatileFunction;

    public static int OuterValueReads => Volatile.Read(ref _outerValue);
    public static int OuterVolatileValueReads => Volatile.Read(ref _outerVolatileValue);
    public static int MiddleValueReads => Volatile.Read(ref _middleValue);
    public static int MiddleVolatileValueReads => Volatile.Read(ref _middleVolatileValue);
    public static int LeafValueReads => Volatile.Read(ref _leafValue);
    public static int LeafVolatileValueReads => Volatile.Read(ref _leafVolatileValue);
    public static int StableOuterFunctionCalls => Volatile.Read(ref _stableOuterFunction);
    public static int StablePairFunctionCalls => Volatile.Read(ref _stablePairFunction);
    public static int VolatileFunctionCalls => Volatile.Read(ref _volatileFunction);

    public static void Reset()
    {
        Interlocked.Exchange(ref _outerValue, 0);
        Interlocked.Exchange(ref _outerVolatileValue, 0);
        Interlocked.Exchange(ref _middleValue, 0);
        Interlocked.Exchange(ref _middleVolatileValue, 0);
        Interlocked.Exchange(ref _leafValue, 0);
        Interlocked.Exchange(ref _leafVolatileValue, 0);
        Interlocked.Exchange(ref _stableOuterFunction, 0);
        Interlocked.Exchange(ref _stablePairFunction, 0);
        Interlocked.Exchange(ref _volatileFunction, 0);
    }

    internal static int ReadOuterValue(int id)
    {
        Interlocked.Increment(ref _outerValue);
        return id * 10;
    }

    internal static int ReadOuterVolatileValue(int id)
    {
        Interlocked.Increment(ref _outerVolatileValue);
        return id * 10;
    }

    internal static int ReadMiddleValue(int id)
    {
        Interlocked.Increment(ref _middleValue);
        return id;
    }

    internal static int ReadMiddleVolatileValue(int id)
    {
        Interlocked.Increment(ref _middleVolatileValue);
        return id;
    }

    internal static int ReadLeafValue(int id)
    {
        Interlocked.Increment(ref _leafValue);
        return id;
    }

    internal static int ReadLeafVolatileValue(int id)
    {
        Interlocked.Increment(ref _leafVolatileValue);
        return id;
    }

    internal static int ReadStableOuterFunction(int value)
    {
        Interlocked.Increment(ref _stableOuterFunction);
        return value;
    }

    internal static int ReadStablePairFunction(int outerValue, int middleValue)
    {
        Interlocked.Increment(ref _stablePairFunction);
        return outerValue + middleValue;
    }

    internal static int ReadVolatileFunction(int value)
    {
        Interlocked.Increment(ref _volatileFunction);
        return value;
    }
}

internal static class LoopInvariantData
{
    public static LoopInvariantOuter[] CreateRows()
    {
        return Enumerable.Range(1, 2)
            .Select(outerId => new LoopInvariantOuter
            {
                Id = outerId,
                Middles = Enumerable.Range(1, 3)
                    .Select(middleId => new LoopInvariantMiddle
                    {
                        Id = outerId * 100 + middleId,
                        Leaves = Enumerable.Range(1, 4)
                            .Select(leafId => new LoopInvariantLeaf { Id = outerId * 1000 + middleId * 10 + leafId })
                            .ToArray()
                    })
                    .ToArray(),
                EmptyMiddles = []
            })
            .ToArray();
    }
}
