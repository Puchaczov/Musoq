using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Musoq.Schema;

/// <summary>
/// Describes one logical field in a query-scoped source row.
/// </summary>
public sealed record QueryRowField
{
    public QueryRowField(
        int slot,
        int sourceColumnIndex,
        string name,
        Type fieldType,
        bool isNullable,
        IReadOnlyDictionary<string, string>? readModifiers = null)
    {
        if (slot < 0)
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "A query row slot cannot be negative.");
        if (sourceColumnIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceColumnIndex), sourceColumnIndex, "A source column index cannot be negative.");

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fieldType);
        if (!IsSupportedFieldType(fieldType))
        {
            throw new ArgumentException(
                $"Field type '{fieldType}' cannot be used in a query-scoped row.",
                nameof(fieldType));
        }

        Slot = slot;
        SourceColumnIndex = sourceColumnIndex;
        Name = name;
        FieldType = fieldType;
        IsNullable = isNullable;
        ReadModifiers = FreezeModifiers(readModifiers);
    }

    public int Slot { get; }

    public int SourceColumnIndex { get; }

    public string Name { get; }

    public Type FieldType { get; }

    public bool IsNullable { get; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; }

    /// <summary>
    /// Determines whether a CLR type can be referenced safely by a generated query-row carrier.
    /// </summary>
    /// <param name="fieldType">The exact CLR field type reported by source metadata.</param>
    /// <returns><see langword="true" /> when generated C# can reference the type; otherwise, <see langword="false" />.</returns>
    public static bool IsSupportedFieldType(Type fieldType)
    {
        ArgumentNullException.ThrowIfNull(fieldType);

        return fieldType != typeof(void) &&
               !fieldType.IsFunctionPointer &&
               !fieldType.IsByRef &&
               !fieldType.IsPointer &&
               !fieldType.IsByRefLike &&
               !fieldType.ContainsGenericParameters &&
               fieldType.IsVisible;
    }

    private static IReadOnlyDictionary<string, string> FreezeModifiers(
        IReadOnlyDictionary<string, string>? modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

        var copy = new Dictionary<string, string>(modifiers.Count, StringComparer.Ordinal);
        foreach (var pair in modifiers)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            ArgumentNullException.ThrowIfNull(pair.Value);
            copy[pair.Key] = pair.Value;
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}
