using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionColumnMetadataField
{
    public ExecutionColumnMetadataField(
        string name,
        int index,
        ExecutionTypeRef type,
        IReadOnlyDictionary<string, string>? readModifiers = null)
    {
        Name = name;
        Index = index;
        Type = type;
        ReadModifiers = ColumnReadModifiers.Create(readModifiers);
    }

    internal ExecutionColumnMetadataField(
        string name,
        int index,
        Type type,
        IReadOnlyDictionary<string, string>? readModifiers = null)
        : this(name, index, ExecutionTypeRef.FromClr(type), readModifiers)
    {
    }

    public string Name { get; init; }

    public int Index { get; init; }

    public ExecutionTypeRef Type { get; init; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; init; }
}
