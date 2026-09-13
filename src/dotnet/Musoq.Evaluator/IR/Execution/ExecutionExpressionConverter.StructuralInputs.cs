using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.IR.Execution;
public static partial class ExecutionExpressionConverter
{
    private static ExecutionExpression ConvertStructuralRecord(
        StructuralRecordLiteral record,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        var fields = record.Fields
            .Select(field => new ExecutionStructuralField(
                field.Name,
                Convert(field.Value, sourceShapes, cteTableIndexes, methodTargets)))
            .ToArray();
        var constructionPlan = record.TargetType == null
            ? null
            : CreateConstructionPlan(record, fields);
        return new ExecutionStructuralRecord(
            ExecutionClrBindingFactory.FromClr(record.ReturnType),
            fields,
            constructionPlan);
    }

    private static ExecutionExpression ConvertStructuralArray(
        StructuralArrayLiteral array,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        return new ExecutionStructuralArray(
            ExecutionClrBindingFactory.FromClr(array.ReturnType),
            ExecutionClrBindingFactory.FromClr(array.ElementType),
            array.Elements.Select(element => Convert(element, sourceShapes, cteTableIndexes, methodTargets)));
    }

    private static ExecutionExpression ConvertStructuralConversion(
        StructuralConversion conversion,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        return new ExecutionStructuralConversion(
            Convert(conversion.Value, sourceShapes, cteTableIndexes, methodTargets),
            ExecutionClrBindingFactory.FromClr(conversion.TargetType));
    }

    private static ExecutionStructuralConstructionPlan CreateConstructionPlan(
        StructuralRecordLiteral record,
        IReadOnlyList<ExecutionStructuralField> fields)
    {
        var targetType = record.TargetType ?? throw new InvalidOperationException("A structural record target type is required.");
        var receiver = StructuralTypeDescriptor.FromClrType(targetType);
        if (receiver.Kind != StructuralTypeKind.Record)
            throw new InvalidOperationException($"Structural receiver '{targetType.FullName}' is not a record.");

        var constructor = StructuralInputMetadata.SelectConstructor(targetType);
        var sourceIndexes = new int[receiver.Fields.Count];
        var defaults = new ExecutionExpression?[receiver.Fields.Count];
        var plans = new List<ExecutionStructuralFieldPlan>(fields.Count);
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            var receiverField = receiver.Fields.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, field.Name, StringComparison.OrdinalIgnoreCase));
            if (receiverField == null)
                throw new InvalidOperationException($"Structural input contains unexpected field '{field.Name}'.");

            plans.Add(ExecutionStructuralShapeFactory.CreateFieldPlan(receiverField));
        }

        for (var parameterIndex = 0; parameterIndex < receiver.Fields.Count; parameterIndex++)
        {
            var parameter = receiver.Fields[parameterIndex];
            sourceIndexes[parameterIndex] = FindFieldIndex(fields, parameter.Name);
            if (sourceIndexes[parameterIndex] >= 0)
                continue;

            if (!parameter.HasDefault)
                throw new InvalidOperationException($"Structural input is missing required field '{parameter.Name}'.");

            defaults[parameterIndex] = new ExecutionLiteral(
                parameter.Default.Value,
                ExecutionClrBindingFactory.FromClr(parameter.Type.ClrType ?? typeof(object)));
        }

        var shape = new ExecutionStructuralShape(
            StructuralTypeKind.Record,
            null,
            plans,
            null,
            receiver.IsNullable);
        return new ExecutionStructuralConstructionPlan(
            ExecutionClrBindingFactory.FromClr(targetType),
            ExecutionClrBindingFactory.FromClr(constructor),
            shape,
            sourceIndexes,
            defaults,
            ExecutionStructuralPreparationLifetime.Inline);
    }

    private static int FindFieldIndex(IReadOnlyList<ExecutionStructuralField> fields, string name)
    {
        for (var index = 0; index < fields.Count; index++)
            if (string.Equals(fields[index].Name, name, StringComparison.OrdinalIgnoreCase))
                return index;

        return -1;
    }
}
