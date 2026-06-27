using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class BoundaryRowShapePlanner
{
    public static BoundaryRowShapePlanningResult Plan(PhysicalNode physicalPlan, PlanProperties properties)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(properties);
        var state = new BoundaryRowShapePlanningState(properties);
        state.Visit(physicalPlan, SchemaColumns(physicalPlan.OutputSchema));

        return new BoundaryRowShapePlanningResult(state.Plans, state.Decisions);
    }

    private sealed partial class BoundaryRowShapePlanningState(PlanProperties properties)
    {
        private readonly ColumnUsageIndex _usageIndex = ColumnUsageIndex.Create(properties);
        private readonly List<BoundaryRowShapePlan> _plans = [];
        private readonly Dictionary<BoundaryRowShapeKind, int> _indexes = new();

        public IReadOnlyList<BoundaryRowShapePlan> Plans => _plans;

        public IReadOnlyList<PlanningDecision> Decisions => _plans.Select(CreateDecision).ToArray();

        public void Visit(PhysicalNode node, IReadOnlyList<string> requiredAfter)
        {
            switch (node)
            {
                case PhysicalProjectNode project:
                    VisitProject(project, requiredAfter);
                    return;
                case PhysicalSortNode sort:
                    VisitOrderedBoundary(sort.Input, sort.Keys, BoundaryRowShapeKind.Sort, requiredAfter);
                    return;
                case PhysicalTopNNode topN:
                    VisitOrderedBoundary(topN.Input, topN.Keys, BoundaryRowShapeKind.TopN, requiredAfter);
                    return;
                case PhysicalTopOffsetNode topOffset:
                    VisitOrderedBoundary(topOffset.Input, topOffset.Keys, BoundaryRowShapeKind.TopOffset, requiredAfter);
                    return;
                case PhysicalAggregateOnlyNode aggregateOnly:
                    VisitAggregate(aggregateOnly.Input, aggregateOnly.OutputSchema, CollectAggregateColumns(aggregateOnly.Bindings), requiredAfter);
                    return;
                case PhysicalSingleKeyAggregateNode singleKeyAggregate:
                    VisitAggregate(singleKeyAggregate.Input, singleKeyAggregate.OutputSchema, CollectAggregateColumns(singleKeyAggregate.GroupKey, singleKeyAggregate.Bindings), requiredAfter);
                    return;
                case PhysicalValueTupleAggregateNode valueTupleAggregate:
                    VisitAggregate(valueTupleAggregate.Input, valueTupleAggregate.OutputSchema, CollectAggregateColumns(valueTupleAggregate.GroupKeys, valueTupleAggregate.Bindings), requiredAfter);
                    return;
                case PhysicalWindowNode window:
                    VisitWindow(window, requiredAfter);
                    return;
                case PhysicalSetOperationNode setOperation:
                    VisitSetOperation(setOperation, requiredAfter);
                    return;
                case PhysicalHashJoinNode hashJoin:
                    VisitHashJoin(hashJoin, requiredAfter);
                    return;
                case PhysicalCteNode cte:
                    VisitCte(cte, requiredAfter);
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
            var childRequired = CollectColumns(fields.Select(static field => field.Expression));

            if (project.IsDistinct)
            {
                var distinctOutputColumns = SchemaColumns(project.OutputSchema);
                var distinctInputRequired = CollectColumns(project.Fields.Select(static field => field.Expression));

                AddPlan(
                    CreateBoundaryId(BoundaryRowShapeKind.Distinct),
                    BoundaryRowShapeKind.Distinct,
                    distinctOutputColumns,
                    ResolveNeededAfter(requiredAfter),
                    distinctOutputColumns,
                    "Distinct materialization keeps all key columns for duplicate removal; post-distinct row-width pruning may drop columns unused downstream.");
                Visit(project.Input, distinctInputRequired);
                return;
            }

            Visit(project.Input, childRequired);
        }

        private void VisitOrderedBoundary(
            PhysicalNode input,
            IReadOnlyList<OrderField> keys,
            BoundaryRowShapeKind kind,
            IReadOnlyList<string> requiredAfter)
        {
            var operationColumns = CollectOrderColumns(keys);
            var neededAfter = ResolveNeededAfter(requiredAfter);

            AddPlan(
                kind,
                input,
                neededAfter,
                operationColumns,
                $"{kind} boundary uses ordering columns but keeps the current row shape unchanged; no physical pruning was applied.");

            Visit(input, Merge(neededAfter, operationColumns));
        }

        private void VisitAggregate(
            PhysicalNode input,
            OutputSchema outputSchema,
            IReadOnlyList<string> operationColumns,
            IReadOnlyList<string> requiredAfter)
        {
            AddPlan(
                BoundaryRowShapeKind.Aggregate,
                input,
                Merge(SchemaColumns(outputSchema), requiredAfter),
                operationColumns,
                "Aggregate boundary separates input columns from aggregate output columns; no physical pruning was applied.");

            Visit(input, operationColumns);
        }

        private void VisitWindow(PhysicalWindowNode window, IReadOnlyList<string> requiredAfter)
        {
            var operationColumns = CollectWindowColumns(window.Registrations);
            var input = UnwrapMaterialize(window.Input);
            var neededAfter = ResolveNeededAfter(requiredAfter);

            AddPlan(
                BoundaryRowShapeKind.Window,
                input,
                neededAfter,
                operationColumns,
                "Window boundary uses partition/order/value columns but keeps the current row shape unchanged; no physical pruning was applied.");

            Visit(window.Input, Merge(neededAfter, operationColumns));
        }

        private void VisitSetOperation(PhysicalSetOperationNode setOperation, IReadOnlyList<string> requiredAfter)
        {
            var leftOperationColumns = ResolveSetOperationColumns(setOperation.Left, setOperation.FieldIndexes);
            var rightOperationColumns = ResolveSetOperationColumns(setOperation.Right, setOperation.FieldIndexes);
            var operationColumns = leftOperationColumns.Concat(rightOperationColumns).ToArray();
            var inputColumns = ResolveAvailableColumns(setOperation.Left)
                .Concat(ResolveAvailableColumns(setOperation.Right))
                .ToArray();
            AddPlan(
                CreateBoundaryId(BoundaryRowShapeKind.SetOperation),
                BoundaryRowShapeKind.SetOperation,
                inputColumns,
                ResolveNeededAfter(requiredAfter),
                operationColumns,
                "Set operation boundary compares key columns while preserving the current row shape; no physical pruning was applied.");

            Visit(setOperation.Left, Merge(requiredAfter, leftOperationColumns));
            Visit(setOperation.Right, Merge(requiredAfter, rightOperationColumns));
        }

        private void VisitCte(PhysicalCteNode cte, IReadOnlyList<string> requiredAfter)
        {
            var queryColumns = CollectColumns(cte.Query);

            foreach (var definition in cte.Definitions)
            {
                AddPlan(
                    $"cte:{definition.Name}",
                    BoundaryRowShapeKind.CteMaterialization,
                    ResolveAvailableColumns(definition.Plan),
                    FilterColumnsForCte(definition.Name, queryColumns),
                    [],
                    "CTE materialization stores the current definition row shape; no physical pruning was applied.");

                Visit(definition.Plan, SchemaColumns(definition.Plan.OutputSchema));
            }

            Visit(cte.Query, requiredAfter);
        }

        private void AddPlan(
            BoundaryRowShapeKind kind,
            PhysicalNode input,
            IReadOnlyList<string> neededAfter,
            IReadOnlyList<string> operationColumns,
            string reason)
        {
            AddPlan(
                CreateBoundaryId(kind),
                kind,
                ResolveAvailableColumns(input),
                neededAfter,
                operationColumns,
                reason);
        }

        private void AddPlan(
            string boundaryId,
            BoundaryRowShapeKind kind,
            IReadOnlyList<string> inputColumns,
            IReadOnlyList<string> neededAfter,
            IReadOnlyList<string> operationColumns,
            string reason)
        {
            var orderedInputColumns = OrderColumns(inputColumns);
            var neededAfterColumns = OrderColumns(neededAfter);
            var operationOnlyColumns = kind == BoundaryRowShapeKind.HashJoinBuild
                ? OrderColumns(operationColumns)
                : OrderColumns(operationColumns.Where(column => !ContainsColumn(neededAfterColumns, column)));
            var unusedInputColumns = orderedInputColumns.Where(column =>
                !ContainsColumn(neededAfterColumns, column) &&
                !ContainsColumn(operationColumns, column) &&
                !_usageIndex.IsRequired(column));
            var droppableColumns = OrderColumns(operationOnlyColumns.Concat(unusedInputColumns));
            var retainedColumns = OrderColumns(orderedInputColumns.Where(column => !ContainsColumn(droppableColumns, column)));
            var blockedColumns = IsCurrentlyPrunableBoundary(kind) ? [] : droppableColumns;
            _plans.Add(new BoundaryRowShapePlan(
                boundaryId,
                kind,
                orderedInputColumns,
                neededAfterColumns,
                operationOnlyColumns,
                droppableColumns,
                operationOnlyColumns.Length == 0 && droppableColumns.Length == 0 ? PlanningConfidence.Low : PlanningConfidence.Medium,
                reason)
            {
                SemanticColumns = neededAfterColumns,
                RetainedExecutionColumns = retainedColumns,
                CandidateColumns = droppableColumns,
                BlockedColumns = blockedColumns
            });
        }

        private string CreateBoundaryId(BoundaryRowShapeKind kind)
        {
            _indexes.TryGetValue(kind, out var index);
            _indexes[kind] = index + 1;
            return $"{FormatKindPrefix(kind)}:{index}";
        }

        private static string[] ResolveNeededAfter(IReadOnlyList<string> requiredAfter)
        {
            return OrderColumns(requiredAfter);
        }
    }
}
