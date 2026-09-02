using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal static partial class ExecutionExpressionCseFacts
{
    internal sealed record HoistOccurrence(
        string Signature,
        ExecutionExpression Expression,
        int Depth,
        bool IsSafeOrigin);

    public static IReadOnlyList<ExecutionExpression> GetWindowHelperExpressions(ExecutionNode node)
    {
        return ExecutionNodeFacts.TryGetWindowComputation(node, out _)
            ? ExecutionNodeFacts.GetLocalExpressions(node).ToArray()
            : [];
    }

    public static IReadOnlyList<ExecutionExpression> GetWindowHelperIndependentExpressions(ExecutionNode node)
    {
        var expressions = GetWindowHelperExpressions(node);
        return expressions.All(IsWindowHelperIndependentExpression)
            ? expressions
            : [];
    }

    /// <summary>
    /// Returns stable per-row window inputs that may be shared by compatible
    /// registrations. The caller still owns registration and frame semantics;
    /// this method only describes scalar inputs, never sort/comparer state.
    /// </summary>
    public static IReadOnlyList<ExecutionExpression> GetWindowSharedExpressions(ExecutionNode node)
    {
        if (!ExecutionNodeFacts.TryGetWindowComputation(node, out _))
            return [];

        return ExecutionNodeFacts.GetLocalExpressions(node)
            .Where(static expression =>
                ExpressionStabilityAnalyzer.IsStable(expression) &&
                IsCseResultTypeStable(expression.ReturnType.ResolveClrType()))
            .ToArray();
    }

    /// <summary>
    /// Returns stable per-input aggregate keys, arguments, filters, and
    /// accumulator inputs. Aggregate state and distinctness remain owned by
    /// the aggregate kernel; only scalar inputs are candidates for sharing.
    /// </summary>
    public static IReadOnlyList<ExecutionExpression> GetAggregateSharedExpressions(ExecutionNode node)
    {
        return node switch
        {
            ExecutionGetOrAddSingleKeyAggregateGroup group => StableScalarExpressions([group.Key]),
            ExecutionGetOrAddValueTupleAggregateGroup group => StableScalarExpressions(group.Keys),
            ExecutionAggregateSet aggregate => StableScalarExpressions(
                AggregateKernelArgumentSelector.SelectValueArgumentsAfterGroup(aggregate.Arguments)
                    .Concat(OptionalExpression(aggregate.FilterPredicate))
                    .Concat(OptionalExpression(aggregate.AccumulatorInput))),
            ExecutionAggregateCapturedValueSet captured => StableScalarExpressions([captured.Value]),
            _ => []
        };
    }

    private static IReadOnlyList<ExecutionExpression> StableScalarExpressions(
        IEnumerable<ExecutionExpression> expressions)
    {
        return expressions
            .Where(static expression =>
                ExpressionStabilityAnalyzer.IsStable(expression) &&
                IsCseResultTypeStable(expression.ReturnType.ResolveClrType()))
            .ToArray();
    }

    private static IEnumerable<ExecutionExpression> OptionalExpression(ExecutionExpression? expression)
    {
        if (expression != null)
            yield return expression;
    }

    public static bool IsWindowHelperIndependentExpression(ExecutionExpression expression)
    {
        return !ExecutionIrAnalysis.FlattenExpressions(expression).Any(static current => current is
            ExecutionFieldRead or
            ExecutionMemberRead or
            ExecutionVariableRead or
            ExecutionRowContextsRead or
            ExecutionWindowValueRead or
            ExecutionAggregateCall or
            ExecutionGroupKeyRead or
            ExecutionAggregateCapturedValueRead or
            ExecutionRowStream or
            ExecutionScalarRowStream or
            ExecutionStoredTable or
            ExecutionStoredTableRows);
    }

    public static IEnumerable<HoistOccurrence> CollectHoistableOccurrences(
        ExecutionBlock block,
        bool inPassThroughUnsafeContext = false)
    {
        foreach (var node in block.Nodes)
        {
            foreach (var occurrence in CollectHoistableOccurrences(node, inPassThroughUnsafeContext))
                yield return occurrence;
        }
    }

    public static IEnumerable<HoistOccurrence> CollectHoistableOccurrences(
        ExecutionNode node,
        bool inPassThroughUnsafeContext = false)
    {
        switch (node)
        {
            case ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd:
                return CollectHoistableOccurrences(getOrAdd.Key, inPassThroughUnsafeContext);
            case ExecutionGetOrAddValueTupleAggregateGroup getOrAdd:
                return getOrAdd.Keys.SelectMany(key => CollectHoistableOccurrences(key, inPassThroughUnsafeContext));
            case ExecutionAggregateSet aggregateSet:
                return aggregateSet.Arguments
                    .SelectMany(argument => CollectHoistableOccurrences(argument, inPassThroughUnsafeContext))
                    .Concat(aggregateSet.AccumulatorInput == null
                        ? []
                        : CollectHoistableOccurrences(aggregateSet.AccumulatorInput, inPassThroughUnsafeContext));
            case ExecutionAggregateCapturedValueSet capturedValueSet:
                return CollectHoistableOccurrences(capturedValueSet.Value, inPassThroughUnsafeContext);
            case ExecutionLet let:
                return CollectHoistableOccurrences(let.Value, inPassThroughUnsafeContext);
            case ExecutionIf branch:
                return CollectHoistableOccurrences(branch.Condition, inPassThroughUnsafeContext)
                    .Concat(CollectHoistableOccurrences(branch.Body, true));
            case ExecutionAppendRow appendRow:
                return appendRow.Values
                    .SelectMany(value => CollectHoistableOccurrences(value.Value, inPassThroughUnsafeContext))
                    .Concat(appendRow.Contexts.SelectMany(context => CollectHoistableOccurrences(context, inPassThroughUnsafeContext)))
                    .Concat(ExecutionNodeFacts.GetContextLayoutExpressions(appendRow.ContextLayout)
                        .SelectMany(context => CollectHoistableOccurrences(context, inPassThroughUnsafeContext)));
            case ExecutionAppendRecord appendRecord:
                return appendRecord.Values.SelectMany(value => CollectHoistableOccurrences(value.Value, inPassThroughUnsafeContext));
            default:
                return [];
        }
    }

    public static IEnumerable<HoistOccurrence> CollectStableScalarReuseOccurrences(
        ExecutionBlock block,
        bool inPassThroughUnsafeContext = false)
    {
        foreach (var node in block.Nodes)
        {
            foreach (var expression in GetStableScalarReuseExpressions(node))
            {
                foreach (var occurrence in CollectHoistableOccurrences(expression, inPassThroughUnsafeContext))
                    yield return occurrence;
            }
        }
    }

    internal static IEnumerable<ExecutionExpression> GetStableScalarReuseExpressions(ExecutionNode node)
    {
        return node switch
        {
            ExecutionAppendRow appendRow => appendRow.Values
                .Select(static value => value.Value)
                .Concat(appendRow.Contexts)
                .Concat(ExecutionNodeFacts.GetContextLayoutExpressions(appendRow.ContextLayout)),
            ExecutionAppendRecord appendRecord => appendRecord.Values.Select(static value => value.Value),
            ExecutionCreateGeneratedRow createRow => createRow.Values
                .Select(static value => value.Value)
                .Concat(createRow.Contexts)
                .Concat(ExecutionNodeFacts.GetContextLayoutExpressions(createRow.ContextLayout)),
            ExecutionCreateHashPayload payload => payload.Values.Select(static value => value.Value),
            ExecutionHashAdd hashAdd => [hashAdd.Key],
            ExecutionHashProbe hashProbe => [hashProbe.Key],
            ExecutionKeySetAdd keySetAdd => [keySetAdd.Key],
            ExecutionKeySetProbe keySetProbe => [keySetProbe.Key],
            ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd => [getOrAdd.Key],
            ExecutionGetOrAddValueTupleAggregateGroup getOrAdd => getOrAdd.Keys,
            ExecutionAggregateSet aggregateSet => aggregateSet.Arguments
                .Concat(aggregateSet.FilterPredicate == null ? [] : [aggregateSet.FilterPredicate])
                .Concat(aggregateSet.AccumulatorInput == null ? [] : [aggregateSet.AccumulatorInput]),
            ExecutionAggregateCapturedValueSet capturedValueSet => [capturedValueSet.Value],
            ExecutionComputeRankingWindow or
                ExecutionComputeOffsetWindow or
            ExecutionComputePluginWindow or
                ExecutionWindowAggregateKernel => ExecutionNodeFacts.GetLocalExpressions(node),
            ExecutionCreateRangeIndex or
                ExecutionRangeProbe => ExecutionNodeFacts.GetLocalExpressions(node),
            ExecutionRecursiveCteAppend append => append.AppendRow.Values
                .Select(static value => value.Value)
                .Concat(append.AppendRow.Contexts)
                .Concat(ExecutionNodeFacts.GetContextLayoutExpressions(append.AppendRow.ContextLayout)),
            _ => []
        };
    }

    public static IEnumerable<HoistOccurrence> CollectHoistableOccurrences(
        ExecutionExpression expression,
        bool inPassThroughUnsafeContext = false)
    {
        if (IsWorthHoistingExpression(expression) && IsDeterministicExpression(expression))
        {
            yield return new HoistOccurrence(
                ExecutionExpressionFingerprint.ForHoist(expression),
                expression,
                GetExpressionDepth(expression),
                !inPassThroughUnsafeContext || expression is ExecutionFieldRead);
        }

        switch (expression)
        {
            case ExecutionBinary binary:
                var rightIsShortCircuitConditional = binary.Kind is BinaryOpKind.And or BinaryOpKind.Or;
                foreach (var occurrence in CollectHoistableOccurrences(binary.Left, inPassThroughUnsafeContext))
                    yield return occurrence;
                foreach (var occurrence in CollectHoistableOccurrences(
                             binary.Right,
                             inPassThroughUnsafeContext || rightIsShortCircuitConditional))
                    yield return occurrence;
                break;
            case ExecutionArrayAccess arrayAccess:
                foreach (var occurrence in CollectHoistableOccurrences(arrayAccess.Index, inPassThroughUnsafeContext))
                    yield return occurrence;
                break;
            case ExecutionCaseWhen caseWhen:
                foreach (var branch in caseWhen.Branches)
                {
                    foreach (var occurrence in CollectHoistableOccurrences(branch.Condition, true))
                        yield return occurrence;
                    foreach (var occurrence in CollectHoistableOccurrences(branch.Result, true))
                        yield return occurrence;
                }

                if (caseWhen.ElseExpression != null)
                {
                    foreach (var occurrence in CollectHoistableOccurrences(caseWhen.ElseExpression, true))
                        yield return occurrence;
                }

                break;
            case ExecutionCoalesce coalesce:
                for (var index = 0; index < coalesce.Expressions.Count; index++)
                {
                    foreach (var occurrence in CollectHoistableOccurrences(
                                 coalesce.Expressions[index],
                                 inPassThroughUnsafeContext || index > 0))
                        yield return occurrence;
                }

                break;
            default:
                foreach (var child in ExecutionIrAnalysis.GetChildExpressions(expression))
                {
                    foreach (var occurrence in CollectHoistableOccurrences(child, inPassThroughUnsafeContext))
                        yield return occurrence;
                }

                break;
        }
    }

    public static bool IsWorthHoistingExpression(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead => !string.IsNullOrWhiteSpace(fieldRead.Alias),
            ExecutionMemberRead memberRead => IsDeterministicExpression(memberRead.Receiver),
            ExecutionMethodCall methodCall => IsSafeWholeMethodCallCseCandidate(methodCall, hasExplicitTargetMetadata: false),
            ExecutionMethodTargetReuseCandidate candidate => IsSafeWholeMethodCallCseCandidate(candidate.MethodCall, hasExplicitTargetMetadata: true),
            ExecutionStrictCast strictCast => IsSafeWholeStrictCastCseCandidate(strictCast),
            ExecutionBinary binary => binary.Kind is not (BinaryOpKind.And or BinaryOpKind.Or),
            ExecutionUnary => true,
            ExecutionArrayAccess => true,
            ExecutionIsNullCheck => true,
            ExecutionPatternMatch => true,
            ExecutionBetween => true,
            _ => false
        };
    }

    private static bool IsSafeWholeStrictCastCseCandidate(ExecutionStrictCast strictCast)
    {
        return !StrictCastLibraryConversionFacts.IsPassThrough(
                   strictCast.Expression.ReturnType.ResolveClrType(),
                   strictCast.ReturnType.ResolveClrType()) &&
               IsCseResultTypeStable(strictCast.ReturnType.ResolveClrType()) &&
               IsDeterministicExpression(strictCast.Expression);
    }

    public static bool IsSafeWholeMethodCallCseCandidate(
        ExecutionMethodCall methodCall,
        bool hasExplicitTargetMetadata)
    {
        if (methodCall.ReturnType.ResolveClrType() == typeof(void) ||
            methodCall.InjectedSource != null ||
            !IsCseResultTypeStable(methodCall.ReturnType.ResolveClrType()) ||
            !IsDeterministicMethod(methodCall.Method) ||
            !methodCall.Arguments.All(IsDeterministicExpression))
        {
            return false;
        }

        return methodCall.Target != null ||
               methodCall.Cache != null ||
               methodCall.Method.IsStatic ||
               hasExplicitTargetMetadata ||
               ExecutionMethodTargetReuse.CanRenderWithoutTarget(methodCall);
    }

    public static bool IsCseResultTypeStable(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.IsValueType || underlyingType == typeof(string);
    }

    public static bool IsDeterministicExpression(ExecutionExpression expression)
    {
        return ExpressionStabilityAnalyzer.IsStable(expression);
    }

    public static bool IsDeterministicMethod(MethodInfo method) =>
        ExpressionStabilityAnalyzer.IsStableMethod(method);

    public static bool IsDeterministicMethod(ExecutionCallableRef method) =>
        ExpressionStabilityAnalyzer.IsStableMethod(method);

    public static int GetExpressionDepth(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionMemberRead memberRead => 1 + GetExpressionDepth(memberRead.Receiver),
            ExecutionBinary binary => 1 + Math.Max(GetExpressionDepth(binary.Left), GetExpressionDepth(binary.Right)),
            ExecutionUnary unary => 1 + GetExpressionDepth(unary.Operand),
            ExecutionStrictCast strictCast => 1 + GetExpressionDepth(strictCast.Expression),
            ExecutionMethodTargetReuseCandidate candidate => GetExpressionDepth(candidate.MethodCall),
            ExecutionMethodCall methodCall => 1 + Math.Max(
                methodCall.Arguments.Count == 0 ? 0 : methodCall.Arguments.Max(GetExpressionDepth),
                methodCall.InjectedSource == null ? 0 : GetExpressionDepth(methodCall.InjectedSource)),
            ExecutionArrayAccess arrayAccess => 1 + Math.Max(
                GetExpressionDepth(arrayAccess.Array),
                GetExpressionDepth(arrayAccess.Index)),
            ExecutionIsNullCheck isNull => 1 + GetExpressionDepth(isNull.Expression),
            ExecutionInCheck inCheck => 1 + Math.Max(
                GetExpressionDepth(inCheck.Expression),
                inCheck.Values.Count == 0 ? 0 : inCheck.Values.Max(GetExpressionDepth)),
            ExecutionCollectionInCheck collectionInCheck => 1 + GetExpressionDepth(collectionInCheck.Expression),
            ExecutionPatternMatch patternMatch => 1 + Math.Max(
                GetExpressionDepth(patternMatch.Expression),
                GetExpressionDepth(patternMatch.Pattern)),
            ExecutionBetween between => 1 + Math.Max(
                GetExpressionDepth(between.Expression),
                Math.Max(GetExpressionDepth(between.Low), GetExpressionDepth(between.High))),
            ExecutionCaseWhen caseWhen => 1 + Math.Max(
                caseWhen.Branches.Count == 0
                    ? 0
                    : caseWhen.Branches.Max(static branch => Math.Max(
                        GetExpressionDepth(branch.Condition),
                        GetExpressionDepth(branch.Result))),
                caseWhen.ElseExpression == null ? 0 : GetExpressionDepth(caseWhen.ElseExpression)),
            ExecutionCoalesce coalesce => 1 + (coalesce.Expressions.Count == 0
                ? 0
                : coalesce.Expressions.Max(GetExpressionDepth)),
            ExecutionCompositeKey compositeKey => 1 + (compositeKey.Parts.Count == 0
                ? 0
                : compositeKey.Parts.Max(GetExpressionDepth)),
            ExecutionValueTupleKey valueTupleKey => 1 + (valueTupleKey.Parts.Count == 0
                ? 0
                : valueTupleKey.Parts.Max(GetExpressionDepth)),
            ExecutionAggregateCall aggregateCall => 1 + (aggregateCall.Arguments.Count == 0
                ? 0
                : aggregateCall.Arguments.Max(GetExpressionDepth)),
            _ => 1
        };
    }
}
