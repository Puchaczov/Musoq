using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Optimization;

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

    public static bool IsWindowHelperIndependentExpression(ExecutionExpression expression)
    {
        return !ExecutionIrAnalysis.FlattenExpressions(expression).Any(static current => current is
            ExecutionFieldRead or
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
        return !StrictCastLibraryConversionFacts.IsPassThrough(strictCast.Expression.ReturnType, strictCast.ReturnType) &&
               IsCseResultTypeStable(strictCast.ReturnType) &&
               IsDeterministicExpression(strictCast.Expression);
    }

    public static bool IsSafeWholeMethodCallCseCandidate(
        ExecutionMethodCall methodCall,
        bool hasExplicitTargetMetadata)
    {
        if (methodCall.ReturnType == typeof(void) ||
            methodCall.InjectedSource != null ||
            !IsCseResultTypeStable(methodCall.ReturnType) ||
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
        return expression switch
        {
            ExecutionRawExpression => false,
            ExecutionMethodTargetReuseCandidate candidate => IsDeterministicExpression(candidate.MethodCall),
            ExecutionStrictCast strictCast => IsDeterministicExpression(strictCast.Expression),
            ExecutionMethodCall methodCall => IsDeterministicMethod(methodCall.Method) &&
                                              methodCall.Arguments.All(IsDeterministicExpression) &&
                                              (methodCall.InjectedSource == null ||
                                               IsDeterministicExpression(methodCall.InjectedSource)),
            ExecutionBinary binary => IsDeterministicExpression(binary.Left) &&
                                      IsDeterministicExpression(binary.Right),
            ExecutionUnary unary => IsDeterministicExpression(unary.Operand),
            ExecutionArrayAccess arrayAccess => IsDeterministicExpression(arrayAccess.Array) &&
                                                IsDeterministicExpression(arrayAccess.Index),
            ExecutionIsNullCheck isNull => IsDeterministicExpression(isNull.Expression),
            ExecutionInCheck inCheck => IsDeterministicExpression(inCheck.Expression) &&
                                        inCheck.Values.All(IsDeterministicExpression),
            ExecutionCollectionInCheck collectionInCheck => IsDeterministicExpression(collectionInCheck.Expression),
            ExecutionPatternMatch patternMatch => IsDeterministicExpression(patternMatch.Expression) &&
                                                  IsDeterministicExpression(patternMatch.Pattern),
            ExecutionBetween between => IsDeterministicExpression(between.Expression) &&
                                        IsDeterministicExpression(between.Low) &&
                                        IsDeterministicExpression(between.High),
            ExecutionCaseWhen caseWhen => caseWhen.Branches.All(branch =>
                                            IsDeterministicExpression(branch.Condition) &&
                                            IsDeterministicExpression(branch.Result)) &&
                                        (caseWhen.ElseExpression == null ||
                                         IsDeterministicExpression(caseWhen.ElseExpression)),
            ExecutionCoalesce coalesce => coalesce.Expressions.All(IsDeterministicExpression),
            ExecutionCompositeKey compositeKey => compositeKey.Parts.All(IsDeterministicExpression),
            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts.All(IsDeterministicExpression),
            ExecutionAggregateCall aggregateCall => IsDeterministicMethod(aggregateCall.Method) &&
                                                   aggregateCall.Arguments.All(IsDeterministicExpression),
            _ => true
        };
    }

    public static bool IsDeterministicMethod(MethodInfo method) =>
        method.GetCustomAttribute<NonDeterministicAttribute>() == null &&
        !method.GetParameters().Any(static parameter => parameter.GetCustomAttribute<InjectQueryStatsAttribute>() != null);

    public static int GetExpressionDepth(ExecutionExpression expression)
    {
        return expression switch
        {
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
