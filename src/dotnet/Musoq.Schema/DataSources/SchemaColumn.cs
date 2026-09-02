using System.Collections.Generic;
using System.Diagnostics;

namespace Musoq.Schema.DataSources;

[DebuggerDisplay("{ColumnType.FullName} {ColumnName}: {ColumnIndex}")]
public class SchemaColumn : ISchemaColumn
{
    public SchemaColumn(string columnName, int columnIndex, Type columnType)
        : this(columnName, columnIndex, columnType, null, null, null, null, ColumnStability.Stable, true)
    {
    }

    public SchemaColumn(string columnName, int columnIndex, Type columnType, ColumnStability stability)
        : this(columnName, columnIndex, columnType, null, null, null, null, stability, true)
    {
    }

    public SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        IReadOnlyDictionary<string, string>? readModifiers)
        : this(columnName, columnIndex, columnType, null, null, null, readModifiers, ColumnStability.Stable, true)
    {
    }

    public SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        IReadOnlyDictionary<string, string>? readModifiers,
        ColumnStability stability)
        : this(columnName, columnIndex, columnType, null, null, null, readModifiers, stability, true)
    {
    }

    public SchemaColumn(string columnName, int columnIndex, Type columnType, string? intendedTypeName)
        : this(columnName, columnIndex, columnType, null, null, intendedTypeName, null, ColumnStability.Stable, true)
    {
    }

    public SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        string? intendedTypeName,
        ColumnStability stability)
        : this(columnName, columnIndex, columnType, null, null, intendedTypeName, null, stability, true)
    {
    }

    public SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        string? intendedTypeName,
        IReadOnlyDictionary<string, string>? readModifiers)
        : this(columnName, columnIndex, columnType, null, null, intendedTypeName, readModifiers,
            ColumnStability.Stable, true)
    {
    }

    public SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        string? intendedTypeName,
        IReadOnlyDictionary<string, string>? readModifiers,
        ColumnStability stability)
        : this(columnName, columnIndex, columnType, null, null, intendedTypeName, readModifiers, stability, true)
    {
    }

    public SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        string? intendedTypeName,
        ColumnStability stability,
        IReadOnlyDictionary<string, string>? readModifiers)
        : this(columnName, columnIndex, columnType, intendedTypeName, readModifiers, stability)
    {
    }

    public SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        Type sourceReadType,
        EnumTypeDescriptor? enumType)
        : this(columnName, columnIndex, columnType, sourceReadType, enumType, null, null,
            ColumnStability.Stable, false)
    {
    }

    public SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        Type sourceReadType,
        EnumTypeDescriptor? enumType,
        string? intendedTypeName,
        IReadOnlyDictionary<string, string>? readModifiers,
        ColumnStability stability)
        : this(columnName, columnIndex, columnType, sourceReadType, enumType, intendedTypeName, readModifiers,
            stability, false)
    {
    }

    private SchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        Type? sourceReadType,
        EnumTypeDescriptor? enumType,
        string? intendedTypeName,
        IReadOnlyDictionary<string, string>? readModifiers,
        ColumnStability stability,
        bool normalizeNativeEnum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentNullException.ThrowIfNull(columnType);
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        Stability = stability;
        IntendedTypeName = intendedTypeName;
        ReadModifiers = ColumnReadModifiers.Create(readModifiers);

        if (normalizeNativeEnum && EnumTypeDescriptor.TryNormalizeClrEnum(columnType, out var carrierType,
                out var nativeDescriptor))
        {
            ColumnType = carrierType;
            SourceReadType = columnType;
            EnumType = nativeDescriptor;
            ValidateLogicalContract();
        }
        else
        {
            SourceReadType = sourceReadType ?? columnType;
            ColumnType = columnType;
            EnumType = enumType;
            ValidateLogicalContract();
        }
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public Type ColumnType { get; }

    public Type SourceReadType { get; }

    public EnumTypeDescriptor? EnumType { get; }

    public ColumnStability Stability { get; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; }

    /// <summary>
    ///     Gets the intended fully-qualified type name for this column.
    ///     This is used when the actual Type is not available at compile time
    ///     (e.g., for embedded interpreter types that don't exist yet).
    /// </summary>
    public string? IntendedTypeName { get; }

    private void ValidateLogicalContract()
    {
        if (EnumType == null)
        {
            if (ColumnType != SourceReadType)
                throw new ArgumentException("Ordinary columns must use identical carrier and source-read types.");
            return;
        }

        if (IntendedTypeName != null)
            throw new ArgumentException("Enum columns cannot use an intended generated type name.");

        var nullableCarrier = Nullable.GetUnderlyingType(ColumnType);
        var primitiveCarrier = nullableCarrier ?? ColumnType;
        if (primitiveCarrier.IsEnum || primitiveCarrier != EnumScalarTypeFacts.GetCarrierType(EnumType.UnderlyingKind))
        {
            throw new ArgumentException(
                $"Enum column carrier '{ColumnType}' does not match descriptor backing '{EnumType.UnderlyingKind}'.");
        }

        var nullableSource = Nullable.GetUnderlyingType(SourceReadType);
        var primitiveSource = nullableSource ?? SourceReadType;
        if (primitiveSource.IsEnum)
        {
            if (Enum.GetUnderlyingType(primitiveSource) != primitiveCarrier ||
                (nullableCarrier == null) != (nullableSource == null))
            {
                throw new ArgumentException(
                    $"Enum source-read type '{SourceReadType}' does not match carrier '{ColumnType}'.");
            }

            var nativeDescriptor = EnumTypeDescriptor.FromClrEnum(primitiveSource);
            if (!nativeDescriptor.Equals(EnumType))
            {
                throw new ArgumentException(
                    $"Enum descriptor '{EnumType.DisplayName}' does not match native source-read type '{SourceReadType}'.");
            }
        }
        else if (SourceReadType != ColumnType)
        {
            throw new ArgumentException(
                "A logical enum source-read type must be either its primitive carrier or a matching native CLR enum.");
        }
    }
}
