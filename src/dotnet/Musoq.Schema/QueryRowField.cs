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
        : this(slot, sourceColumnIndex, name, fieldType, fieldType, null, isNullable, readModifiers,
            ColumnStability.Stable)
    {
    }

    public QueryRowField(
        int slot,
        int sourceColumnIndex,
        string name,
        Type fieldType,
        bool isNullable,
        IReadOnlyDictionary<string, string>? readModifiers,
        ColumnStability stability)
        : this(slot, sourceColumnIndex, name, fieldType, fieldType, null, isNullable, readModifiers, stability)
    {
    }

    public QueryRowField(
        int slot,
        int sourceColumnIndex,
        string name,
        Type fieldType,
        Type sourceReadType,
        EnumTypeDescriptor? enumType,
        bool isNullable,
        IReadOnlyDictionary<string, string>? readModifiers,
        ColumnStability stability)
    {
        if (slot < 0)
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "A query row slot cannot be negative.");
        if (sourceColumnIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceColumnIndex), sourceColumnIndex, "A source column index cannot be negative.");

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fieldType);
        ArgumentNullException.ThrowIfNull(sourceReadType);
        if (!IsSupportedFieldType(fieldType))
        {
            throw new ArgumentException(
                $"Field type '{fieldType}' cannot be used in a query-scoped row.",
                nameof(fieldType));
        }
        if (!IsSupportedFieldType(sourceReadType))
        {
            throw new ArgumentException(
                $"Source-read type '{sourceReadType}' cannot be used in a query-scoped row.",
                nameof(sourceReadType));
        }

        ValidateEnumContract(fieldType, sourceReadType, enumType);

        Slot = slot;
        SourceColumnIndex = sourceColumnIndex;
        Name = name;
        FieldType = fieldType;
        SourceReadType = sourceReadType;
        EnumType = enumType;
        IsNullable = isNullable;
        Stability = stability;
        ReadModifiers = FreezeModifiers(readModifiers);
    }

    public QueryRowField(
        int slot,
        int sourceColumnIndex,
        string name,
        Type fieldType,
        bool isNullable,
        ColumnStability stability,
        IReadOnlyDictionary<string, string>? readModifiers = null)
        : this(slot, sourceColumnIndex, name, fieldType, fieldType, null, isNullable, readModifiers, stability)
    {
    }

    public int Slot { get; }

    public int SourceColumnIndex { get; }

    public string Name { get; }

    public Type FieldType { get; }

    public Type SourceReadType { get; }

    public EnumTypeDescriptor? EnumType { get; }

    public bool IsNullable { get; }

    public ColumnStability Stability { get; }

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

    private static void ValidateEnumContract(
        Type fieldType,
        Type sourceReadType,
        EnumTypeDescriptor? enumType)
    {
        if (enumType == null)
        {
            if (fieldType != sourceReadType)
                throw new ArgumentException("Ordinary query-row fields must use identical field and source-read types.");
            return;
        }

        var nullableCarrier = Nullable.GetUnderlyingType(fieldType);
        var primitiveCarrier = nullableCarrier ?? fieldType;
        if (primitiveCarrier.IsEnum || primitiveCarrier != EnumScalarTypeFacts.GetCarrierType(enumType.UnderlyingKind))
            throw new ArgumentException("The query-row enum carrier does not match its descriptor backing kind.");

        var nullableSource = Nullable.GetUnderlyingType(sourceReadType);
        var primitiveSource = nullableSource ?? sourceReadType;
        if (primitiveSource.IsEnum)
        {
            if (Enum.GetUnderlyingType(primitiveSource) != primitiveCarrier ||
                (nullableCarrier == null) != (nullableSource == null))
                throw new ArgumentException("The query-row native enum source type does not match its carrier.");

            var nativeDescriptor = EnumTypeDescriptor.FromClrEnum(primitiveSource);
            if (!nativeDescriptor.Equals(enumType))
                throw new ArgumentException("The query-row enum descriptor does not match its native source type.");
        }
        else if (sourceReadType != fieldType)
        {
            throw new ArgumentException(
                "A query-row enum source type must be either its carrier or a matching native CLR enum.");
        }
    }
}
