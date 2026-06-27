using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionColumnMetadataField
{
    public ExecutionColumnMetadataField(
        string name,
        int index,
        Type type,
        IReadOnlyDictionary<string, string>? readModifiers = null)
    {
        Name = name;
        Index = index;
        Type = type;
        ReadModifiers = ColumnReadModifiers.Create(readModifiers);
    }

    public string Name { get; init; }

    public int Index { get; init; }

    public Type Type { get; init; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; init; }
}
