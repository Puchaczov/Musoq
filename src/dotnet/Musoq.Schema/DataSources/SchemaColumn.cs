using System;
using System.Diagnostics;

namespace Musoq.Schema.DataSources;

[DebuggerDisplay("{ColumnType.FullName} {ColumnName}: {ColumnIndex}")]
public class SchemaColumn : ISchemaColumn
{
    public SchemaColumn(string columnName, int columnIndex, Type columnType)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        ColumnType = columnType;
    }

    public SchemaColumn(string columnName, int columnIndex, Type columnType, string? intendedTypeName)
        : this(columnName, columnIndex, columnType)
    {
        IntendedTypeName = intendedTypeName;
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public Type ColumnType { get; }

    /// <summary>
    ///     Gets the intended fully-qualified type name for this column.
    ///     This is used when the actual Type is not available at compile time
    ///     (e.g., for embedded interpreter types that don't exist yet).
    /// </summary>
    public string? IntendedTypeName { get; }
}
