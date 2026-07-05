using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static GeneratedRowShape CreateGeneratedShape(string typeName, ProjectedField[] fields)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        return new GeneratedRowShape(
            typeName,
            fields.Select(field => new FieldBinding(
                field.OutputName,
                field.OutputName,
                field.OutputIndex,
                field.Expression.ReturnType,
                FieldNullability.Unknown,
                new GeneratedFieldAccess(CreateGeneratedFieldName(field.OutputName, field.OutputIndex, usedFieldNames)))).ToArray());
    }

    private static GeneratedRowShape CreateGeneratedShape(
        string typeName,
        ProjectedField[] fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        return new GeneratedRowShape(
            typeName,
            fields.Select(field => CreateProjectedFieldBinding(field, sourceLookup, usedFieldNames)).ToArray(),
            CreateContextBindings(sourceLookup),
            SupportsGeneratedFieldAccess(sourceLookup));
    }

    private static bool SupportsGeneratedFieldAccess(IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        return !sourceLookup.Values.Any(static shape => shape is ExpandoAdapterShape);
    }

    private static FieldBinding CreateProjectedFieldBinding(
        ProjectedField field,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        HashSet<string> usedFieldNames)
    {
        var expression = ConvertProjectedExpression(field, sourceLookup);
        var columnType = ResolveProjectedFieldType(expression);
        var storageType = ResolveProjectedFieldStorageType(expression, columnType);

        return new FieldBinding(
            field.OutputName,
            field.OutputName,
            field.OutputIndex,
            storageType,
            FieldNullability.Unknown,
            new GeneratedFieldAccess(CreateGeneratedFieldName(field.OutputName, field.OutputIndex, usedFieldNames)),
            storageType == columnType ? null : columnType);
    }

    private static List<FieldBinding> CreateContextBindings(
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var contexts = new List<FieldBinding>();

        foreach (var sourceShape in sourceLookup.Values)
            contexts.AddRange(CreateContextBindings(sourceShape, contexts.Count));

        return contexts;
    }

    private static IEnumerable<FieldBinding> CreateContextBindings(
        RowShape sourceShape,
        int startIndex)
    {
        if (sourceShape is TableRowShape tableRow)
        {
            if (tableRow.Contexts.Count == 0)
            {
                var tableAlias = RowShapeLookup.ResolveSourceAlias(tableRow);
                yield return new FieldBinding(
                    tableAlias,
                    tableAlias,
                    startIndex,
                    typeof(object),
                    FieldNullability.Unknown,
                    new ContextAccess(startIndex));

                yield break;
            }

            for (var index = 0; index < tableRow.Contexts.Count; index++)
            {
                var context = tableRow.Contexts[index];
                yield return context with { AccessStrategy = new ContextAccess(startIndex + index) };
            }

            yield break;
        }

        var alias = RowShapeLookup.ResolveSourceAlias(sourceShape);
        yield return new FieldBinding(
            alias,
            alias,
            startIndex,
            RowShapeLookup.ResolveSourceRuntimeType(sourceShape),
            FieldNullability.Unknown,
            new ContextAccess(startIndex));
    }

    private static Type ResolveProjectedFieldType(ExecutionExpression expression)
    {
        if (!ShouldLiftNullableProjection(expression))
            return expression.ReturnType;

        return typeof(Nullable<>).MakeGenericType(expression.ReturnType);
    }

    private static Type ResolveProjectedFieldStorageType(
        ExecutionExpression expression,
        Type columnType)
    {
        return expression is ExecutionBinary binary && IsNullableTemporalSubtraction(binary)
            ? typeof(TimeSpan?)
            : columnType;
    }

    private static bool ShouldLiftNullableProjection(ExecutionExpression expression)
    {
        return expression is ExecutionBinary binary &&
               CanLiftNullableTransitionBinary(binary.Kind) &&
               binary.ReturnType.IsValueType &&
               Nullable.GetUnderlyingType(binary.ReturnType) == null &&
               !IsNullableTemporalSubtraction(binary) &&
               (ContainsNullablePositionalFieldRead(binary) ||
                ContainsLiftedNullableArithmeticInput(binary));
    }

    private static bool CanLiftNullableTransitionBinary(BinaryOpKind kind)
    {
        return kind is BinaryOpKind.Add
            or BinaryOpKind.Subtract
            or BinaryOpKind.Multiply
            or BinaryOpKind.Divide
            or BinaryOpKind.Modulo
            or BinaryOpKind.BitwiseAnd
            or BinaryOpKind.BitwiseOr
            or BinaryOpKind.BitwiseXor
            or BinaryOpKind.LeftShift
            or BinaryOpKind.RightShift;
    }

    private static bool IsNullableTemporalSubtraction(ExecutionBinary binary)
    {
        return binary.Kind == BinaryOpKind.Subtract &&
               binary.ReturnType == typeof(TimeSpan) &&
               IsNullableTemporal(binary.Left.ReturnType) &&
               IsNullableTemporal(binary.Right.ReturnType);
    }

    private static bool IsNullableTemporal(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset);
    }

    private static bool ContainsLiftedNullableArithmeticInput(ExecutionBinary binary)
    {
        return ContainsLiftedNullableArithmeticInput(binary.Left) ||
               ContainsLiftedNullableArithmeticInput(binary.Right);
    }

    private static bool ContainsLiftedNullableArithmeticInput(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead => Nullable.GetUnderlyingType(fieldRead.ReturnType) != null,
            ExecutionBinary binary when CanLiftNullableTransitionBinary(binary.Kind) =>
                ContainsLiftedNullableArithmeticInput(binary),
            ExecutionUnary unary => ContainsLiftedNullableArithmeticInput(unary.Operand),
            ExecutionStrictCast strictCast => ContainsLiftedNullableArithmeticInput(strictCast.Expression),
            _ => false
        };
    }

    private static bool ContainsNullablePositionalFieldRead(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead => fieldRead.AccessStrategy is PositionalAccess &&
                                            Nullable.GetUnderlyingType(fieldRead.ReturnType) != null,
            ExecutionBinary binary => ContainsNullablePositionalFieldRead(binary.Left) ||
                                      ContainsNullablePositionalFieldRead(binary.Right),
            ExecutionUnary unary => ContainsNullablePositionalFieldRead(unary.Operand),
            ExecutionMethodCall method => method.Arguments.Any(ContainsNullablePositionalFieldRead) ||
                                          (method.InjectedSource != null &&
                                           ContainsNullablePositionalFieldRead(method.InjectedSource)),
            ExecutionStrictCast strictCast => ContainsNullablePositionalFieldRead(strictCast.Expression),
            ExecutionIsNullCheck isNull => ContainsNullablePositionalFieldRead(isNull.Expression),
            ExecutionInCheck inCheck => ContainsNullablePositionalFieldRead(inCheck.Expression) ||
                                        inCheck.Values.Any(ContainsNullablePositionalFieldRead),
            ExecutionPatternMatch patternMatch => ContainsNullablePositionalFieldRead(patternMatch.Expression) ||
                                                  ContainsNullablePositionalFieldRead(patternMatch.Pattern),
            ExecutionBetween between => ContainsNullablePositionalFieldRead(between.Expression) ||
                                        ContainsNullablePositionalFieldRead(between.Low) ||
                                        ContainsNullablePositionalFieldRead(between.High),
            ExecutionCaseWhen caseWhen => caseWhen.Branches.Any(branch =>
                                                 ContainsNullablePositionalFieldRead(branch.Condition) ||
                                                 ContainsNullablePositionalFieldRead(branch.Result)) ||
                                             (caseWhen.ElseExpression != null &&
                                              ContainsNullablePositionalFieldRead(caseWhen.ElseExpression)),
            ExecutionCoalesce coalesce => coalesce.Expressions.Any(ContainsNullablePositionalFieldRead),
            ExecutionCompositeKey compositeKey => compositeKey.Parts.Any(ContainsNullablePositionalFieldRead),
            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts.Any(ContainsNullablePositionalFieldRead),
            ExecutionAggregateCall aggregateCall => aggregateCall.Arguments.Any(ContainsNullablePositionalFieldRead),
            _ => false
        };
    }
}
