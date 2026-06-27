using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record FieldBinding
{
    public FieldBinding(
        string name,
        string qualifiedName,
        int outputIndex,
        Type type,
        FieldNullability nullability,
        FieldAccessStrategy accessStrategy,
        Type? publicType = null,
        IReadOnlyDictionary<string, string>? readModifiers = null)
    {
        Name = name;
        QualifiedName = qualifiedName;
        OutputIndex = outputIndex;
        Type = type;
        Nullability = nullability;
        AccessStrategy = accessStrategy;
        PublicType = publicType;
        ReadModifiers = ColumnReadModifiers.Create(readModifiers);
    }

    public string Name { get; init; }

    public string QualifiedName { get; init; }

    public int OutputIndex { get; init; }

    public Type Type { get; init; }

    public FieldNullability Nullability { get; init; }

    public FieldAccessStrategy AccessStrategy { get; init; }

    public Type? PublicType { get; init; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; init; }

    public Type ColumnType => PublicType ?? Type;
}
