using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Benchmarks.Scenarios;

internal sealed class PerformanceProvider(PerformanceRow[] rows, PerformanceRow[] right, string profile)
    : ISchemaProvider
{
    private readonly PerformanceSchema _schema = new(rows, right, profile);

    public ISchema GetSchema(string schema) =>
        schema.TrimStart('#').Equals("perf", StringComparison.OrdinalIgnoreCase)
            ? _schema
            : throw new InvalidOperationException($"Unknown performance schema '{schema}'.");
}

public sealed class PerformanceSchema : SchemaBase
{
    private readonly PerformanceRows _rows;
    private readonly PerformanceRows _right;
    private readonly string _profile;

    public PerformanceSchema(PerformanceRow[] rows, PerformanceRow[] right, string profile)
        : base("perf", CreateLibrary())
    {
        _rows = new(rows);
        _right = new(right);
        _profile = profile;
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext context,
        params object?[] parameters
    ) =>
        name is "rows" or "right"
            ? new PerformanceTable()
            : throw new InvalidOperationException($"Unknown performance source '{name}'.");

    public override RowSource<T> GetRowSource<T, TArguments>(
        string name,
        SourceExecutionContext context,
        TArguments arguments
    ) =>
        EnsureSourceType<T, PerformanceRow>(
            name,
            name switch
            {
                "rows" => _rows,
                "right" => _right,
                _ => throw new InvalidOperationException($"Unknown performance source '{name}'."),
            }
        );

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters
    ) =>
        base.DescribeSource(name, context, parameters) with
        {
            ExecutionPermissions =
                _profile == "scalar-serial"
                    ? SourceExecutionPermissions.None
                    : SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing,
        };

    private static MethodsAggregator CreateLibrary()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new PerformanceLibrary());
        return new(manager);
    }
}

internal sealed class PerformanceRows(PerformanceRow[] rows) : RowSource<PerformanceRow>
{
    private readonly PerformanceRow[][] _chunks = rows.Chunk(4096).ToArray();
    public override IEnumerable<IReadOnlyList<PerformanceRow>> Chunks => _chunks;
}

internal sealed class PerformanceTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(PerformanceRow.Id), 0, typeof(int)),
        new SchemaColumn(nameof(PerformanceRow.StringKey), 1, typeof(string)),
        new SchemaColumn(nameof(PerformanceRow.Partition), 2, typeof(int)),
        new SchemaColumn(nameof(PerformanceRow.Subkey), 3, typeof(int?)),
        new SchemaColumn(nameof(PerformanceRow.Amount), 4, typeof(decimal)),
        new SchemaColumn(nameof(PerformanceRow.GroupKey), 5, typeof(string)),
        new SchemaColumn(nameof(PerformanceRow.Bucket), 6, typeof(int)),
        new SchemaColumn(nameof(PerformanceRow.Text), 7, typeof(string)),
        new SchemaColumn(nameof(PerformanceRow.Items), 8, typeof(int[])),
        new SchemaColumn(nameof(PerformanceRow.Payload), 9, typeof(byte[])),
    ];
    public SchemaTableMetadata Metadata { get; } = new(typeof(PerformanceRow));

    public ISchemaColumn? GetColumnByName(string name) => Columns.SingleOrDefault(column => column.ColumnName == name);

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => column.ColumnName == name).ToArray();
}
