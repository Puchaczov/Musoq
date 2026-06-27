using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{

    private static string? GetUnsupportedExpressionReason(ExecutionExpression expression)
    {
        if (CanRenderExpression(expression))
            return null;

        if (expression is ExecutionMethodCall methodCall)
            return GetUnsupportedMethodCallReason(methodCall);

        if (expression is ExecutionMethodTargetReuseCandidate candidate)
            return GetUnsupportedMethodCallReason(candidate.MethodCall);

        return $"Execution IR C# backend cannot render expression {expression.GetType().Name}.";
    }

    private static string? GetUnsupportedExpressionReason(IEnumerable<ExecutionExpression> expressions)
    {
        foreach (var expression in expressions)
        {
            var reason = GetUnsupportedExpressionReason(expression);
            if (reason != null)
                return reason;
        }

        return null;
    }

    private static bool CanRenderExpression(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead => CanRenderFieldRead(fieldRead),
            ExecutionScriptParameterRead parameterRead => !string.IsNullOrWhiteSpace(parameterRead.Name) &&
                                                          CanReferenceType(parameterRead.ReturnType),
            ExecutionScriptVariableRead variableRead => !string.IsNullOrWhiteSpace(variableRead.Name) &&
                                                        CanReferenceType(variableRead.ReturnType),
            ExecutionLiteral literal => CanRenderLiteral(literal.Value),
            ExecutionBinary binary => CanRenderBinary(binary),
            ExecutionUnary unary => CanRenderUnary(unary),
            ExecutionMethodCall methodCall => CanRenderMethodCall(methodCall),
            ExecutionStrictCast strictCast => CanRenderExpression(strictCast.Expression) &&
                                              CanReferenceType(strictCast.ReturnType),
            ExecutionMethodTargetReuseCandidate candidate => CanRenderMethodCall(candidate.MethodCall),
            ExecutionArrayAccess arrayAccess => CanRenderExpression(arrayAccess.Array) &&
                                                CanRenderExpression(arrayAccess.Index) &&
                                                CanReferenceType(arrayAccess.ElementType),
            ExecutionIndexedHashRowCreate indexedRowCreate => CanReferenceType(indexedRowCreate.ReturnType),
            ExecutionIndexedHashRowRowRead rowRead => CanReferenceType(rowRead.ReturnType),
            ExecutionIndexedHashRowIndexRead => true,
            ExecutionIsNullCheck isNull => CanRenderExpression(isNull.Expression),
            ExecutionRowPresence rowPresence => CanRenderExpression(rowPresence.PresenceSource),
            ExecutionInCheck inCheck => CanRenderExpression(inCheck.Expression) &&
                                        inCheck.Values.All(CanRenderExpression) &&
                                        (inCheck.ConstantSet == null || CanRenderConstantInSet(inCheck.ConstantSet)),
            ExecutionCollectionInCheck collectionInCheck => CanRenderCollectionInCheck(collectionInCheck),
            ExecutionPatternMatch patternMatch => CanRenderPatternMatch(patternMatch),
            ExecutionBetween between => CanRenderExpression(between.Expression) &&
                                        CanRenderExpression(between.Low) &&
                                        CanRenderExpression(between.High),
            ExecutionCaseWhen caseWhen => caseWhen.Branches.All(branch =>
                                              CanRenderExpression(branch.Condition) &&
                                              CanRenderExpression(branch.Result)) &&
                                          (caseWhen.ElseExpression == null ||
                                           CanRenderExpression(caseWhen.ElseExpression)),
            ExecutionCoalesce coalesce => coalesce.Expressions.Count > 0 &&
                                          coalesce.Expressions.All(CanRenderExpression),
            ExecutionRowStream => true,
            ExecutionScalarRowStream => true,
            ExecutionStoredTable => true,
            ExecutionStoredTableRows => true,
            ExecutionVariableRead => true,
            ExecutionRowContextsRead contextsRead => typeof(Row).IsAssignableFrom(contextsRead.Row.Type),
            ExecutionNullContextArray nullContextArray => nullContextArray.Count >= 0,
            ExecutionContextArray contextArray => contextArray.Segments.All(static segment => segment.Count >= 0) &&
                                                  contextArray.Segments.Select(static segment => segment.Value).All(CanRenderExpression),
            ExecutionCompositeKey compositeKey => compositeKey.Parts.All(CanRenderExpression),
            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts.Count is >= 2 and <= 7 &&
                                                    IsValueTupleType(valueTupleKey.ReturnType, valueTupleKey.Parts.Count) &&
                                                    valueTupleKey.Parts.All(CanRenderExpression),
            ExecutionWindowValueRead => true,
            ExecutionAggregateCall aggregateCall => CanReferenceType(aggregateCall.Accumulator.InputType) &&
                                                    CanReferenceType(aggregateCall.Accumulator.ResultType) &&
                                                    CanReferenceType(aggregateCall.Accumulator.AccumulatorType) &&
                                                    CanReferenceType(aggregateCall.Accumulator.Kernel.KernelType),
            ExecutionGroupKeyRead groupKeyRead => groupKeyRead.Key != null,
            ExecutionAggregateCapturedValueRead capturedValueRead => CanReferenceType(capturedValueRead.ReturnType) &&
                                                                     CanReferenceType(capturedValueRead.CapturedField.Type),
            _ => false
        };
    }

    private static bool CanRenderFieldRead(ExecutionFieldRead fieldRead)
    {
        return fieldRead.AccessStrategy is not (
                   PositionalAccess or
                   ContextAccess or
                   GeneratedRowContextAccess or
                   GeneratedRowTypeAccess or
                   GeneratedRowNestedAccess or
                   ReflectedMemberAccess or
                   NestedClrPropertyAccess or
                   NestedPositionalAccess) ||
               !string.IsNullOrWhiteSpace(fieldRead.Alias);
    }

    private static bool CanRenderLiteral(object? value)
    {
        return value is null or string or bool or char or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private static bool CanRenderBinary(ExecutionBinary binary)
    {
        return CanRenderBinaryKind(binary.Kind) &&
               CanRenderExpression(binary.Left) &&
               CanRenderExpression(binary.Right);
    }

    private static bool CanRenderUnary(ExecutionUnary unary)
    {
        return CanRenderUnaryKind(unary.Kind) &&
               CanRenderExpression(unary.Operand);
    }

    private static bool ContainsMethodCall(ExecutionExpression expression)
    {
        return ExecutionIrAnalysis.FlattenExpressions(expression)
            .Any(static current => current is ExecutionMethodCall);
    }

    private static bool CanRenderPatternMatch(ExecutionPatternMatch patternMatch)
    {
        return patternMatch.Kind is PatternKind.Like or PatternKind.RLike &&
               CanRenderExpression(patternMatch.Expression) &&
               CanRenderExpression(patternMatch.Pattern);
    }

    private static bool CanRenderConstantInSet(ExecutionConstantInSet constantSet)
    {
        return CanReferenceType(constantSet.ElementType) &&
               constantSet.Values.All(CanRenderLiteral);
    }

    private static bool CanRenderBinaryKind(BinaryOpKind kind)
    {
        return kind is BinaryOpKind.Add
            or BinaryOpKind.StringConcatenate
            or BinaryOpKind.Subtract
            or BinaryOpKind.Multiply
            or BinaryOpKind.Divide
            or BinaryOpKind.Modulo
            or BinaryOpKind.And
            or BinaryOpKind.Or
            or BinaryOpKind.Equal
            or BinaryOpKind.NotEqual or BinaryOpKind.IsDistinctFrom or BinaryOpKind.IsNotDistinctFrom
            or BinaryOpKind.GreaterThan
            or BinaryOpKind.LessThan
            or BinaryOpKind.GreaterOrEqual
            or BinaryOpKind.LessOrEqual
            or BinaryOpKind.BitwiseAnd
            or BinaryOpKind.BitwiseOr
            or BinaryOpKind.BitwiseXor
            or BinaryOpKind.LeftShift
            or BinaryOpKind.RightShift;
    }

    private static bool RequiresNullableTemporalSubtraction(ExecutionBinary binary)
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

    private static bool CanRenderUnaryKind(UnaryOpKind kind)
    {
        return kind is UnaryOpKind.Not or UnaryOpKind.Negate;
    }
}
