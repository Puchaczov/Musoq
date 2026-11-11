using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourceColumnRef
{
    public SourceColumnRef(string name)
        : this(name, null) { }

    public SourceColumnRef(string name, IReadOnlyDictionary<string, string>? readModifiers)
    {
        Name = name;
        ReadModifiers = ColumnReadModifiers.Create(readModifiers);
    }

    public string Name { get; init; }

    /// <summary>Authoritative CLR type identity for compilation-time provider binding.</summary>
    public string? ValueTypeName { get; init; }

    private IReadOnlyDictionary<string, string> _readModifiers = ColumnReadModifiers.Create(null);

    public IReadOnlyDictionary<string, string> ReadModifiers
    {
        get => _readModifiers;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _readModifiers = ColumnReadModifiers.Create(value);
        }
    }
}
