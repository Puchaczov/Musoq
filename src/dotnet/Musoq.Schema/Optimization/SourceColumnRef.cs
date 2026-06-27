using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourceColumnRef
{
    public SourceColumnRef(string name)
        : this(name, null)
    {
    }

    public SourceColumnRef(string name, IReadOnlyDictionary<string, string>? readModifiers)
    {
        Name = name;
        ReadModifiers = ColumnReadModifiers.Create(readModifiers);
    }

    public string Name { get; init; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; init; }
}
