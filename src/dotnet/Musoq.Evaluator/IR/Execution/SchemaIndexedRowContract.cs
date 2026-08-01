using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Describes a source row whose schema, rather than CLR members, names the
/// values stored at positional indexes.
/// </summary>
internal static class SchemaIndexedRowContract
{
    public static bool IsSupported(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return entityType == typeof(object[]);
    }

    public static bool TryValidateColumn(ISchemaColumn column, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(column);
        reason = null;

        if (column.ColumnIndex < 0)
        {
            reason = $"schema-indexed column '{column.ColumnName}' has an invalid negative index";
            return false;
        }

        if (!ExecutionSourceCodeGenerationPolicy.CanReferenceType(column.ColumnType))
        {
            reason = $"schema-indexed column '{column.ColumnName}' has the non-referenceable type '{column.ColumnType.FullName ?? column.ColumnType.Name}'";
            return false;
        }

        return true;
    }
}
