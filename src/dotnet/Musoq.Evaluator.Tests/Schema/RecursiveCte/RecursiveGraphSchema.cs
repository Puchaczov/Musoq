using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.RecursiveCte;

internal sealed class RecursiveGraphSchema(
    RecursiveGraphData data,
    RecursiveGraphSourceRecorder recorder) : SchemaBase("graph", CreateLibrary())
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters) => name.ToLowerInvariant() switch
    {
        "roots" => RecursiveGraphRootTable.Instance,
        "edges" or "neighbors" => RecursiveGraphEdgeTable.Instance,
        _ => throw new NotSupportedException($"Unknown recursive graph source '{name}'.")
    };

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        recorder.SourceCreated(name);

        if (name.Equals("roots", StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, RecursiveGraphRoot>(
                name,
                new ObservableRowSource<RecursiveGraphRoot>(name, () => [data.GetRoots()], recorder));

        if (name.Equals("edges", StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, RecursiveGraphEdge>(
                name,
                new ObservableRowSource<RecursiveGraphEdge>(
                    name,
                    data.GetEdgeChunks,
                    recorder,
                    data.EdgesEnumerationCompleted));

        if (name.Equals("neighbors", StringComparison.OrdinalIgnoreCase))
        {
            recorder.NeighborInvoked();
            var sourceId = Convert.ToInt32(parameters.Single(), System.Globalization.CultureInfo.InvariantCulture);
            return EnsureSourceType<T, RecursiveGraphEdge>(
                name,
                new ObservableRowSource<RecursiveGraphEdge>(
                    name,
                    () => [data.GetNeighbors(sourceId)],
                    recorder));
        }

        throw new NotSupportedException($"Unknown recursive graph source '{name}'.");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(manager);
    }

    private sealed class ObservableRowSource<T>(
        string sourceName,
        Func<IEnumerable<IReadOnlyList<T>>> chunksFactory,
        RecursiveGraphSourceRecorder recorder,
        Action? enumerationCompleted = null) : RowSource<T>
    {
        public override IEnumerable<IReadOnlyList<T>> Chunks => Enumerate();

        private IEnumerable<IReadOnlyList<T>> Enumerate()
        {
            recorder.EnumerationStarted(sourceName);
            try
            {
                foreach (var rows in chunksFactory())
                {
                    recorder.RowsProduced(sourceName, rows.Count);
                    yield return rows;
                }
            }
            finally
            {
                enumerationCompleted?.Invoke();
                recorder.EnumerationDisposed(sourceName);
            }
        }
    }
}

internal sealed class RecursiveGraphRootTable : ISchemaTable
{
    public static RecursiveGraphRootTable Instance { get; } = new();

    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(RecursiveGraphRoot.RootId), 0, typeof(int))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(RecursiveGraphRoot));

    public ISchemaColumn? GetColumnByName(string name) => Columns.SingleOrDefault(column => column.ColumnName == name);

    public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
}

internal sealed class RecursiveGraphEdgeTable : ISchemaTable
{
    public static RecursiveGraphEdgeTable Instance { get; } = new();

    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(RecursiveGraphEdge.SourceId), 0, typeof(int)),
        new SchemaColumn(nameof(RecursiveGraphEdge.TargetId), 1, typeof(int)),
        new SchemaColumn(nameof(RecursiveGraphEdge.Weight), 2, typeof(decimal)),
        new SchemaColumn(nameof(RecursiveGraphEdge.Label), 3, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(RecursiveGraphEdge));

    public ISchemaColumn? GetColumnByName(string name) => Columns.SingleOrDefault(column => column.ColumnName == name);

    public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
}
