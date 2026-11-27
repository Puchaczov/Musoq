using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal sealed class ByteBufferFlowValue(int identity, MethodCall? producer)
{
    // An ordinary datasource column has a known origin, but never owns a planned representation.
    internal static readonly ByteBufferFlowValue External = new(-1, null)
    {
        Requirement = ByteBufferRequirement.Complete,
    };

    internal int Identity { get; } = identity;
    internal MethodCall? Producer { get; } = producer;
    internal ByteBufferFlowValue? Input { get; set; }
    internal ByteBufferRequirement Requirement { get; private set; } = ByteBufferRequirement.Prefix(0);
    internal bool IsParsedField => Producer is null && !ReferenceEquals(this, External);
    internal bool StoresLength => IsParsedField && Requirement == ByteBufferRequirement.Length;
    internal bool UsesView => !IsParsedField && !Requirement.RequiresCompleteArray && Requirement.ObservesLength;

    internal void Require(ByteBufferRequirement requirement)
    {
        if (!ReferenceEquals(this, External))
            Requirement = Requirement.Combine(IsParsedField ? ByteBufferRequirement.Complete : requirement);
    }

    internal void ObserveLength()
    {
        if (!ReferenceEquals(this, External))
            Requirement = Requirement.Combine(ByteBufferRequirement.Length);
    }
}

/// <summary>Field bindings for one physical row boundary, including qualified intermediate names.</summary>
internal sealed class ByteBufferFlowBindings
{
    private readonly Dictionary<string, ByteBufferFlowValue?> _names = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _aliases = new(StringComparer.OrdinalIgnoreCase);
    internal IReadOnlyList<(string Name, ByteBufferFlowValue? Value)> Columns { get; private set; } = [];
    internal IEnumerable<ByteBufferFlowValue> Values =>
        _names.Values.Concat(Columns.Select(column => column.Value)).OfType<ByteBufferFlowValue>().Distinct();

    internal ByteBufferFlowValue? Resolve(ColumnRef column)
    {
        if (_names.TryGetValue($"{column.Alias}.{column.ColumnName}", out var qualified))
            return qualified;
        if (
            column.ColumnName.Contains('.', StringComparison.Ordinal)
            || string.IsNullOrEmpty(column.Alias)
            || _aliases.Contains(column.Alias)
        )
            return _names.GetValueOrDefault(column.ColumnName);
        return null;
    }

    internal ByteBufferFlowValue? ResolveName(string name) => _names.GetValueOrDefault(name);

    // Some source schemas bind a complete member path as one scalar column.
    // Observing the scalar still requires the receiver from which that member is read.
    internal void ObserveFieldPath(ColumnRef column)
    {
        if (Resolve(column) is { IsParsedField: true } direct)
            direct.Require(ByteBufferRequirement.Complete);
        var path = string.IsNullOrEmpty(column.Alias) ? column.ColumnName : $"{column.Alias}.{column.ColumnName}";
        RequirePathReceivers(path);
        if (path.Length != column.ColumnName.Length)
            RequirePathReceivers(column.ColumnName);
    }

    private void RequirePathReceivers(string path)
    {
        for (var i = 1; i < path.Length; ++i)
            if (path[i] is '.' or '[' && ResolveName(path[..i]) is { IsParsedField: true } value)
                value.Require(ByteBufferRequirement.Complete);
    }

    internal void RequireComplete()
    {
        foreach (var value in Values)
            value.Require(ByteBufferRequirement.Complete);
    }

    internal static ByteBufferFlowBindings Source(
        OutputSchema schema,
        string alias,
        ByteBufferFlowBindings? producer = null,
        bool identifyExternalBuffers = false
    )
    {
        var result = new ByteBufferFlowBindings();
        result._aliases.Add(alias);
        result.Columns =
            schema.Columns.Length == 0 && producer is not null
                ? producer.Columns
                : schema
                    .Columns.Select(column =>
                        (
                            column.Name,
                            identifyExternalBuffers && producer is null && column.Type == typeof(byte[])
                                ? ByteBufferFlowValue.External
                                : producer?.ResolveName(column.Name)
                        )
                    )
                    .ToArray();
        if (
            producer is not null
            && schema.Columns.Any(column => column.Type == typeof(byte[]) && producer.ResolveName(column.Name) is null)
        )
            producer.RequireComplete();
        foreach (var (name, value) in result.Columns)
        {
            result.Add(name, value);
            result.Add($"{alias}.{name}", value);
        }
        return result;
    }

    internal void BindParsedFields(string alias, IReadOnlyDictionary<string, ByteBufferFlowValue> fields)
    {
        foreach (var (name, value) in fields)
        {
            _names[name] = value;
            _names[$"{alias}.{name}"] = value;
        }
        Columns = Columns.Select(column => (column.Name, ResolveName(column.Name))).ToArray();
    }

    internal static ByteBufferFlowBindings Project(
        IReadOnlyList<ProjectedField> fields,
        IReadOnlyList<ByteBufferFlowValue?> values,
        ByteBufferFlowBindings input
    )
    {
        var result = new ByteBufferFlowBindings();
        result._aliases.UnionWith(input._aliases);
        result.Columns = fields.Select((field, index) => (field.OutputName, values[index])).ToArray();
        foreach (var (name, value) in result.Columns)
        {
            result.Add(name, value);
            if (result._aliases.Count == 1 && !name.Contains('.', StringComparison.Ordinal))
                result.Add($"{result._aliases.Single()}.{name}", value);
        }
        return result;
    }

    internal static ByteBufferFlowBindings Merge(IEnumerable<ByteBufferFlowBindings> inputs)
    {
        var result = new ByteBufferFlowBindings();
        var columns = new List<(string, ByteBufferFlowValue?)>();
        foreach (var input in inputs)
        {
            result._aliases.UnionWith(input._aliases);
            columns.AddRange(input.Columns);
            foreach (var (name, value) in input._names)
                result.Add(name, value);
        }
        result.Columns = columns;
        return result;
    }

    private void Add(string name, ByteBufferFlowValue? value)
    {
        if (_names.TryGetValue(name, out var previous) && !ReferenceEquals(previous, value))
            value = null;
        _names[name] = value;
    }
}
