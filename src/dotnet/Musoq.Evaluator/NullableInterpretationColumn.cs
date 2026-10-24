using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator;

internal sealed class NullableInterpretationColumn : SchemaColumn
{
    private NullableInterpretationColumn(ISchemaColumn column)
        : base(column.ColumnName, column.ColumnIndex, column.ColumnType, column.SourceReadType,
            column.EnumType, column.IntendedTypeName, column.ReadModifiers, column.Stability)
    {
    }

    internal static ISchemaColumn Preserve(ISchemaColumn column, bool mayBeNull) =>
        mayBeNull && column.IntendedTypeName != null ? new NullableInterpretationColumn(column) : column;
}
