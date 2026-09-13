using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.IR.Execution;
public static partial class ExecutionExpressionConverter
{
    private static ExecutionExpression ConvertCteCollectionInput(
        CteCollectionInput input,
        IReadOnlyDictionary<string, int>? cteTableIndexes)
    {
        if (cteTableIndexes?.TryGetValue(input.CteName, out var tableIndex) != true)
            throw Unsupported(input, $"CTE table '{input.CteName}' requires a registered table index");
        var fields = input.Fields
            .Select(field => new ExecutionCteCollectionField(
                field.Name,
                field.Index,
                ExecutionClrBindingFactory.FromClr(field.Type)))
            .ToArray();
        var receiver = StructuralTypeDescriptor.FromClrType(input.ElementType);
        var constructionPlan = receiver.Kind == StructuralTypeKind.Record
            ? CreateCteConstructionPlan(input.ElementType, fields, receiver)
            : null;
        return new ExecutionCteCollectionInput(
            input.CteName,
            new ExecutionStoredTableRows(tableIndex),
            ExecutionClrBindingFactory.FromClr(input.ReturnType),
            ExecutionClrBindingFactory.FromClr(input.ElementType),
            fields,
            constructionPlan);
    }
    private static ExecutionStructuralConstructionPlan CreateCteConstructionPlan(
        Type elementType,
        IReadOnlyList<ExecutionCteCollectionField> fields,
        StructuralTypeDescriptor receiver)
    {
        var constructor = StructuralInputMetadata.SelectConstructor(elementType);
        var sourceFieldPlans = fields
            .Select(field => CreateCteFieldPlan(field))
            .ToArray();
        var sourceIndexes = new int[receiver.Fields.Count];
        var defaults = new ExecutionExpression?[receiver.Fields.Count];

        for (var parameterIndex = 0; parameterIndex < receiver.Fields.Count; parameterIndex++)
        {
            var receiverField = receiver.Fields[parameterIndex];
            sourceIndexes[parameterIndex] = FindCteFieldIndex(fields, receiverField.Name);
            if (sourceIndexes[parameterIndex] >= 0)
                continue;

            if (!receiverField.HasDefault)
                throw new InvalidOperationException(
                    $"CTE relation is missing required receiving field '{receiverField.Name}'.");

            var targetType = receiverField.Type.ClrType ?? typeof(object);
            defaults[parameterIndex] = new ExecutionLiteral(
                receiverField.Default.Value,
                targetType);
        }

        var shape = new ExecutionStructuralShape(
            StructuralTypeKind.Record,
            scalarType: null,
            sourceFieldPlans,
            elementType: null,
            receiver.IsNullable);
        return new ExecutionStructuralConstructionPlan(
            ExecutionClrBindingFactory.FromClr(elementType),
            ExecutionClrBindingFactory.FromClr(constructor),
            shape,
            sourceIndexes,
            defaults,
            ExecutionStructuralPreparationLifetime.Inline,
            origin: ExecutionStructuralInputOrigin.Cte,
            metricsStrategy: ExecutionStructuralMetricsStrategy.TypedPrepass,
            ownership: ExecutionStructuralOwnershipMode.ConstructFresh);
    }

    private static ExecutionStructuralFieldPlan CreateCteFieldPlan(ExecutionCteCollectionField field)
    {
        var descriptor = StructuralTypeDescriptor.FromClrType(field.Type.ResolveClrType());
        return new ExecutionStructuralFieldPlan(
            field.Name,
            field.Type,
            Required: true,
            HasDefault: false,
            DefaultText: null,
            Shape: ExecutionStructuralShapeFactory.Create(descriptor));
    }

    private static int FindCteFieldIndex(
        IReadOnlyList<ExecutionCteCollectionField> fields,
        string name)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            if (string.Equals(fields[index].Name, name, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return -1;
    }
}
