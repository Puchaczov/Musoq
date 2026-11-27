using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static GeneratedRowShape CreateSetOperationResultShape(string typeName, OutputSchema outputSchema)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        return new GeneratedRowShape(
            typeName,
            outputSchema
                .Columns.Select(column => new FieldBinding(
                    column.Name,
                    column.Name,
                    column.Index,
                    column.Type,
                    FieldNullability.Unknown,
                    new GeneratedFieldAccess(CreateGeneratedFieldName(column.Name, column.Index, usedFieldNames)),
                    sourceReadType: column.SourceReadType,
                    enumType: column.EnumType
                )
                {
                    GeneratedTypeName = InterpretationTypeNames.Executable(column.IntendedTypeName),
                })
                .ToArray()
        );
    }

    private static GeneratedRowShape BindStreamingSetFields(
        GeneratedRowShape target,
        IReadOnlyList<ExecutionRowValue> left,
        IReadOnlyList<ExecutionRowValue> right
    )
    {
        FieldBinding[]? fields = null;
        for (var i = 0; i < target.Fields.Count; i++)
        {
            var field = target.Fields[i];
            var shape = SelectSetInterpretationShape(
                left[i].Value.InterpretationShape,
                right[i].Value.InterpretationShape
            );
            var typeName =
                field.GeneratedTypeName
                ?? ExecutionGeneratedTypeName.For(left[i].Value)
                ?? ExecutionGeneratedTypeName.For(right[i].Value);
            if (ReferenceEquals(field.InterpretationShape, shape) && field.GeneratedTypeName == typeName)
                continue;
            fields ??= [.. target.Fields];
            fields[i] = field with { InterpretationShape = shape, GeneratedTypeName = typeName };
        }
        return fields == null ? target : target with { Fields = fields };
    }

    private static ExecutionInterpretationShape? SelectSetInterpretationShape(
        ExecutionInterpretationShape? left,
        ExecutionInterpretationShape? right
    ) => left is { Members.Count: > 0 } or { Alternatives.Count: > 0 } or { Element: not null } ? left : right ?? left;

    private static GeneratedRowShape BindMaterializedSetFields(
        GeneratedRowShape target,
        GeneratedRowShape left,
        GeneratedRowShape right
    )
    {
        FieldBinding[]? fields = null;
        var requiresTypedCarrier = false;
        for (var i = 0; i < target.Fields.Count; i++)
        {
            var field = target.Fields[i];
            var shape = SelectSetInterpretationShape(
                left.Fields[i].InterpretationShape,
                right.Fields[i].InterpretationShape
            );
            if (shape == null)
                continue;

            // Closed alternatives are read from the generated field holding their selected value.
            requiresTypedCarrier |= shape.Alternatives.Count > 0;
            if (ReferenceEquals(field.InterpretationShape, shape))
                continue;
            fields ??= [.. target.Fields];
            fields[i] = field with { InterpretationShape = shape };
        }

        return target with
        {
            Fields = fields ?? target.Fields,
            Contexts = [],
            SupportsGeneratedFieldAccess = requiresTypedCarrier,
        };
    }

    private static bool RequiresSetOperationResultShape(OutputSchema outputSchema, GeneratedRowShape leftShape)
    {
        for (var index = 0; index < outputSchema.Columns.Length; index++)
        {
            var outputColumn = outputSchema.Columns[index];
            if (
                index >= leftShape.Fields.Count
                || outputColumn.Type != leftShape.Fields[index].Type.ResolveClrType()
                || outputColumn.EnumType != null && !Equals(outputColumn.EnumType, leftShape.Fields[index].EnumType)
            )
                return true;
        }

        return false;
    }
}
