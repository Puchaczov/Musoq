using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Benchmarks.Scenarios;

public sealed class FixtureProvider(int count, int payloadValueOffset = 0) : ISchemaProvider
{
    private readonly FixtureSchema _schema = new(count, payloadValueOffset);

    public ISchema GetSchema(string schema) => string.Equals(schema.TrimStart('#'), "bench", StringComparison.OrdinalIgnoreCase)
        ? _schema : throw new InvalidOperationException($"Unknown benchmark schema '{schema}'.");
}

public sealed partial class FixtureSchema : SchemaBase
{
    private const int CollectionRootNodes = 1;
    private const int NumberInputNodesPerRecord = 3;
    private readonly Dictionary<string, FixtureRows> _sources;

    public FixtureSchema(int count, int payloadValueOffset = 0) : base("bench", CreateLibrary())
    {
        AddTypedSource<StructuredRows>("structured", new TypedSourceRegistrationOptions(StructuredLimits(count)));
        var lookup = FixtureData.Rows(3);
        _sources = new Dictionary<string, FixtureRows>(StringComparer.OrdinalIgnoreCase)
        {
            ["rows"] = new(FixtureData.Rows(count, payloadValueOffset)),
            ["skewed"] = new(FixtureData.Rows(count, payloadValueOffset).Select(row => row with { Key = row.Id % 10 == 0 ? row.Id % 4 : 0 }).ToArray()),
            ["planned"] = new(FixtureData.Rows(count, payloadValueOffset)),
            ["lookup"] = new(lookup),
            ["reconcile"] = new([.. lookup, FixtureData.Create(4) with { Key = 4 }]),
            ["twice"] = new(lookup.SelectMany(row => new[] { row, row }).ToArray()),
            ["empty"] = new([])
        };
        var stream = FixtureData.Rows(count, payloadValueOffset).SelectMany(row => row.Payload)
            .Concat(FixtureData.Create(0, payloadValueOffset).Truncated).ToArray();
        _sources.Add("stream", new FixtureRows([FixtureData.Create(0, payloadValueOffset) with { Payload = stream }]));
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
            return new FixtureTable(typeof(NumberInput));
        return new FixtureTable(typeof(FixtureRow));
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

public sealed class FixtureTable(Type rowType) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = rowType.GetProperties()
        .Select((property, index) => (ISchemaColumn)new SchemaColumn(property.Name, index, property.PropertyType)).ToArray();
    public SchemaTableMetadata Metadata { get; } = new(rowType);
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
