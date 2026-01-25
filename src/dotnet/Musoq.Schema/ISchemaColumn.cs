using System;

namespace Musoq.Schema;

public interface ISchemaColumn
{
    string ColumnName { get; }
    int ColumnIndex { get; }
    Type ColumnType { get; }

    /// <summary>
    ///     Gets the intended fully-qualified type name for this column.
    ///     This is used when the actual Type is not available at compile time
    ///     (e.g., for embedded interpreter types that don't exist yet).
    ///     When set, code generation should cast to this type instead of ColumnType.
    /// </summary>
    string? IntendedTypeName => null;
}
