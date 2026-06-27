using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyRightAlias(
        ExecutionExpression expression,
        string rightAlias)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead when string.Equals(fieldRead.Alias, rightAlias, StringComparison.OrdinalIgnoreCase) =>
                OuterApplyNullSubstitutionResult.Unknown(),
            ExecutionFieldRead => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionLiteral => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionBinary binary => SubstituteOuterApplyBinary(binary, rightAlias),
            ExecutionUnary unary => SubstituteOuterApplyUnary(unary, rightAlias),
            ExecutionStrictCast strictCast => SubstituteOuterApplyStrictCast(strictCast, rightAlias),
            ExecutionMethodCall method => SubstituteOuterApplyMethodCall(method, rightAlias),
            ExecutionIsNullCheck isNull => SubstituteOuterApplyIsNullCheck(isNull, rightAlias),
            ExecutionRowPresence rowPresence when string.Equals(rowPresence.Alias, rightAlias, StringComparison.OrdinalIgnoreCase) =>
                OuterApplyNullSubstitutionResult.Known(new ExecutionLiteral(!rowPresence.IsPresent, typeof(bool))),
            ExecutionRowPresence => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionInCheck inCheck => SubstituteOuterApplyInCheck(inCheck, rightAlias),
            ExecutionPatternMatch patternMatch => SubstituteOuterApplyPatternMatch(patternMatch, rightAlias),
            ExecutionBetween between => SubstituteOuterApplyBetween(between, rightAlias),
            ExecutionCaseWhen caseWhen => SubstituteOuterApplyCaseWhen(caseWhen, rightAlias),
            ExecutionCoalesce coalesce => SubstituteOuterApplyCoalesce(coalesce, rightAlias),
            ExecutionRowStream => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionScalarRowStream => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionStoredTableRows => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionVariableRead => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionCompositeKey compositeKey => SubstituteOuterApplyCompositeKey(compositeKey, rightAlias),
            ExecutionValueTupleKey valueTupleKey => SubstituteOuterApplyValueTupleKey(valueTupleKey, rightAlias),
            ExecutionWindowValueRead => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionAggregateCall aggregateCall => SubstituteOuterApplyAggregateCall(aggregateCall, rightAlias),
            ExecutionGroupKeyRead => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionRawExpression raw when ReferencesAlias(raw.Expression, rightAlias) =>
                OuterApplyNullSubstitutionResult.Unsupported(
                    $"Execution IR outer apply lowering cannot null-substitute right-side filter expression {IrExpressionPrinter.Print(raw.Expression)}."),
            ExecutionRawExpression => OuterApplyNullSubstitutionResult.Known(expression),
            _ => OuterApplyNullSubstitutionResult.Unsupported(
                $"Execution IR outer apply lowering cannot null-substitute expression {expression.GetType().Name}.")
        };
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyStrictCast(
        ExecutionStrictCast strictCast,
        string rightAlias)
    {
        var expression = SubstituteOuterApplyRightAlias(strictCast.Expression, rightAlias);
        return expression.Supported
            ? OuterApplyNullSubstitutionResult.Known(strictCast with { Expression = expression.Expression })
            : expression;
    }

}
