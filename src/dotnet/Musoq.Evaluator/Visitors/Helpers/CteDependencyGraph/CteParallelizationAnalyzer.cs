using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Analyzes CTE dependencies and creates execution plans for parallelization.
///     Identifies which CTEs can run in parallel based on their dependencies.
/// </summary>
public static class CteParallelizationAnalyzer
{
    /// <summary>
    ///     Creates an execution plan for the given CTE expression.
    ///     The plan groups CTEs into levels where all CTEs at the same level can run in parallel.
    /// </summary>
    /// <param name="cteExpression">The CTE expression to analyze.</param>
    /// <returns>An execution plan for parallel CTE execution.</returns>
    public static CteExecutionPlan CreatePlan(CteExpressionNode cteExpression)
    {
        var builder = new CteDependencyGraphBuilder();
        var graph = builder.Build(cteExpression);
        return CreatePlan(graph);
    }

    /// <summary>
    ///     Creates an execution plan from an existing dependency graph.
    ///     Uses the execution levels already computed in the graph.
    /// </summary>
    /// <param name="graph">The dependency graph to create a plan from.</param>
    /// <returns>An execution plan for parallel CTE execution.</returns>
    public static CteExecutionPlan CreatePlan(CteDependencyGraph graph)
    {
        var levels = new List<CteExecutionLevel>();


        for (var i = 0; i < graph.ExecutionLevels.Count; i++)
        {
            var ctesAtLevel = graph.ExecutionLevels[i];
            if (ctesAtLevel.Count > 0) levels.Add(new CteExecutionLevel(i, ctesAtLevel.ToList()));
        }

        return new CteExecutionPlan(levels, graph);
    }

    /// <summary>
    ///     Determines if parallelization would be beneficial for the given CTE expression.
    ///     Returns true if there are at least two CTEs that can run in parallel.
    /// </summary>
    /// <param name="cteExpression">The CTE expression to analyze.</param>
    /// <returns>True if parallelization is possible and beneficial.</returns>
    public static bool CanBenefitFromParallelization(CteExpressionNode cteExpression)
    {
        var builder = new CteDependencyGraphBuilder();
        var graph = builder.Build(cteExpression);
        return graph.CanParallelize;
    }

    /// <summary>
    ///     Determines if parallelization would be beneficial based on an existing graph.
    /// </summary>
    /// <param name="graph">The dependency graph to check.</param>
    /// <returns>True if parallelization is possible and beneficial.</returns>
    public static bool CanBenefitFromParallelization(CteDependencyGraph graph)
    {
        return graph.CanParallelize;
    }

    /// <summary>
    ///     Gets the names of CTEs at each execution level from a CTE expression.
    ///     Useful for debugging and testing.
    /// </summary>
    /// <param name="cteExpression">The CTE expression to analyze.</param>
    /// <returns>A list of lists, where each inner list contains CTE names at that level.</returns>
    public static IReadOnlyList<IReadOnlyList<string>> GetExecutionLevelNames(CteExpressionNode cteExpression)
    {
        var plan = CreatePlan(cteExpression);
        return plan.Levels
            .Select(l => l.Ctes.Select(c => c.Name).ToList() as IReadOnlyList<string>)
            .ToList();
    }
}
