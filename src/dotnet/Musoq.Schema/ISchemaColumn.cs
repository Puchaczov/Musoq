namespace Musoq.Schema;

public interface ISchemaColumn
{
    string ColumnName { get; }
    int ColumnIndex { get; }

    /// <summary>
    ///     Gets the primitive carrier type used by query rows and expressions.
    /// </summary>
    Type ColumnType { get; }

    /// <summary>
    ///     Gets the exact type presented by the source boundary before normalization.
    /// </summary>
    Type SourceReadType { get; }

    /// <summary>
    ///     Gets the optional portable logical enum identity for this column.
    /// </summary>
    EnumTypeDescriptor? EnumType { get; }

    /// <summary>
    ///     Gets whether the column can be evaluated once for the lifetime of its bound row.
    ///     Existing providers default to <see cref="ColumnStability.Stable"/>.
    /// </summary>
    ColumnStability Stability => ColumnStability.Stable;

    System.Collections.Generic.IReadOnlyDictionary<string, string> ReadModifiers => ColumnReadModifiers.Empty;

    /// <summary>
    ///     Gets the intended fully-qualified type name for this column.
    ///     This is used when the actual Type is not available at compile time
    ///     (e.g., for embedded interpreter types that don't exist yet).
    ///     When set, code generation should cast to this type instead of ColumnType.
    /// </summary>
    string? IntendedTypeName => null;
}
