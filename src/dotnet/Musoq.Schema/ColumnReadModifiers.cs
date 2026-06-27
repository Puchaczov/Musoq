using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Musoq.Schema;

public static class ColumnReadModifiers
{
    public const string Encoding = "encoding";
    public const string Culture = "culture";
    public const string Format = "format";
    public const string Trim = "trim";
    public const string SourcePrefix = "source.";

    public static IReadOnlyDictionary<string, string> Empty { get; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0, StringComparer.Ordinal));

    public static IReadOnlyDictionary<string, string> Create(IReadOnlyDictionary<string, string>? modifiers)
    {
        if (modifiers is not { Count: > 0 })
            return Empty;

        return new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(modifiers, StringComparer.Ordinal));
    }

    public static IReadOnlyDictionary<string, string> Create(IEnumerable<KeyValuePair<string, string>> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var modifier in modifiers)
            dictionary.Add(modifier.Key, modifier.Value);

        return dictionary.Count == 0
            ? Empty
            : new ReadOnlyDictionary<string, string>(dictionary);
    }
}
