using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnUsage = Musoq.Evaluator.IR.Optimization.Physical.PhysicalColumnUsageFacts;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static class PhysicalProjectionBoundaryInputPruner
{
    public static (PhysicalNode Node, int PrunedFields) Prune(
        PhysicalNode input,
        HashSet<string> requiredNames)
    {
        if (!TryFindPrunableInnerProject(
                input,
                requiredNames,
                out var inner,
                out var requiredThroughChain,
                out var rebuildInput) ||
            inner.IsDistinct ||
            !TrySelectRequiredInnerFields(requiredThroughChain, inner.Fields, out var prunedFields))
        {
            return (input, 0);
        }

        var prunedInput = rebuildInput(new PhysicalProjectNode(prunedFields, inner.Input)
        {
            IsDistinct = inner.IsDistinct
        });

        return (prunedInput, inner.Fields.Length - prunedFields.Length);
    }

    public static bool TryFindPrunableInnerProject(
        PhysicalNode input,
        HashSet<string> requiredNames,
        out PhysicalProjectNode inner,
        out HashSet<string> requiredThroughChain,
        out Func<PhysicalNode, PhysicalNode> rebuildInput)
    {
        switch (input)
        {
            case PhysicalProjectNode project:
                inner = project;
                requiredThroughChain = requiredNames;
                rebuildInput = static rewritten => rewritten;
                return true;
            case PhysicalFilterNode filter:
                ColumnUsage.AddExpressionColumns(requiredNames, filter.Predicate);
                return TryFindTransparentChild(
                    filter.Input,
                    requiredNames,
                    rewritten => new PhysicalFilterNode(filter.Predicate, rewritten),
                    out inner,
                    out requiredThroughChain,
                    out rebuildInput);
            case PhysicalSortNode sort:
                ColumnUsage.AddOrderColumns(requiredNames, sort.Keys);
                return TryFindTransparentChild(
                    sort.Input,
                    requiredNames,
                    rewritten => new PhysicalSortNode(sort.Keys, rewritten),
                    out inner,
                    out requiredThroughChain,
                    out rebuildInput);
            case PhysicalTopNNode topN:
                ColumnUsage.AddOrderColumns(requiredNames, topN.Keys);
                return TryFindTransparentChild(
                    topN.Input,
                    requiredNames,
                    rewritten => new PhysicalTopNNode(topN.N, topN.Keys, rewritten),
                    out inner,
                    out requiredThroughChain,
                    out rebuildInput);
            case PhysicalTopOffsetNode topOffset:
                ColumnUsage.AddOrderColumns(requiredNames, topOffset.Keys);
                return TryFindTransparentChild(
                    topOffset.Input,
                    requiredNames,
                    rewritten => new PhysicalTopOffsetNode(topOffset.Skip, topOffset.Take, topOffset.Keys, rewritten),
                    out inner,
                    out requiredThroughChain,
                    out rebuildInput);
            case PhysicalSkipNode skip:
                return TryFindTransparentChild(
                    skip.Input,
                    requiredNames,
                    rewritten => new PhysicalSkipNode(skip.Count, rewritten),
                    out inner,
                    out requiredThroughChain,
                    out rebuildInput);
            case PhysicalTakeNode take:
                return TryFindTransparentChild(
                    take.Input,
                    requiredNames,
                    rewritten => new PhysicalTakeNode(take.Count, rewritten),
                    out inner,
                    out requiredThroughChain,
                    out rebuildInput);
            default:
                inner = null!;
                requiredThroughChain = requiredNames;
                rebuildInput = static rewritten => rewritten;
                return false;
        }
    }

    public static bool TrySelectRequiredInnerFields(
        IReadOnlySet<string> requiredNames,
        IReadOnlyList<ProjectedField> innerFields,
        out ProjectedField[] prunedFields)
    {
        prunedFields = [];

        if (innerFields.Count == 0 ||
            ColumnUsage.HasAmbiguousOutputNames(innerFields))
        {
            return false;
        }

        if (requiredNames.Count == 0)
            return false;

        if (requiredNames.Any(name => !innerFields.Any(field => ColumnUsage.NameEquals(field.OutputName, name))))
            return false;

        var selected = innerFields
            .Where(field => requiredNames.Contains(field.OutputName))
            .Select((field, index) => field with { OutputIndex = index })
            .ToArray();

        if (selected.Length == innerFields.Count)
            return false;

        prunedFields = selected;
        return true;
    }

    public static PhysicalNode PruneSetOperationArm(
        PhysicalNode arm,
        IReadOnlyList<int> retainedIndexes,
        out bool pruned)
    {
        pruned = false;

        if (arm is not PhysicalProjectNode { IsDistinct: false } project ||
            retainedIndexes.Count >= project.Fields.Length ||
            retainedIndexes.Any(index => index < 0 || index >= project.Fields.Length))
        {
            return arm;
        }

        var fields = retainedIndexes
            .Select((fieldIndex, outputIndex) => project.Fields[fieldIndex] with { OutputIndex = outputIndex })
            .ToArray();

        if (fields.Length == project.Fields.Length)
            return arm;

        pruned = true;
        return new PhysicalProjectNode(fields, project.Input);
    }

    public static Dictionary<int, int> CreateSetOperationIndexMap(IReadOnlyList<int> retainedIndexes)
    {
        var indexMap = new Dictionary<int, int>();

        for (var index = 0; index < retainedIndexes.Count; index++)
            indexMap[retainedIndexes[index]] = index;

        return indexMap;
    }

    private static bool TryFindTransparentChild(
        PhysicalNode child,
        HashSet<string> requiredNames,
        Func<PhysicalNode, PhysicalNode> rebuildCurrent,
        out PhysicalProjectNode inner,
        out HashSet<string> requiredThroughChain,
        out Func<PhysicalNode, PhysicalNode> rebuildInput)
    {
        if (!TryFindPrunableInnerProject(
                child,
                requiredNames,
                out inner,
                out requiredThroughChain,
                out var rebuildChild))
        {
            rebuildInput = static rewritten => rewritten;
            return false;
        }

        rebuildInput = rewritten => rebuildCurrent(rebuildChild(rewritten));
        return true;
    }
}

