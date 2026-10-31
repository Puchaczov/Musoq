using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Benchmarks.Scenarios;

public sealed class FixtureProvider : ISchemaProvider
{
    private readonly FixtureSchema _schema;

    public FixtureProvider(int count, int payloadValueOffset = 0)
        : this(new FixtureSchema(count, payloadValueOffset)) { }

    internal FixtureProvider(FixtureSchema schema) => _schema = schema;

    public ISchema GetSchema(string schema) => string.Equals(schema.TrimStart('#'), "bench", StringComparison.OrdinalIgnoreCase)
        ? _schema : throw new InvalidOperationException($"Unknown benchmark schema '{schema}'.");
}

public sealed partial class FixtureSchema : SchemaBase
{
    private const int CollectionRootNodes = 1;
    private const int NumberInputNodesPerRecord = 3;
    private readonly Dictionary<string, FixtureRows> _sources;

    public FixtureSchema(int count, int payloadValueOffset = 0)
        : this(FixtureInputs.Create(count, payloadValueOffset), StructuredLimits(count)) { }

    internal FixtureSchema(FixtureInputs inputs, StructuralInputLimits limits) : base("bench", CreateLibrary())
    {
        AddTypedSource<StructuredRows>("structured", new TypedSourceRegistrationOptions(limits));
        _sources = inputs.Sources;
    }

    /// <summary>Allows a collection of NumberInput records with two scalar fields, keeping other safety defaults.</summary>
    public static StructuralInputLimits StructuredLimits(int inputCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputCount);
        var defaults = StructuralInputLimits.Default;
        var requiredNodes = checked(CollectionRootNodes + inputCount * NumberInputNodesPerRecord);
        return new StructuralInputLimits(defaults.MaxDepth, Math.Max(defaults.MaxNodes, requiredNodes), defaults.MaxStringBytes);
    }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext context,
        params object?[] parameters)
    {
        if (name.Equals("structured", StringComparison.OrdinalIgnoreCase))
            return FixtureTable.NumberInput();
        return FixtureTable.Rows();
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context,
        TArguments arguments)
    {
        if (name.Equals("planned", StringComparison.OrdinalIgnoreCase) && context.Plan.AcceptedPredicate is SourcePredicateStringMatch match)
            return EnsureSourceType<T, FixtureRow>(name, _sources[name].FilterPrefix(match.Needle));
        return EnsureSourceType<T, FixtureRow>(name, _sources[name]);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methods);
    }
}

public sealed class FixtureTable : ISchemaTable
{
    private FixtureTable(Type rowType, ISchemaColumn[] columns)
    {
        Columns = columns;
        Metadata = new(rowType);
    }

    public static FixtureTable Rows() => new(typeof(FixtureRow),
    [
        new SchemaColumn(nameof(FixtureRow.Id), 0, typeof(int)),
        new SchemaColumn(nameof(FixtureRow.Key), 1, typeof(int)),
        new SchemaColumn(nameof(FixtureRow.Amount), 2, typeof(decimal)),
        new SchemaColumn(nameof(FixtureRow.Name), 3, typeof(string)),
        new SchemaColumn(nameof(FixtureRow.Optional), 4, typeof(int?)),
        new SchemaColumn(nameof(FixtureRow.Items), 5, typeof(int[])),
        new SchemaColumn(nameof(FixtureRow.Payload), 6, typeof(byte[])),
        new SchemaColumn(nameof(FixtureRow.Text), 7, typeof(string)),
        new SchemaColumn(nameof(FixtureRow.Time), 8, typeof(DateTime)),
        new SchemaColumn(nameof(FixtureRow.State), 9, typeof(JobState)),
        new SchemaColumn(nameof(FixtureRow.Truncated), 10, typeof(byte[]))
    ]);

    public static FixtureTable NumberInput() => new(typeof(global::Musoq.Benchmarks.Scenarios.NumberInput),
    [
        new SchemaColumn(nameof(global::Musoq.Benchmarks.Scenarios.NumberInput.Id), 0, typeof(int)),
        new SchemaColumn(nameof(global::Musoq.Benchmarks.Scenarios.NumberInput.Amount), 1, typeof(decimal))
    ]);

    public ISchemaColumn[] Columns { get; }
    public SchemaTableMetadata Metadata { get; }
    public ISchemaColumn? GetColumnByName(string name) => Columns.FirstOrDefault(column => column.ColumnName == name);
    public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
}

public sealed class FixtureRows : RowSource<FixtureRow>
{
    private readonly FixtureRow[][] _chunks;
    public FixtureRows(FixtureRow[] rows) => _chunks = rows.Chunk(4_096).ToArray();
    public override IEnumerable<IReadOnlyList<FixtureRow>> Chunks => _chunks;
    public FixtureRows FilterPrefix(string prefix) => new(_chunks.SelectMany(chunk => chunk)
        .Where(row => row.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray());
}
