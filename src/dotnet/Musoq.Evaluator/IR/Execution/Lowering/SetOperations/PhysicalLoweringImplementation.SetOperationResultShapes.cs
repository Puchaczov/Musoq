using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static GeneratedRowShape CreateSetOperationResultShape(
        string typeName,
        OutputSchema outputSchema)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        return new GeneratedRowShape(
            typeName,
            outputSchema.Columns.Select(column => new FieldBinding(
                column.Name,
                column.Name,
                column.Index,
                column.Type,
                FieldNullability.Unknown,
                new GeneratedFieldAccess(CreateGeneratedFieldName(column.Name, column.Index, usedFieldNames)),
                sourceReadType: column.SourceReadType,
                enumType: column.EnumType)).ToArray());
    }

    private static bool RequiresSetOperationResultShape(
        OutputSchema outputSchema,
        GeneratedRowShape leftShape)
    {
        for (var index = 0; index < outputSchema.Columns.Length; index++)
        {
            var outputColumn = outputSchema.Columns[index];
            if (outputColumn.EnumType != null &&
                (index >= leftShape.Fields.Count || !Equals(outputColumn.EnumType, leftShape.Fields[index].EnumType)))
                return true;
        }

        return false;
    }
}
