using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnBoundaryPlanner
{
    private sealed class JoinEdgeCollector
    {
        private readonly List<RequiredColumnBoundaryPlan> _plans = [];
        private int _joinIndex;

        public IReadOnlyList<RequiredColumnBoundaryPlan> Plans => _plans;

        public void Visit(PhysicalNode node, IReadOnlyList<string> requiredAfter)
        {
            switch (node)
            {
                case PhysicalProjectNode project:
                    VisitProject(project, requiredAfter);
                    return;
                case PhysicalFilterNode filter:
                    Visit(filter.Input, Merge(requiredAfter, CollectColumns(filter.Predicate)));
                    return;
                case PhysicalHavingFilterNode having:
                    Visit(having.Input, Merge(requiredAfter, CollectColumns(having.Predicate)));
                    return;
                case PhysicalQualifyFilterNode qualify:
                    Visit(qualify.Input, Merge(requiredAfter, CollectColumns(qualify.Predicate)));
                    return;
                case PhysicalSortNode sort:
                    Visit(sort.Input, Merge(requiredAfter, CollectOrderColumns(sort.Keys)));
                    return;
                case PhysicalTopNNode topN:
                    Visit(topN.Input, Merge(requiredAfter, CollectOrderColumns(topN.Keys)));
                    return;
                case PhysicalTopOffsetNode topOffset:
                    Visit(topOffset.Input, Merge(requiredAfter, CollectOrderColumns(topOffset.Keys)));
                    return;
                case PhysicalSkipNode skip:
                    Visit(skip.Input, requiredAfter);
                    return;
                case PhysicalTakeNode take:
                    Visit(take.Input, requiredAfter);
                    return;
                case PhysicalAggregateOnlyNode aggregateOnly:
                    Visit(aggregateOnly.Input, CollectAggregateColumns(aggregateOnly.Bindings));
                    return;
                case PhysicalSingleKeyAggregateNode singleKeyAggregate:
                    Visit(singleKeyAggregate.Input, CollectAggregateColumns(singleKeyAggregate.GroupKey, singleKeyAggregate.Bindings));
                    return;
                case PhysicalValueTupleAggregateNode valueTupleAggregate:
                    Visit(valueTupleAggregate.Input, CollectAggregateColumns(valueTupleAggregate.GroupKeys, valueTupleAggregate.Bindings));
                    return;
                case PhysicalWindowNode window:
                    Visit(window.Input, Merge(requiredAfter, CollectWindowColumns(window.Registrations)));
                    return;
                case PhysicalSetOperationNode setOperation:
                    VisitSetOperation(setOperation, requiredAfter);
                    return;
                case PhysicalHashJoinNode hashJoin:
                    VisitHashJoin(hashJoin, requiredAfter);
                    return;
                case PhysicalNestedLoopJoinNode nestedLoopJoin:
                    VisitPredicateJoin(
                        nestedLoopJoin.Left,
                        nestedLoopJoin.Right,
                        nestedLoopJoin.OnPredicate,
                        nestedLoopJoin.TieBreak,
                        requiredAfter);
                    return;
                case PhysicalSortMergeJoinNode sortMergeJoin:
                    VisitSortMergeJoin(sortMergeJoin, requiredAfter);
                    return;
                case PhysicalCteNode cte:
                    foreach (var definition in cte.Definitions)
                        Visit(definition.Plan, SchemaColumns(definition.Plan.OutputSchema));

                    Visit(cte.Query, requiredAfter);
                    return;
            }

            foreach (var child in node.Children)
                Visit(child, SchemaColumns(child.OutputSchema));
        }

        private void VisitProject(PhysicalProjectNode project, IReadOnlyList<string> requiredAfter)
        {
            var requiredFields = project.Fields
                .Where(field => requiredAfter.Count == 0 || ContainsColumn(requiredAfter, field.OutputName))
                .ToArray();
            var fields = requiredFields.Length == 0 ? project.Fields : requiredFields;

            Visit(project.Input, CollectColumns(fields.Select(static field => field.Expression)));
        }

        private void VisitSetOperation(PhysicalSetOperationNode setOperation, IReadOnlyList<string> requiredAfter)
        {
            var leftOperationColumns = ResolveSetOperationColumns(setOperation.Left, setOperation.FieldIndexes);
            var rightOperationColumns = ResolveSetOperationColumns(setOperation.Right, setOperation.FieldIndexes);

            Visit(setOperation.Left, Merge(requiredAfter, leftOperationColumns));
            Visit(setOperation.Right, Merge(requiredAfter, rightOperationColumns));
        }

        private void VisitHashJoin(PhysicalHashJoinNode join, IReadOnlyList<string> requiredAfter)
        {
            var residualColumns = join.Residual != null ? CollectColumns(join.Residual) : [];
            var leftRequired = Merge(
                FilterProducedColumns(join.Left, requiredAfter),
                FilterProducedColumns(join.Left, CollectColumns(join.ProbeKeys).Concat(residualColumns)));
            var rightRequired = Merge(
                FilterProducedColumns(join.Right, requiredAfter),
                FilterProducedColumns(join.Right, CollectColumns(join.BuildKeys).Concat(residualColumns)));

            AddJoinEdgePlans(leftRequired, rightRequired, join.Left, join.Right);
            Visit(join.Left, leftRequired);
            Visit(join.Right, rightRequired);
        }

        private void VisitPredicateJoin(
            PhysicalNode left,
            PhysicalNode right,
            Musoq.Evaluator.IR.Expressions.IrExpression predicate,
            OrderField? tieBreak,
            IReadOnlyList<string> requiredAfter)
        {
            var predicateColumns = tieBreak == null
                ? CollectColumns(predicate)
                : Merge(CollectColumns(predicate), CollectColumns(tieBreak.Expression));
            var leftRequired = Merge(
                FilterProducedColumns(left, requiredAfter),
                FilterProducedColumns(left, predicateColumns));
            var rightRequired = Merge(
                FilterProducedColumns(right, requiredAfter),
                FilterProducedColumns(right, predicateColumns));

            AddJoinEdgePlans(leftRequired, rightRequired, left, right);
            Visit(left, leftRequired);
            Visit(right, rightRequired);
        }

        private void VisitSortMergeJoin(PhysicalSortMergeJoinNode join, IReadOnlyList<string> requiredAfter)
        {
            var residualColumns = CollectColumns(join.Residual);
            var leftRequired = Merge(
                FilterProducedColumns(join.Left, requiredAfter),
                FilterProducedColumns(join.Left, CollectColumns(join.LeftKey).Concat(residualColumns)));
            var rightRequired = Merge(
                FilterProducedColumns(join.Right, requiredAfter),
                FilterProducedColumns(join.Right, CollectColumns(join.RightKey).Concat(residualColumns)));

            AddJoinEdgePlans(leftRequired, rightRequired, join.Left, join.Right);
            Visit(join.Left, leftRequired);
            Visit(join.Right, rightRequired);
        }

        private void AddJoinEdgePlans(
            IReadOnlyList<string> leftRequired,
            IReadOnlyList<string> rightRequired,
            PhysicalNode left,
            PhysicalNode right)
        {
            var index = _joinIndex++;
            _plans.Add(CreateJoinEdgePlan($"join:{index}:left", RequiredColumnBoundaryKind.JoinLeftEdge, left, leftRequired));
            _plans.Add(CreateJoinEdgePlan($"join:{index}:right", RequiredColumnBoundaryKind.JoinRightEdge, right, rightRequired));
        }

        private static RequiredColumnBoundaryPlan CreateJoinEdgePlan(
            string boundaryId,
            RequiredColumnBoundaryKind kind,
            PhysicalNode input,
            IReadOnlyList<string> requiredColumns)
        {
            var availableColumns = ResolveAvailableColumns(input);
            var required = OrderColumns(requiredColumns);
            var retained = required.Length == 0 ? availableColumns : required;
            var blocked = required.Length == 0
                ? []
                : OrderColumns(availableColumns.Where(column => !ContainsColumn(required, column)));

            return new RequiredColumnBoundaryPlan(
                boundaryId,
                kind,
                required,
                retained,
                blocked,
                CreateMappings(retained),
                blocked.Length == 0 ? PlanningConfidence.Low : PlanningConfidence.Medium,
                $"{kind} required-column facts are diagnostic-only; join-edge projection pruning is not applied in this wave.");
        }
    }
}
