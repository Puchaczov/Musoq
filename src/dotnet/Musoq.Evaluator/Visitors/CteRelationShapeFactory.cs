using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

/// <summary>Builds the structural output shape exposed by a CTE relation.</summary>
internal static class CteRelationShapeFactory
{
    public static StructuralTypeDescriptor CreateRecordShape(IReadOnlyList<ISchemaColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fields = new StructuralFieldDescriptor[columns.Count];
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            if (!names.Add(column.ColumnName))
                throw new InvalidOperationException(
                    $"CTE output contains duplicate column '{column.ColumnName}'.");

            fields[index] = new StructuralFieldDescriptor(
                column.ColumnName,
                StructuralTypeDescriptor.FromClrType(column.ColumnType),
                required: true);
        }

        return StructuralTypeDescriptor.Record(typeof(StructuralValue), fields);
    }
}
