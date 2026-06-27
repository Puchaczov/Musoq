using System;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using ColumnUsage = Musoq.Evaluator.IR.Optimization.PhysicalColumnUsageFacts;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class ProjectionPruningPhysicalPass : IPlanOptimizationPass<PhysicalNode>
{
    public string Name => "ProjectionPruning";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var rewriter = new ProjectionPruningRewriter();
        var rewritten = rewriter.Rewrite(plan);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(
                plan,
                "No simple projection chains were safe to prune.")
            : OptimizationResult<PhysicalNode>.Changed(
                rewritten,
                $"Pruned {rewriter.PrunedFields} unused projected field(s) from {rewriter.RewrittenProjects} simple projection chain(s), {rewriter.RewrittenAggregateInputs} aggregate input(s), {rewriter.RewrittenWindowInputs} window input(s), {rewriter.RewrittenJoinInputs} join input(s), {rewriter.RewrittenSetOperationInputs} set-operation input(s), and {rewriter.RewrittenCteDefinitions} CTE definition(s).");
    }

    private sealed class ProjectionPruningRewriter
    {
        private readonly ProjectionPruningBoundaryPruner _boundaryPruner;
        private int _prunedFields;
        private int _rewrittenProjects;
        private int _rewrittenCteDefinitions;

        public ProjectionPruningRewriter()
        {
            _boundaryPruner = new ProjectionPruningBoundaryPruner(Rewrite);
        }

        public int PrunedFields => _prunedFields + _boundaryPruner.PrunedFields;

        public int RewrittenProjects => _rewrittenProjects;

        public int RewrittenAggregateInputs => _boundaryPruner.RewrittenAggregateInputs;

        public int RewrittenJoinInputs => _boundaryPruner.RewrittenJoinInputs;

        public int RewrittenWindowInputs => _boundaryPruner.RewrittenWindowInputs;

        public int RewrittenCteDefinitions => _rewrittenCteDefinitions;

        public int RewrittenSetOperationInputs => _boundaryPruner.RewrittenSetOperationInputs;

        public PhysicalNode Rewrite(PhysicalNode node)
        {
            if (node is PhysicalProjectNode project)
                return RewriteProject(project);

            return node switch
            {
                PhysicalCteNode cte => RewriteCte(cte),
                PhysicalSetOperationNode setOperation => _boundaryPruner.RewriteSetOperation(setOperation),
                PhysicalAggregateOnlyNode aggregateOnly => _boundaryPruner.RewriteAggregateOnly(aggregateOnly),
                PhysicalSingleKeyAggregateNode singleKeyAggregate => _boundaryPruner.RewriteSingleKeyAggregate(singleKeyAggregate),
                PhysicalValueTupleAggregateNode valueTupleAggregate => _boundaryPruner.RewriteValueTupleAggregate(valueTupleAggregate),
                _ => PhysicalPlanRewriter.RewriteChildren(node, Rewrite)
            };
        }

        private PhysicalNode RewriteCte(PhysicalCteNode cte)
        {
            var definitions = new PhysicalCteDefinition[cte.Definitions.Length];
            var changed = false;

            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = cte.Definitions[index];
                var rewrittenPlan = Rewrite(definition.Plan);
                definitions[index] = ReferenceEquals(rewrittenPlan, definition.Plan)
                    ? definition
                    : new PhysicalCteDefinition(definition.Name, rewrittenPlan);
                changed |= !ReferenceEquals(definitions[index], definition);
            }

            var query = Rewrite(cte.Query);
            changed |= !ReferenceEquals(query, cte.Query);
            var rewrittenCte = changed ? new PhysicalCteNode(definitions, query) : cte;

            return PruneCteDefinitions(rewrittenCte);
        }

        private PhysicalCteNode PruneCteDefinitions(PhysicalCteNode cte)
        {
            var result = ProjectionPruningCteDefinitions.Prune(cte);
            _prunedFields += result.PrunedFields;
            _rewrittenCteDefinitions += result.RewrittenDefinitions;

            return result.Node;
        }

        private PhysicalNode RewriteProject(PhysicalProjectNode project)
        {
            var input = Rewrite(project.Input);
            var current = ReferenceEquals(input, project.Input)
                ? project
                : new PhysicalProjectNode(project.Fields, input) { IsDistinct = project.IsDistinct };

            current = _boundaryPruner.RewriteProjectBoundaries(current);

            if (!PhysicalProjectionBoundaryInputPruner.TryFindPrunableInnerProject(
                    current.Input,
                    ColumnUsage.CollectReferencedNames(current.Fields),
                    out var inner,
                    out var requiredNames,
                    out var rebuildInput) ||
                inner.IsDistinct ||
                !PhysicalProjectionBoundaryInputPruner.TrySelectRequiredInnerFields(requiredNames, inner.Fields, out var prunedFields))
            {
                return current;
            }

            _prunedFields += inner.Fields.Length - prunedFields.Length;
            _rewrittenProjects++;

            var prunedInner = new PhysicalProjectNode(prunedFields, inner.Input)
            {
                IsDistinct = inner.IsDistinct
            };

            return new PhysicalProjectNode(
                current.Fields,
                rebuildInput(prunedInner))
            {
                IsDistinct = current.IsDistinct
            };
        }
    }
}
