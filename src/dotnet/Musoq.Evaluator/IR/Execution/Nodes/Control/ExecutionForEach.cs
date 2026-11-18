using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionForEach(ExecutionVariable Item, ExecutionExpression Source, ExecutionBlock Body)
    : ExecutionSourceLoop(Item, Source, Body)
{
    internal (ExecutionEnsureAggregateGroup Group, ExecutionAggregateSet Set)? GlobalCountAll =>
        Body.Nodes
            is [
                ExecutionEnsureAggregateGroup
                {
                    GroupShape:
                    { Keys.Count: 0, CapturedFields.Count: 0, OwnerFields.Count: 0, Accumulators: [var accumulator] }
                } ensure,
                ExecutionAggregateSet
                {
                    FilterPredicate: null,
                    AccumulatorInput: null,
                    Accumulator: { ParentDepth: 0, OwnerFieldName: null }
                } set,
            ]
        && ensure.CurrentGroup == set.Group
        && accumulator == set.Accumulator
        && AggregateInlinePolicy.Resolve(accumulator.Kernel) == AggregateInlineKind.CountAll
        && AggregateKernelArgumentSelector.SelectValueArgumentsAfterGroup(set.Arguments).Length == 0
            ? (ensure, set)
            : null;

    // Only a leading unconditional SKIP can consume input without evaluating the row.
    internal ExecutionVariable? LeadingSkipCounter
    {
        get
        {
            if (
                Body.Nodes
                is not [ExecutionIf { Body.Nodes: [ExecutionAssign decrement, ExecutionContinue] } branch, ..]
            )
                return null;
            var counter = decrement.Variable;
            return
                counter.Type.StableId == "primitive:int32"
                && MatchesCounterOperation(branch.Condition, BinaryOpKind.GreaterThan, counter, 0)
                && MatchesCounterOperation(decrement.Value, BinaryOpKind.Subtract, counter, 1)
                ? counter
                : null;
        }
    }

    private static bool MatchesCounterOperation(
        ExecutionExpression expression,
        BinaryOpKind kind,
        ExecutionVariable counter,
        int constant
    )
    {
        return expression
                is ExecutionBinary
                {
                    Left: ExecutionVariableRead read,
                    Right: ExecutionLiteral
                    {
                        Value: { Kind: ExecutionConstantKind.SignedInteger, SignedInteger: var value }
                    }
                } binary
            && binary.Kind == kind
            && read.Variable == counter
            && value == constant;
    }
}
