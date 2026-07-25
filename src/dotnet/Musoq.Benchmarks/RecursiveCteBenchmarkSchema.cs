using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public sealed record RecursiveCteBenchmarkEdge(int SourceId, int TargetId, int Weight, string Label);

internal sealed class RecursiveCteBenchmarkSchemaProvider : ISchemaProvider
{
    private readonly RecursiveCteBenchmarkSchema _schema;

    public RecursiveCteBenchmarkSchemaProvider(IReadOnlyList<RecursiveCteBenchmarkEdge> edges)
    {
        _schema = new RecursiveCteBenchmarkSchema(edges);
    }

    public ISchema GetSchema(string schema) => _schema;
}

internal sealed class RecursiveCteBenchmarkSchema(IReadOnlyList<RecursiveCteBenchmarkEdge> edges)
    : SchemaBase("graph", CreateLibrary())
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters) => new RecursiveCteBenchmarkEdgeTable();

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        var sourceId = name.Equals("neighbors", StringComparison.OrdinalIgnoreCase) &&
                       parameters is [int value, ..]
            ? value
            : (int?)null;
        return EnsureSourceType<T, RecursiveCteBenchmarkEdge>(
            name,
            new RecursiveCteBenchmarkEdgeSource(edges, sourceId));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(manager);
    }
}

internal sealed class RecursiveCteBenchmarkEdgeTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(RecursiveCteBenchmarkEdge.SourceId), 0, typeof(int)),
        new SchemaColumn(nameof(RecursiveCteBenchmarkEdge.TargetId), 1, typeof(int)),
        new SchemaColumn(nameof(RecursiveCteBenchmarkEdge.Weight), 2, typeof(int)),
        new SchemaColumn(nameof(RecursiveCteBenchmarkEdge.Label), 3, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(RecursiveCteBenchmarkEdge));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

internal sealed class RecursiveCteBenchmarkEdgeSource(
    IReadOnlyList<RecursiveCteBenchmarkEdge> edges,
    int? sourceId) : RowSource<RecursiveCteBenchmarkEdge>
{
    public override IEnumerable<IReadOnlyList<RecursiveCteBenchmarkEdge>> Chunks
    {
        get
        {
            if (!sourceId.HasValue)
            {
                yield return edges;
                yield break;
            }

            var matches = new List<RecursiveCteBenchmarkEdge>();
            foreach (var edge in edges)
                if (edge.SourceId == sourceId.Value)
                    matches.Add(edge);
            yield return matches;
        }
    }
}
