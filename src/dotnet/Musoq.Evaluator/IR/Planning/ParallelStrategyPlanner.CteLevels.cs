using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ParallelStrategyPlanner
{
    private List<PlannedParallelCteLevel> TryPlanParallelCteLevels(PhysicalCteNode node, out string reason)
    {
        if (!compilationOptions.UseCteParallelization)
        {
            reason = "Compilation option disables CTE parallelization.";
            return [];
        }

        if (compilationOptions.ParallelizationMode == ParallelizationMode.None)
        {
            reason = "Compilation option disables parallel execution.";
            return [];
        }

        if (cteExecutionPlan is not { CanParallelize: true })
        {
            reason = "CTE dependency plan is absent or not parallelizable.";
            return [];
        }

        var definitionNames = node.Definitions
            .Select(static definition => definition.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var levels = new List<PlannedParallelCteLevel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var level in cteExecutionPlan.Levels)
        {
            var names = new List<string>();

            foreach (var cteNode in level.Ctes)
            {
                if (!definitionNames.Contains(cteNode.Name))
                {
                    reason = "CTE dependency plan does not match physical CTE definitions.";
                    return [];
                }

                names.Add(cteNode.Name);
                seen.Add(cteNode.Name);
            }

            if (names.Count > 0)
                levels.Add(new PlannedParallelCteLevel(level.Level, names));
        }

        if (seen.Count != node.Definitions.Length)
        {
            reason = "CTE dependency plan does not cover every physical CTE definition.";
            return [];
        }

        reason = $"CTE dependency plan provides {levels.Count} parallel level(s).";
        return levels;
    }
}
