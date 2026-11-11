using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Interpreters;

/// <summary>Immutable declaration-order field slots shared by generated interpreter instances.</summary>
public sealed class ParserCompletionLayout
{
    private readonly Dictionary<string, int> _indexes = new(StringComparer.Ordinal);

    /// <summary>Copies and validates the declared field names.</summary>
    public ParserCompletionLayout(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var values = names.ToArray();
        for (var index = 0; index < values.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(values[index]);
            if (!_indexes.TryAdd(values[index], index))
                throw new ArgumentException("Completion field names must be unique.", nameof(names));
        }
        Names = Array.AsReadOnly(values);
    }

    /// <summary>Gets the immutable field slot order.</summary>
    public IReadOnlyList<string> Names { get; }

    internal int IndexOf(string? name) => name is not null && _indexes.TryGetValue(name, out var index) ? index : -1;
}
