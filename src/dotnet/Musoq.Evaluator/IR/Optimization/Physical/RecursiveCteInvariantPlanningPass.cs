using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class RecursiveCteInvariantPlanningPass : IPhysicalOptimizationPass
{
    public string Name => "RecursiveCteInvariantPlanning";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var rewritten = Rewrite(
            plan,
            new Dictionary<string, OutputSchema>(StringComparer.Ordinal));

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(plan, "No recursive invariant inputs were found.")
            : OptimizationResult<PhysicalNode>.Changed(
                rewritten,
                "Extracted recursive invariant inputs into explicit physical definitions.");
    }

    private static PhysicalNode Rewrite(
        PhysicalNode node,
        IReadOnlyDictionary<string, OutputSchema> cteSchemas)
    {
        if (node is PhysicalCteNode cte)
        {
            var visibleSchemas = new Dictionary<string, OutputSchema>(cteSchemas, StringComparer.Ordinal);
            foreach (var definition in cte.Definitions)
                visibleSchemas[definition.Name] = definition.Plan.OutputSchema;

            var definitions = cte.Definitions
                .Select(definition => new PhysicalCteDefinition(
                    definition.Name,
                    Rewrite(definition.Plan, visibleSchemas)))
                .ToArray();
            var query = Rewrite(cte.Query, visibleSchemas);
            return new PhysicalCteNode(definitions, query);
        }

        if (node is PhysicalRecursiveCteNode recursive)
        {
            var anchor = Rewrite(recursive.Anchor, cteSchemas);
            var member = Rewrite(recursive.RecursiveMember, cteSchemas);
            return RecursiveCteInvariantPlanner.Plan(recursive with
            {
                Anchor = anchor,
                RecursiveMember = member
            }, cteSchemas);
        }

        return PhysicalPlanRewriter.RewriteChildren(node, child => Rewrite(child, cteSchemas));
    }
}
