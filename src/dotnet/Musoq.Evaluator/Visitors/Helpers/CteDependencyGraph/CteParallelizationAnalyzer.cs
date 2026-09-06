using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        return CreatePlan(cteExpression, CancellationToken.None);
    }

    /// <summary>
    ///     Creates an execution plan for a CTE expression and observes cancellation.
    /// </summary>
    /// <param name="cteExpression">The CTE expression to analyze.</param>
    /// <param name="cancellationToken">Token used to cancel CTE analysis.</param>
    /// <returns>An execution plan for parallel CTE execution.</returns>
    public static CteExecutionPlan CreatePlan(
        CteExpressionNode cteExpression,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new CteDependencyGraphBuilder();
        var graph = builder.Build(cteExpression, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return CreatePlan(graph, cancellationToken);
    }

    /// <summary>
    ///     Creates an execution plan from an existing dependency graph.
    ///     Uses the execution levels already computed in the graph.
    /// </summary>
    /// <param name="graph">The dependency graph to create a plan from.</param>
    /// <returns>An execution plan for parallel CTE execution.</returns>
    public static CteExecutionPlan CreatePlan(CteDependencyGraph graph)
    {
        return CreatePlan(graph, CancellationToken.None);
    }

    /// <summary>
    ///     Creates an execution plan from a dependency graph and observes cancellation.
    /// </summary>
    /// <param name="graph">The dependency graph to create a plan from.</param>
    /// <param name="cancellationToken">Token used to cancel plan construction.</param>
    /// <returns>An execution plan for parallel CTE execution.</returns>
    public static CteExecutionPlan CreatePlan(
        CteDependencyGraph graph,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();
        var levels = new List<CteExecutionLevel>();


        for (var i = 0; i < graph.ExecutionLevels.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ctesAtLevel = graph.ExecutionLevels[i];
            if (ctesAtLevel.Count > 0) levels.Add(new CteExecutionLevel(i, ctesAtLevel.ToList()));
        }

        cancellationToken.ThrowIfCancellationRequested();
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
        return CanBenefitFromParallelization(cteExpression, CancellationToken.None);
    }

    /// <summary>
    ///     Determines whether CTE parallelization would be beneficial and observes cancellation.
    /// </summary>
    /// <param name="cteExpression">The CTE expression to analyze.</param>
    /// <param name="cancellationToken">Token used to cancel CTE analysis.</param>
    /// <returns>True if parallelization is possible and beneficial.</returns>
    public static bool CanBenefitFromParallelization(
        CteExpressionNode cteExpression,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new CteDependencyGraphBuilder();
        var graph = builder.Build(cteExpression, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return graph.CanParallelize;
    }

    /// <summary>
    ///     Determines if parallelization would be beneficial based on an existing graph.
    /// </summary>
    /// <param name="graph">The dependency graph to check.</param>
    /// <returns>True if parallelization is possible and beneficial.</returns>
    public static bool CanBenefitFromParallelization(CteDependencyGraph graph)
    {
        return CanBenefitFromParallelization(graph, CancellationToken.None);
    }

    /// <summary>
    ///     Determines whether CTE parallelization would be beneficial and observes cancellation.
    /// </summary>
    /// <param name="graph">The dependency graph to check.</param>
    /// <param name="cancellationToken">Token used to cancel the check.</param>
    /// <returns>True if parallelization is possible and beneficial.</returns>
    public static bool CanBenefitFromParallelization(
        CteDependencyGraph graph,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();
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
        return GetExecutionLevelNames(cteExpression, CancellationToken.None);
    }

    /// <summary>
    ///     Gets CTE execution-level names and observes cancellation.
    /// </summary>
    /// <param name="cteExpression">The CTE expression to analyze.</param>
    /// <param name="cancellationToken">Token used to cancel level-name construction.</param>
    /// <returns>A list of CTE names at each execution level.</returns>
    public static IReadOnlyList<IReadOnlyList<string>> GetExecutionLevelNames(
        CteExpressionNode cteExpression,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var plan = CreatePlan(cteExpression, cancellationToken);
        var result = new List<IReadOnlyList<string>>(plan.Levels.Count);
        foreach (var level in plan.Levels)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var names = new List<string>(level.Ctes.Count);
            foreach (var cte in level.Ctes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                names.Add(cte.Name);
            }

            result.Add(names);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
