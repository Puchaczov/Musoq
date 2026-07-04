using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using ColumnUsage = Musoq.Evaluator.IR.Optimization.Physical.PhysicalColumnUsageFacts;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static class ProjectionPruningCteDefinitions
{
    public static ProjectionPruningCteResult Prune(PhysicalCteNode cte)
    {
        ArgumentNullException.ThrowIfNull(cte);

        var requirements = CteConsumerRequirementCollector.Collect(cte);
        if (requirements.Count == 0)
            return ProjectionPruningCteResult.NoChange(cte);

        var definitions = new PhysicalCteDefinition[cte.Definitions.Length];
        var schemaUpdates = new Dictionary<string, OutputSchema>(StringComparer.OrdinalIgnoreCase);
        var prunedFields = 0;
        var rewrittenDefinitions = 0;
        var changed = false;

        for (var index = 0; index < cte.Definitions.Length; index++)
        {
            var definition = cte.Definitions[index];
            if (requirements.TryGetValue(definition.Name, out var requiredColumns))
            {
                var pruning = PruneDefinitionOutput(definition.Plan, requiredColumns);
                if (pruning.IsChanged)
                {
                    definitions[index] = new PhysicalCteDefinition(definition.Name, pruning.Plan);
                    schemaUpdates[definition.Name] = pruning.Plan.OutputSchema;
                    prunedFields += pruning.PrunedFieldCount;
                    rewrittenDefinitions++;
                    changed = true;
                    continue;
                }
            }

            definitions[index] = definition;
        }

        if (!changed)
            return ProjectionPruningCteResult.NoChange(cte);

        for (var index = 0; index < definitions.Length; index++)
        {
            var rewrittenPlan = RewriteCteRefSchemas(definitions[index].Plan, schemaUpdates);
            if (!ReferenceEquals(rewrittenPlan, definitions[index].Plan))
                definitions[index] = new PhysicalCteDefinition(definitions[index].Name, rewrittenPlan);
        }

        var query = RewriteCteRefSchemas(cte.Query, schemaUpdates);
        return new ProjectionPruningCteResult(
            new PhysicalCteNode(definitions, query),
            prunedFields,
            rewrittenDefinitions);
    }

    private static CteProjectionPruning PruneDefinitionOutput(
        PhysicalNode plan,
        IReadOnlySet<string> requiredColumns)
    {
        if (plan is PhysicalMultiStatementNode multiStatement && multiStatement.Statements.Length > 0)
        {
            var statements = multiStatement.Statements.ToArray();
            var pruning = PruneDefinitionOutput(statements[^1], requiredColumns);
            if (!pruning.IsChanged)
                return CteProjectionPruning.NoChange(plan);

            statements[^1] = pruning.Plan;
            return new CteProjectionPruning(
                new PhysicalMultiStatementNode(statements),
                pruning.PrunedFieldCount,
                IsChanged: true);
        }

        if (plan is not PhysicalProjectNode { IsDistinct: false } project ||
            ColumnUsage.HasAmbiguousOutputNames(project.Fields) ||
            requiredColumns.Count == 0 ||
            requiredColumns.Count >= project.Fields.Length ||
            requiredColumns.Any(required => !project.Fields.Any(field => ColumnUsage.NameEquals(field.OutputName, required))))
        {
            return CteProjectionPruning.NoChange(plan);
        }

        var fields = project.Fields
            .Where(field => requiredColumns.Contains(field.OutputName))
            .Select((field, index) => field with { OutputIndex = index })
            .ToArray();

        if (fields.Length == project.Fields.Length)
            return CteProjectionPruning.NoChange(plan);

        return new CteProjectionPruning(
            new PhysicalProjectNode(fields, project.Input),
            project.Fields.Length - fields.Length,
            IsChanged: true);
    }

    private static PhysicalNode RewriteCteRefSchemas(
        PhysicalNode node,
        IReadOnlyDictionary<string, OutputSchema> schemasByName)
    {
        if (node is PhysicalCteRefNode cteRef &&
            schemasByName.TryGetValue(cteRef.CteName, out var schema))
        {
            return cteRef with { OutputSchema = schema };
        }

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => RewriteCteRefSchemas(child, schemasByName));
    }

    private sealed class CteConsumerRequirementCollector(IReadOnlySet<string> cteNames)
    {
        private readonly Dictionary<string, HashSet<string>> _requiredByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _requiresAll = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, IReadOnlySet<string>> Collect(PhysicalCteNode cte)
        {
            var names = cte.Definitions
                .Select(static definition => definition.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var collector = new CteConsumerRequirementCollector(names);

            foreach (var definition in cte.Definitions)
                collector.Visit(definition.Plan, ColumnUsage.SchemaColumns(definition.Plan.OutputSchema));

            collector.Visit(cte.Query, ColumnUsage.SchemaColumns(cte.Query.OutputSchema));
            return collector.CreateResult(cte.Definitions);
        }

        private void Visit(PhysicalNode node, IReadOnlyList<string> requiredAfter)
        {
            switch (node)
            {
                case PhysicalCteRefNode cteRef:
                    Record(cteRef, requiredAfter);
                    return;
                case PhysicalProjectNode project:
                    VisitProject(project, requiredAfter);
                    return;
                case PhysicalFilterNode filter:
                    VisitFilteredInput(filter.Input, filter.Predicate, requiredAfter);
                    return;
                case PhysicalHavingFilterNode having:
                    VisitFilteredInput(having.Input, having.Predicate, requiredAfter);
                    return;
                case PhysicalQualifyFilterNode qualify:
                    VisitFilteredInput(qualify.Input, qualify.Predicate, requiredAfter);
                    return;
                case PhysicalSortNode sort:
                    VisitOrderedInput(sort.Input, sort.Keys, requiredAfter);
                    return;
                case PhysicalTopNNode topN:
                    VisitOrderedInput(topN.Input, topN.Keys, requiredAfter);
                    return;
                case PhysicalTopOffsetNode topOffset:
                    VisitOrderedInput(topOffset.Input, topOffset.Keys, requiredAfter);
                    return;
                case PhysicalSkipNode skip:
                    Visit(skip.Input, requiredAfter);
                    return;
                case PhysicalTakeNode take:
                    Visit(take.Input, requiredAfter);
                    return;
                case PhysicalHashJoinNode hashJoin:
                    VisitJoin(hashJoin.Left, hashJoin.Right, requiredAfter, CollectJoinColumns(hashJoin));
                    return;
                case PhysicalJoinCandidateNode joinCandidate:
                    VisitJoin(
                        joinCandidate.Left,
                        joinCandidate.Right,
                        requiredAfter,
                        CollectPredicateJoinColumns(joinCandidate.OnPredicate, joinCandidate.TieBreak));
                    return;
                case PhysicalNestedLoopJoinNode nestedLoopJoin:
                    VisitJoin(
                        nestedLoopJoin.Left,
                        nestedLoopJoin.Right,
                        requiredAfter,
                        CollectPredicateJoinColumns(nestedLoopJoin.OnPredicate, nestedLoopJoin.TieBreak));
                    return;
                case PhysicalSortMergeJoinNode sortMergeJoin:
                    VisitJoin(sortMergeJoin.Left, sortMergeJoin.Right, requiredAfter, CollectJoinColumns(sortMergeJoin));
                    return;
                case PhysicalSetOperationNode setOperation:
                    VisitSetOperationArm(setOperation.Left, setOperation.FieldIndexes, requiredAfter);
                    VisitSetOperationArm(setOperation.Right, setOperation.FieldIndexes, requiredAfter);
                    return;
                case PhysicalMultiStatementNode multiStatement:
                    foreach (var statement in multiStatement.Statements)
                        Visit(statement, ColumnUsage.SchemaColumns(statement.OutputSchema));
                    return;
            }

            foreach (var child in node.Children)
                Visit(child, ColumnUsage.SchemaColumns(child.OutputSchema));
        }

        private void VisitProject(PhysicalProjectNode project, IReadOnlyList<string> requiredAfter)
        {
            var requiredFields = project.Fields
                .Where(field => requiredAfter.Count == 0 || ColumnUsage.ContainsColumn(requiredAfter, field.OutputName))
                .ToArray();
            var fields = requiredFields.Length == 0 ? project.Fields : requiredFields;

            Visit(project.Input, ColumnUsage.CollectColumnNames(fields.Select(static field => field.Expression)));
        }

        private static IReadOnlyList<string> CollectPredicateJoinColumns(
            IrExpression predicate,
            OrderField? tieBreak)
        {
            return tieBreak == null
                ? ColumnUsage.CollectColumnNames(predicate)
                : ColumnUsage.Merge(
                    ColumnUsage.CollectColumnNames(predicate),
                    ColumnUsage.CollectColumnNames(tieBreak.Expression));
        }

        private void VisitFilteredInput(
            PhysicalNode input,
            IrExpression predicate,
            IReadOnlyList<string> requiredAfter)
        {
            Visit(
                input,
                ColumnUsage.Merge(requiredAfter, ColumnUsage.CollectColumnNames(predicate)));
        }

        private void VisitOrderedInput(
            PhysicalNode input,
            IReadOnlyList<OrderField> keys,
            IReadOnlyList<string> requiredAfter)
        {
            var requiredColumns = ColumnUsage.Merge(
                requiredAfter,
                keys.SelectMany(static key => ColumnUsage.CollectColumnNames(key.Expression)));

            Visit(input, requiredColumns);
        }

        private void VisitSetOperationArm(
            PhysicalNode arm,
            IReadOnlyList<int> fieldIndexes,
            IReadOnlyList<string> requiredAfter)
        {
            var requiredColumns = ColumnUsage.Merge(
                requiredAfter,
                ColumnUsage.ResolveSetOperationColumns(arm, fieldIndexes));

            Visit(arm, requiredColumns);
        }

        private void VisitJoin(
            PhysicalNode left,
            PhysicalNode right,
            IReadOnlyList<string> requiredAfter,
            IReadOnlyList<string> joinColumns)
        {
            Visit(
                left,
                ColumnUsage.Merge(
                    ColumnUsage.FilterProducedColumns(left, requiredAfter),
                    ColumnUsage.FilterProducedColumns(left, joinColumns)));
            Visit(
                right,
                ColumnUsage.Merge(
                    ColumnUsage.FilterProducedColumns(right, requiredAfter),
                    ColumnUsage.FilterProducedColumns(right, joinColumns)));
        }

        private void Record(PhysicalCteRefNode cteRef, IReadOnlyList<string> requiredAfter)
        {
            if (!cteNames.Contains(cteRef.CteName))
                return;

            var available = ColumnUsage.SchemaColumns(cteRef.OutputSchema);
            var required = requiredAfter.Count == 0
                ? available
                : available.Where(column =>
                    ColumnUsage.ContainsColumn(requiredAfter, column) ||
                    ColumnUsage.ContainsColumn(requiredAfter, $"{cteRef.Alias}.{column}")).ToArray();

            if (required.Length == 0 || required.Length >= available.Length)
            {
                _requiresAll.Add(cteRef.CteName);
                return;
            }

            if (!_requiredByName.TryGetValue(cteRef.CteName, out var columns))
            {
                columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _requiredByName[cteRef.CteName] = columns;
            }

            foreach (var column in required)
                columns.Add(column);
        }

        private IReadOnlyDictionary<string, IReadOnlySet<string>> CreateResult(
            IReadOnlyList<PhysicalCteDefinition> definitions)
        {
            var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var definition in definitions)
            {
                if (_requiresAll.Contains(definition.Name) ||
                    !_requiredByName.TryGetValue(definition.Name, out var required) ||
                    required.Count == 0 ||
                    required.Count >= definition.Plan.OutputSchema.Columns.Length)
                {
                    continue;
                }

                result[definition.Name] = required;
            }

            return result;
        }
    }

    private static string[] CollectJoinColumns(PhysicalHashJoinNode join)
    {
        var residualColumns = join.Residual != null ? ColumnUsage.CollectColumnNames(join.Residual) : [];
        return ColumnUsage.CollectColumnNames(join.BuildKeys)
            .Concat(ColumnUsage.CollectColumnNames(join.ProbeKeys))
            .Concat(residualColumns)
            .ToArray();
    }

    private static string[] CollectJoinColumns(PhysicalSortMergeJoinNode join)
    {
        return ColumnUsage.CollectColumnNames(join.LeftKey)
            .Concat(ColumnUsage.CollectColumnNames(join.RightKey))
            .Concat(ColumnUsage.CollectColumnNames(join.Residual))
            .ToArray();
    }

    private sealed record CteProjectionPruning(
        PhysicalNode Plan,
        int PrunedFieldCount,
        bool IsChanged)
    {
        public static CteProjectionPruning NoChange(PhysicalNode plan)
        {
            return new CteProjectionPruning(plan, PrunedFieldCount: 0, IsChanged: false);
        }
    }
}

