using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Rewriting;

internal static class PhysicalPlanRewriter
{
    public static PhysicalNode RewriteChildren(PhysicalNode node, Func<PhysicalNode, PhysicalNode> rewriteNode)
    {
        return node switch
        {
            PhysicalFilterNode filter => RewriteInput(
                filter.Input,
                rewriteNode,
                input => new PhysicalFilterNode(filter.Predicate, input),
                filter),
            PhysicalProjectNode project => RewriteInput(
                project.Input,
                rewriteNode,
                input => new PhysicalProjectNode(project.Fields, input) { IsDistinct = project.IsDistinct },
                project),
            PhysicalComputeNode compute => RewriteInput(
                compute.Input,
                rewriteNode,
                input => new PhysicalComputeNode(input, compute.ComputedFields),
                compute),
            PhysicalSortNode sort => RewriteInput(
                sort.Input,
                rewriteNode,
                input => new PhysicalSortNode(sort.Keys, input),
                sort),
            PhysicalSkipNode skip => RewriteInput(
                skip.Input,
                rewriteNode,
                input => new PhysicalSkipNode(skip.Count, input),
                skip),
            PhysicalTakeNode take => RewriteInput(
                take.Input,
                rewriteNode,
                input => new PhysicalTakeNode(take.Count, input),
                take),
            PhysicalTopNNode topN => RewriteInput(
                topN.Input,
                rewriteNode,
                input => new PhysicalTopNNode(topN.N, topN.Keys, input),
                topN),
            PhysicalTopOffsetNode topOffset => RewriteInput(
                topOffset.Input,
                rewriteNode,
                input => new PhysicalTopOffsetNode(topOffset.Skip, topOffset.Take, topOffset.Keys, input),
                topOffset),
            PhysicalMaterializeNode materialize => RewriteInput(
                materialize.Input,
                rewriteNode,
                input => new PhysicalMaterializeNode(input),
                materialize),
            PhysicalWindowNode window => RewriteInput(
                window.Input,
                rewriteNode,
                input => new PhysicalWindowNode(window.Registrations, input),
                window),
            PhysicalUnpivotNode unpivot => RewriteInput(
                unpivot.Source,
                rewriteNode,
                input => new PhysicalUnpivotNode(
                    unpivot.Alias,
                    unpivot.NameColumn,
                    unpivot.ValueColumn,
                    unpivot.Entries,
                    unpivot.KeepFields,
                    input,
                    unpivot.OutputSchema),
                unpivot),
            PhysicalAggregateCandidateNode aggregate => RewriteInput(
                aggregate.Input,
                rewriteNode,
                input => new PhysicalAggregateCandidateNode(
                    aggregate.GroupKeys,
                    aggregate.GroupKeyNames,
                    aggregate.GroupKeyTypes,
                    aggregate.Bindings,
                    input),
                aggregate),
            PhysicalAggregateOnlyNode aggregate => RewriteInput(
                aggregate.Input,
                rewriteNode,
                input => new PhysicalAggregateOnlyNode(aggregate.Bindings, input),
                aggregate),
            PhysicalSingleKeyAggregateNode aggregate => RewriteInput(
                aggregate.Input,
                rewriteNode,
                input => new PhysicalSingleKeyAggregateNode(
                    aggregate.GroupKey,
                    aggregate.GroupKeyName,
                    aggregate.GroupKeyType,
                    aggregate.Bindings,
                    input),
                aggregate),
            PhysicalValueTupleAggregateNode aggregate => RewriteInput(
                aggregate.Input,
                rewriteNode,
                input => new PhysicalValueTupleAggregateNode(
                    aggregate.GroupKeys,
                    aggregate.GroupKeyNames,
                    aggregate.GroupKeyTypes,
                    aggregate.Bindings,
                    input),
                aggregate),
            PhysicalHavingFilterNode having => RewriteInput(
                having.Input,
                rewriteNode,
                input => new PhysicalHavingFilterNode(having.Predicate, input),
                having),
            PhysicalQualifyFilterNode qualify => RewriteInput(
                qualify.Input,
                rewriteNode,
                input => new PhysicalQualifyFilterNode(qualify.Predicate, input),
                qualify),
            PhysicalJoinCandidateNode join => RewritePair(
                join.Left,
                join.Right,
                rewriteNode,
                (left, right) => new PhysicalJoinCandidateNode(
                    join.Kind,
                    join.OnPredicate,
                    left,
                    right,
                    join.LeftMovedPredicates,
                    join.RightMovedPredicates,
                    join.TieBreak,
                    join.WithOrdinality),
                join),
            PhysicalHashJoinNode join => RewritePair(
                join.Left,
                join.Right,
                rewriteNode,
                (left, right) => new PhysicalHashJoinNode(join.Kind, join.BuildKeys, join.ProbeKeys, join.Residual, left, right),
                join),
            PhysicalNestedLoopJoinNode join => RewritePair(
                join.Left,
                join.Right,
                rewriteNode,
                (left, right) => new PhysicalNestedLoopJoinNode(join.Kind, join.OnPredicate, left, right, join.TieBreak, join.WithOrdinality),
                join),
            PhysicalSortMergeJoinNode join => RewritePair(
                join.Left,
                join.Right,
                rewriteNode,
                (left, right) => new PhysicalSortMergeJoinNode(
                    join.Kind,
                    join.LeftKey,
                    join.RightKey,
                    join.ComparisonKind,
                    join.Residual,
                    left,
                    right)
                {
                    LeftPartitionKeys = join.LeftPartitionKeys,
                    RightPartitionKeys = join.RightPartitionKeys
                },
                join),
            PhysicalNestedLoopApplyNode apply => RewritePair(
                apply.Left,
                apply.Right,
                rewriteNode,
                (left, right) => new PhysicalNestedLoopApplyNode(apply.Kind, left, right, apply.WithOrdinality)
                {
                    ApplyPredicateMovementPlans = apply.ApplyPredicateMovementPlans
                },
                apply),
            PhysicalSetOperationNode setOperation => RewritePair(
                setOperation.Left,
                setOperation.Right,
                rewriteNode,
                (left, right) => new PhysicalSetOperationNode(
                    setOperation.Kind,
                    left,
                    right,
                    setOperation.FieldIndexes,
                    setOperation.FieldTypes),
                setOperation),
            PhysicalRecursiveCteNode recursiveCte => RewriteRecursiveCte(recursiveCte, rewriteNode),
            PhysicalCteNode cte => RewriteCte(cte, rewriteNode),
            PhysicalMultiStatementNode multiStatement => RewriteMultiStatement(multiStatement, rewriteNode),
            _ => node
        };
    }

    private static PhysicalNode RewriteRecursiveCte(
        PhysicalRecursiveCteNode recursiveCte,
        Func<PhysicalNode, PhysicalNode> rewriteNode)
    {
        var anchor = rewriteNode(recursiveCte.Anchor);
        var member = rewriteNode(recursiveCte.RecursiveMember);
        var invariants = recursiveCte.Invariants
            .Select(invariant => invariant with { Plan = rewriteNode(invariant.Plan) })
            .ToArray();
        if (ReferenceEquals(anchor, recursiveCte.Anchor) &&
            ReferenceEquals(member, recursiveCte.RecursiveMember) &&
            invariants.Select(static invariant => invariant.Plan)
                .SequenceEqual(recursiveCte.Invariants.Select(static invariant => invariant.Plan), ReferenceEqualityComparer.Instance))
        {
            return recursiveCte;
        }

        return recursiveCte with
        {
            Anchor = anchor,
            RecursiveMember = member,
            Invariants = invariants
        };
    }

    public static bool TryResolveDirectSchemaScan(PhysicalNode input, [NotNullWhen(true)] out PhysicalSchemaScanNode? scan)
    {
        scan = input switch
        {
            PhysicalSchemaScanNode sourceScan => sourceScan,
            PhysicalProjectNode { IsDistinct: false, Input: PhysicalSchemaScanNode sourceScan } => sourceScan,
            _ => null
        };

        return scan != null;
    }

    private static PhysicalNode RewriteInput(
        PhysicalNode input,
        Func<PhysicalNode, PhysicalNode> rewriteNode,
        Func<PhysicalNode, PhysicalNode> createNode,
        PhysicalNode originalNode)
    {
        var rewrittenInput = rewriteNode(input);
        return ReferenceEquals(rewrittenInput, input)
            ? originalNode
            : createNode(rewrittenInput);
    }

    private static PhysicalNode RewritePair(
        PhysicalNode left,
        PhysicalNode right,
        Func<PhysicalNode, PhysicalNode> rewriteNode,
        Func<PhysicalNode, PhysicalNode, PhysicalNode> createNode,
        PhysicalNode originalNode)
    {
        var rewrittenLeft = rewriteNode(left);
        var rewrittenRight = rewriteNode(right);
        return ReferenceEquals(rewrittenLeft, left) && ReferenceEquals(rewrittenRight, right)
            ? originalNode
            : createNode(rewrittenLeft, rewrittenRight);
    }

    private static PhysicalCteNode RewriteCte(
        PhysicalCteNode cte,
        Func<PhysicalNode, PhysicalNode> rewriteNode)
    {
        var definitions = new PhysicalCteDefinition[cte.Definitions.Length];
        var changed = false;

        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = cte.Definitions[index];
            var rewrittenPlan = rewriteNode(definition.Plan);
            definitions[index] = ReferenceEquals(rewrittenPlan, definition.Plan)
                ? definition
                : new PhysicalCteDefinition(definition.Name, rewrittenPlan);
            changed |= !ReferenceEquals(definitions[index], definition);
        }

        var query = rewriteNode(cte.Query);
        changed |= !ReferenceEquals(query, cte.Query);

        return changed ? new PhysicalCteNode(definitions, query) : cte;
    }

    private static PhysicalMultiStatementNode RewriteMultiStatement(
        PhysicalMultiStatementNode multiStatement,
        Func<PhysicalNode, PhysicalNode> rewriteNode)
    {
        var statements = new PhysicalNode[multiStatement.Statements.Length];
        var changed = false;

        for (var index = 0; index < statements.Length; index++)
        {
            statements[index] = rewriteNode(multiStatement.Statements[index]);
            changed |= !ReferenceEquals(statements[index], multiStatement.Statements[index]);
        }

        return changed ? new PhysicalMultiStatementNode(statements) : multiStatement;
    }
}
