using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Planning;

internal static class ParallelExecutionEligibilityRules
{
    public static bool CanUseParallelRows(ExecutionExpression rows)
    {
        return ExecutionRowStreams.IsChunked(rows) || rows is ExecutionStoredTableRows;
    }

    public static ParallelEligibilityCheck CanUseFilterProjectExpression(ExecutionExpression? expression)
    {
        return CanUseExpression(expression, CanUseFilterProjectFieldRead);
    }

    public static ParallelEligibilityCheck CanUseAggregateGroupKeyExpression(ExecutionExpression? expression)
    {
        return CanUseExpression(expression, static _ => ParallelEligibilityCheck.Enabled);
    }

    private static ParallelEligibilityCheck CanUseExpression(
        ExecutionExpression? expression,
        Func<ExecutionFieldRead, ParallelEligibilityCheck> fieldReadEligibility)
    {
        return expression switch
        {
            null => ParallelEligibilityCheck.Enabled,
            ExecutionFieldRead fieldRead => fieldReadEligibility(fieldRead),
            ExecutionLiteral => ParallelEligibilityCheck.Enabled,
            ExecutionBinary binary => Combine(
                CanUseExpression(binary.Left, fieldReadEligibility),
                CanUseExpression(binary.Right, fieldReadEligibility)),
            ExecutionUnary unary => CanUseExpression(unary.Operand, fieldReadEligibility),
            ExecutionArrayAccess arrayAccess => Combine(
                CanUseExpression(arrayAccess.Array, fieldReadEligibility),
                CanUseExpression(arrayAccess.Index, fieldReadEligibility)),
            ExecutionIsNullCheck isNull => CanUseExpression(isNull.Expression, fieldReadEligibility),
            ExecutionRowPresence rowPresence => CanUseExpression(rowPresence.PresenceSource, fieldReadEligibility),
            ExecutionInCheck inCheck => Combine(
                inCheck.Values
                    .Select(value => CanUseExpression(value, fieldReadEligibility))
                    .Prepend(CanUseExpression(inCheck.Expression, fieldReadEligibility))
                    .ToArray()),
            ExecutionPatternMatch patternMatch => Combine(
                CanUseExpression(patternMatch.Expression, fieldReadEligibility),
                CanUseExpression(patternMatch.Pattern, fieldReadEligibility)),
            ExecutionBetween between => Combine(
                CanUseExpression(between.Expression, fieldReadEligibility),
                CanUseExpression(between.Low, fieldReadEligibility),
                CanUseExpression(between.High, fieldReadEligibility)),
            ExecutionCaseWhen caseWhen => Combine(
                caseWhen.Branches
                    .Select(branch => Combine(
                        CanUseExpression(branch.Condition, fieldReadEligibility),
                        CanUseExpression(branch.Result, fieldReadEligibility)))
                    .Append(CanUseExpression(caseWhen.ElseExpression, fieldReadEligibility))
                    .ToArray()),
            ExecutionCoalesce { Expressions.Count: > 0 } coalesce => Combine(
                coalesce.Expressions.Select(part => CanUseExpression(part, fieldReadEligibility)).ToArray()),
            ExecutionCoalesce => ParallelEligibilityCheck.Skipped("Coalesce expression has no operands."),
            ExecutionRowStream stream => ParallelEligibilityCheck.Skipped(stream.Kind == ExecutionRowStreamKind.Chunks
                ? "Expression reads chunked rows directly."
                : "Expression reads a row source directly."),
            ExecutionStoredTable => ParallelEligibilityCheck.Skipped("Expression reads a stored table directly."),
            ExecutionStoredTableRows => ParallelEligibilityCheck.Skipped("Expression reads stored table rows directly."),
            ExecutionVariableRead => ParallelEligibilityCheck.Enabled,
            ExecutionScriptParameterRead => ParallelEligibilityCheck.Enabled,
            ExecutionScriptVariableRead => ParallelEligibilityCheck.Enabled,
            ExecutionRowContextsRead => ParallelEligibilityCheck.Enabled,
            ExecutionNullContextArray => ParallelEligibilityCheck.Enabled,
            ExecutionCompositeKey compositeKey => Combine(
                compositeKey.Parts.Select(part => CanUseExpression(part, fieldReadEligibility)).ToArray()),
            ExecutionValueTupleKey valueTupleKey => Combine(
                valueTupleKey.Parts.Select(part => CanUseExpression(part, fieldReadEligibility)).ToArray()),
            ExecutionWindowValueRead => ParallelEligibilityCheck.Skipped("Expression reads a window value."),
            ExecutionAggregateCall => ParallelEligibilityCheck.Skipped("Expression invokes aggregate state."),
            ExecutionGroupKeyRead => ParallelEligibilityCheck.Skipped("Expression reads aggregate group state."),
            ExecutionAggregateCapturedValueRead => ParallelEligibilityCheck.Skipped("Expression reads aggregate captured state."),
            ExecutionMethodCall methodCall => CanUseMethodCall(methodCall, fieldReadEligibility),
            ExecutionStrictCast strictCast => CanUseExpression(strictCast.Expression, fieldReadEligibility),
            ExecutionMethodTargetReuseCandidate candidate => CanUseMethodCall(candidate.MethodCall, fieldReadEligibility),
            ExecutionRawExpression => ParallelEligibilityCheck.Skipped("Expression could not be lowered to typed Execution IR."),
            _ => ParallelEligibilityCheck.Skipped($"Expression kind {expression.GetType().Name} is not parallel-safe.")
        };
    }

    public static bool ContainsMethodCall(ExecutionExpression? expression)
    {
        return expression switch
        {
            null => false,
            ExecutionMethodCall => true,
            ExecutionMethodTargetReuseCandidate => true,
            ExecutionStrictCast strictCast => ContainsMethodCall(strictCast.Expression),
            ExecutionBinary binary => ContainsMethodCall(binary.Left) || ContainsMethodCall(binary.Right),
            ExecutionUnary unary => ContainsMethodCall(unary.Operand),
            ExecutionArrayAccess arrayAccess => ContainsMethodCall(arrayAccess.Array) ||
                                                ContainsMethodCall(arrayAccess.Index),
            ExecutionIsNullCheck isNull => ContainsMethodCall(isNull.Expression),
            ExecutionRowPresence rowPresence => ContainsMethodCall(rowPresence.PresenceSource),
            ExecutionInCheck inCheck => ContainsMethodCall(inCheck.Expression) ||
                                        inCheck.Values.Any(ContainsMethodCall),
            ExecutionPatternMatch patternMatch => ContainsMethodCall(patternMatch.Expression) ||
                                                  ContainsMethodCall(patternMatch.Pattern),
            ExecutionBetween between => ContainsMethodCall(between.Expression) ||
                                        ContainsMethodCall(between.Low) ||
                                        ContainsMethodCall(between.High),
            ExecutionCaseWhen caseWhen => caseWhen.Branches.Any(static branch =>
                                              ContainsMethodCall(branch.Condition) ||
                                              ContainsMethodCall(branch.Result)) ||
                                          ContainsMethodCall(caseWhen.ElseExpression),
            ExecutionCoalesce coalesce => coalesce.Expressions.Any(ContainsMethodCall),
            ExecutionCompositeKey compositeKey => compositeKey.Parts.Any(ContainsMethodCall),
            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts.Any(ContainsMethodCall),
            ExecutionAggregateCall aggregateCall => aggregateCall.Arguments.Any(ContainsMethodCall),
            _ => false
        };
    }

    public static bool ContainsMethodCall(IrExpression? expression)
    {
        return expression switch
        {
            null => false,
            MethodCall => true,
            StrictCast strictCast => ContainsMethodCall(strictCast.Expression),
            BinaryOp binary => ContainsMethodCall(binary.Left) || ContainsMethodCall(binary.Right),
            UnaryOp unary => ContainsMethodCall(unary.Operand),
            ArrayAccess arrayAccess => ContainsMethodCall(arrayAccess.Array) || ContainsMethodCall(arrayAccess.Index),
            IsNullCheck isNull => ContainsMethodCall(isNull.Expression),
            RowPresence => false,
            InCheck inCheck => ContainsMethodCall(inCheck.Expression) || inCheck.Values.Any(ContainsMethodCall),
            PatternMatch patternMatch => ContainsMethodCall(patternMatch.Expression) || ContainsMethodCall(patternMatch.Pattern),
            Between between => ContainsMethodCall(between.Expression) ||
                               ContainsMethodCall(between.Low) ||
                               ContainsMethodCall(between.High),
            CaseWhen caseWhen => caseWhen.Branches.Any(static branch =>
                                     ContainsMethodCall(branch.Condition) ||
                                     ContainsMethodCall(branch.Result)) ||
                                 ContainsMethodCall(caseWhen.ElseExpression),
            Coalesce coalesce => coalesce.Expressions.Any(ContainsMethodCall),
            _ => false
        };
    }

    private static ParallelEligibilityCheck CanUseMethodCall(
        ExecutionMethodCall methodCall,
        Func<ExecutionFieldRead, ParallelEligibilityCheck> fieldReadEligibility)
    {
        if (methodCall.Method.GetCustomAttribute<NonDeterministicAttribute>() != null)
            return ParallelEligibilityCheck.Skipped($"Expression contains non-deterministic method {methodCall.Method.Name}.");

        if (methodCall.Method.GetParameters()
            .Any(static parameter => parameter.GetCustomAttribute<InjectQueryStatsAttribute>() != null))
        {
            return ParallelEligibilityCheck.Skipped($"Expression calls {methodCall.Method.Name}, which injects query statistics.");
        }

        return Combine(
            methodCall.Arguments
                .Select(argument => CanUseExpression(argument, fieldReadEligibility))
                .Append(CanUseExpression(methodCall.InjectedSource, fieldReadEligibility))
                .ToArray());
    }

    private static ParallelEligibilityCheck CanUseFilterProjectFieldRead(ExecutionFieldRead fieldRead)
    {
        return fieldRead.AccessStrategy is ExpandoDictionaryAccess
            or ReflectedMemberAccess
            or NestedClrPropertyAccess
            or NestedPositionalAccess
            ? ParallelEligibilityCheck.Skipped($"Expression reads field {fieldRead.Alias}.{fieldRead.FieldName} through dynamic or reflected access.")
            : ParallelEligibilityCheck.Enabled;
    }

    private static ParallelEligibilityCheck Combine(params ParallelEligibilityCheck[] checks)
    {
        return checks.FirstOrDefault(static check => !check.IsEligible) ?? ParallelEligibilityCheck.Enabled;
    }
}
