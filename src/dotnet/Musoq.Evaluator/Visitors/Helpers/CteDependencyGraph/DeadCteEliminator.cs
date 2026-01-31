using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Eliminates dead (unreferenced) CTEs from a CTE expression.
///     A CTE is considered dead if it is not reachable from the outer query.
/// </summary>
public static class DeadCteEliminator
{
    /// <summary>
    ///     Eliminates dead CTEs from the given CTE expression node.
    ///     Always enabled - dead CTE elimination cannot be turned off.
    /// </summary>
    /// <param name="cteExpression">The CTE expression to analyze and prune.</param>
    /// <returns>The elimination result containing the pruned node.</returns>
    public static EliminationResult Eliminate(CteExpressionNode cteExpression)
    {
        var builder = new CteDependencyGraphBuilder();
        var graph = builder.Build(cteExpression);


        if (!graph.HasDeadCtes)
            return new EliminationResult
            {
                ResultNode = cteExpression,
                WereCTEsEliminated = false,
                AllCTEsEliminated = false,
                EliminatedCount = 0,
                Graph = graph
            };


        var reachableCtes = cteExpression.InnerExpression
            .Where(inner => graph.Nodes[inner.Name].IsReachable)
            .ToArray();


        if (reachableCtes.Length == 0)
            return new EliminationResult
            {
                ResultNode = cteExpression.OuterExpression,
                WereCTEsEliminated = true,
                AllCTEsEliminated = true,
                EliminatedCount = graph.DeadCtes.Count,
                Graph = graph
            };


        var prunedCteExpression = new CteExpressionNode(reachableCtes, cteExpression.OuterExpression);

        return new EliminationResult
        {
            ResultNode = prunedCteExpression,
            WereCTEsEliminated = true,
            AllCTEsEliminated = false,
            EliminatedCount = graph.DeadCtes.Count,
            Graph = graph
        };
    }

    /// <summary>
    ///     Analyzes a CTE expression for dead CTEs without modifying it.
    /// </summary>
    /// <param name="cteExpression">The CTE expression to analyze.</param>
    /// <returns>The dependency graph with reachability information.</returns>
    public static CteDependencyGraph Analyze(CteExpressionNode cteExpression)
    {
        var builder = new CteDependencyGraphBuilder();
        return builder.Build(cteExpression);
    }

    /// <summary>
    ///     Result of dead CTE elimination.
    /// </summary>
    public readonly struct EliminationResult
    {
        /// <summary>
        ///     The resulting node after elimination.
        ///     If all CTEs were dead, this is the outer query node only.
        ///     Otherwise, it's a pruned CteExpressionNode.
        /// </summary>
        public Node ResultNode { get; init; }

        /// <summary>
        ///     Whether any CTEs were eliminated.
        /// </summary>
        public bool WereCTEsEliminated { get; init; }

        /// <summary>
        ///     Whether all CTEs were eliminated (result is just outer query).
        /// </summary>
        public bool AllCTEsEliminated { get; init; }

        /// <summary>
        ///     The number of CTEs that were eliminated.
        /// </summary>
        public int EliminatedCount { get; init; }

        /// <summary>
        ///     The dependency graph used for analysis.
        /// </summary>
        public CteDependencyGraph? Graph { get; init; }
    }
}
