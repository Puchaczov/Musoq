using System.Linq;
using Musoq.Evaluator.IR.Analysis;

namespace Musoq.Evaluator.IR.Execution;

internal static class ParallelExecutionEligibilityRules
{
    public static bool CanUseParallelRows(ExecutionExpression rows)
    {
        return ExecutionRowStreams.IsChunked(rows) || rows is ExecutionStoredTableRows;
    }

    public static ParallelExecutionEligibilityCheck CanUseFilterProjectExpression(ExecutionExpression? expression)
    {
        return CanUseExpression(expression, CanUseFilterProjectFieldRead);
    }

    private static ParallelExecutionEligibilityCheck CanUseExpression(
        ExecutionExpression? expression,
        Func<ExecutionFieldRead, ParallelExecutionEligibilityCheck> fieldReadEligibility)
    {
        return expression switch
        {
            null => ParallelExecutionEligibilityCheck.Enabled,
            ExecutionFieldRead { Stability: Musoq.Schema.ColumnStability.Volatile } fieldRead =>
                ParallelExecutionEligibilityCheck.Skipped($"Expression reads volatile field {fieldRead.Alias}.{fieldRead.FieldName}."),
            ExecutionFieldRead fieldRead => fieldReadEligibility(fieldRead),
            ExecutionMemberRead memberRead when !ExpressionStabilityAnalyzer.IsStable(memberRead) =>
                ParallelExecutionEligibilityCheck.Skipped($"Expression reads unstable member {memberRead.MemberName}."),
            ExecutionMemberRead memberRead => CanUseExpression(memberRead.Receiver, fieldReadEligibility),
            ExecutionLiteral => ParallelExecutionEligibilityCheck.Enabled,
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
            ExecutionCoalesce => ParallelExecutionEligibilityCheck.Skipped("Coalesce expression has no operands."),
            ExecutionRowStream stream => ParallelExecutionEligibilityCheck.Skipped(stream.Kind == ExecutionRowStreamKind.Chunks
                ? "Expression reads chunked rows directly."
                : "Expression reads a row source directly."),
            ExecutionStoredTable => ParallelExecutionEligibilityCheck.Skipped("Expression reads a stored table directly."),
            ExecutionStoredTableRows => ParallelExecutionEligibilityCheck.Skipped("Expression reads stored table rows directly."),
            ExecutionVariableRead => ParallelExecutionEligibilityCheck.Enabled,
            ExecutionScriptParameterRead => ParallelExecutionEligibilityCheck.Enabled,
            ExecutionScriptVariableRead => ParallelExecutionEligibilityCheck.Enabled,
            ExecutionRowContextsRead => ParallelExecutionEligibilityCheck.Enabled,
            ExecutionNullContextArray => ParallelExecutionEligibilityCheck.Enabled,
            ExecutionCompositeKey compositeKey => Combine(
                compositeKey.Parts.Select(part => CanUseExpression(part, fieldReadEligibility)).ToArray()),
            ExecutionValueTupleKey valueTupleKey => Combine(
                valueTupleKey.Parts.Select(part => CanUseExpression(part, fieldReadEligibility)).ToArray()),
            ExecutionWindowValueRead => ParallelExecutionEligibilityCheck.Skipped("Expression reads a window value."),
            ExecutionAggregateCall => ParallelExecutionEligibilityCheck.Skipped("Expression invokes aggregate state."),
            ExecutionGroupKeyRead => ParallelExecutionEligibilityCheck.Skipped("Expression reads aggregate group state."),
            ExecutionAggregateCapturedValueRead => ParallelExecutionEligibilityCheck.Skipped("Expression reads aggregate captured state."),
            ExecutionMethodCall methodCall => CanUseMethodCall(methodCall, fieldReadEligibility),
            ExecutionStrictCast strictCast => CanUseExpression(strictCast.Expression, fieldReadEligibility),
            ExecutionMethodTargetReuseCandidate candidate => CanUseMethodCall(candidate.MethodCall, fieldReadEligibility),
            ExecutionAggregateResultRef => ParallelExecutionEligibilityCheck.Skipped("Aggregate result reference must be resolved before parallel execution."),
            ExecutionWindowResultRef => ParallelExecutionEligibilityCheck.Skipped("Window result reference must be resolved before parallel execution."),
            _ => ParallelExecutionEligibilityCheck.Skipped($"Expression kind {expression.GetType().Name} is not parallel-safe.")
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

    private static ParallelExecutionEligibilityCheck CanUseMethodCall(
        ExecutionMethodCall methodCall,
        Func<ExecutionFieldRead, ParallelExecutionEligibilityCheck> fieldReadEligibility)
    {
        if (ExpressionStabilityAnalyzer.TryGetMethodInstabilityReason(
                methodCall.Method.ResolveClrMethod(),
                "Expression",
                out var instabilityReason))
        {
            return ParallelExecutionEligibilityCheck.Skipped(instabilityReason);
        }

        return Combine(
            methodCall.Arguments
                .Select(argument => CanUseExpression(argument, fieldReadEligibility))
                .Append(CanUseExpression(methodCall.InjectedSource, fieldReadEligibility))
                .ToArray());
    }

    private static ParallelExecutionEligibilityCheck CanUseFilterProjectFieldRead(ExecutionFieldRead fieldRead)
    {
        return fieldRead.AccessStrategy is ExpandoDictionaryAccess
            or ReflectedMemberAccess
            or NestedClrPropertyAccess
            or NestedPositionalAccess
            ? ParallelExecutionEligibilityCheck.Skipped($"Expression reads field {fieldRead.Alias}.{fieldRead.FieldName} through dynamic or reflected access.")
            : ParallelExecutionEligibilityCheck.Enabled;
    }

    private static ParallelExecutionEligibilityCheck Combine(params ParallelExecutionEligibilityCheck[] checks)
    {
        return checks.FirstOrDefault(static check => !check.IsEligible) ?? ParallelExecutionEligibilityCheck.Enabled;
    }
}

internal sealed record ParallelExecutionEligibilityCheck(bool IsEligible, string Reason)
{
    public static ParallelExecutionEligibilityCheck Enabled { get; } = new(true, string.Empty);

    public static ParallelExecutionEligibilityCheck Skipped(string reason)
    {
        return new ParallelExecutionEligibilityCheck(false, reason);
    }
}
