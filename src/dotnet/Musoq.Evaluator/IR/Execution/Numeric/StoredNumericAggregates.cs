using System.Collections.Generic;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record StoredNumericReduction(
    ExecutionAggregateSet Set,
    AggregateInlineKind Kind,
    NumericPredicate? Filter,
    NumericColumn? Input
);

internal sealed record StoredNumericAggregates(
    ExecutionEnsureAggregateGroup? GlobalGroup,
    IReadOnlyList<StoredNumericReduction> Reductions
) : StoredNumericConsumer
{
    internal static StoredNumericAggregates? FromLoop(ExecutionBlock body, StoredNumericBindings bindings)
    {
        var reductions = new List<StoredNumericReduction>();
        ExecutionVariable? group = null;
        ExecutionEnsureAggregateGroup? global = null;
        bool canReduceInBlocks = true;
        foreach (var node in body.Nodes)
        {
            switch (node)
            {
                case ExecutionLet let when bindings.Bind(let):
                    continue;
                case ExecutionEnsureAggregateGroup ensure when group == null:
                    global = ensure;
                    group = ensure.CurrentGroup;
                    continue;
                case ExecutionGetOrAddSingleKeyAggregateGroup keyed
                    when group == null
                        && bindings.Operand(keyed.Key) is NumericColumn { Type.StableId: "primitive:int32" }:
                    group = keyed.Group;
                    continue;
                case ExecutionAggregateSet set
                    when set.Group.Name == group?.Name && set.Accumulator is { ParentDepth: 0, OwnerFieldName: null }:
                    var filter = set.FilterPredicate == null ? null : bindings.Predicate(set.FilterPredicate);
                    if (set.FilterPredicate != null && filter == null)
                        return null;
                    var kind = AggregateInlinePolicy.Resolve(set.Accumulator.Kernel);
                    var arguments = AggregateKernelArgumentSelector.SelectValueArgumentsAfterGroup(set.Arguments);
                    NumericColumn? input = null;
                    if (kind == AggregateInlineKind.CountAll)
                    {
                        if (arguments.Length != 0 || set.AccumulatorInput != null)
                            return null;
                    }
                    else
                    {
                        var expression = arguments.Length == 1 ? arguments[0] : set.AccumulatorInput;
                        if (
                            kind is not (AggregateInlineKind.Min or AggregateInlineKind.Max)
                            || arguments.Length > 1
                            || expression == null
                            || bindings.Operand(expression) is not NumericColumn column
                            || column.Type.StableId
                                is not (
                                    "primitive:int32"
                                    or "primitive:int64"
                                    or "nullable<primitive:int32>"
                                    or "nullable<primitive:int64>"
                                )
                        )
                            return null;
                        input = column;
                        canReduceInBlocks &=
                            filter == null && column.Type.Descriptor.Kind != ExecutionPortableTypeKind.Nullable;
                    }
                    reductions.Add(new(set, kind, filter, input));
                    break;
                default:
                    return null;
            }
        }
        if (group == null || reductions.Count is < 1 or > 8)
            return null;
        // Keyed updates and nullable/filtered extrema retain the existing per-row kernel.
        return new(canReduceInBlocks ? global : null, ExecutionIrCollections.Freeze(reductions));
    }
}
