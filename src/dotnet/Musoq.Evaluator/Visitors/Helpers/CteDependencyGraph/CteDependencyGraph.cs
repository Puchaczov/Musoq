using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Represents the complete CTE dependency graph for a query.
///     Provides information about CTE dependencies, reachability (for dead code elimination),
///     and execution levels (for parallelization).
/// </summary>
public class CteDependencyGraph
{
    private readonly List<CteGraphNode> _deadCtes;
    private readonly List<IReadOnlyList<CteGraphNode>> _executionLevels;
    private readonly Dictionary<string, CteGraphNode> _nodes;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CteDependencyGraph" /> class.
    /// </summary>
    /// <param name="nodes">Dictionary of all CTE nodes indexed by name.</param>
    /// <param name="outerQuery">The outer query pseudo-node.</param>
    public CteDependencyGraph(Dictionary<string, CteGraphNode> nodes, CteGraphNode outerQuery)
    {
        _nodes = nodes;
        OuterQuery = outerQuery;

        _deadCtes = nodes.Values
            .Where(n => !n.IsReachable && !n.IsOuterQuery)
            .ToList();

        _executionLevels = ComputeExecutionLevels(nodes);
    }

    /// <summary>
    ///     Gets all CTE nodes indexed by name.
    /// </summary>
    public IReadOnlyDictionary<string, CteGraphNode> Nodes => _nodes;

    /// <summary>
    ///     Gets the outer query pseudo-node.
    /// </summary>
    public CteGraphNode OuterQuery { get; }

    /// <summary>
    ///     Gets CTEs that are not reachable from the outer query (dead code).
    /// </summary>
    public IReadOnlyList<CteGraphNode> DeadCtes => _deadCtes;

    /// <summary>
    ///     Gets CTEs that are reachable from the outer query.
    /// </summary>
    public IReadOnlyList<CteGraphNode> ReachableCtes => _nodes.Values
        .Where(n => n.IsReachable && !n.IsOuterQuery)
        .ToList();

    /// <summary>
    ///     Gets CTEs grouped by execution level for parallelization.
    ///     Level 0 contains CTEs with no dependencies on other CTEs.
    ///     Only includes reachable CTEs.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<CteGraphNode>> ExecutionLevels => _executionLevels;

    /// <summary>
    ///     Gets a value indicating whether any dead CTEs exist.
    /// </summary>
    public bool HasDeadCtes => _deadCtes.Count > 0;

    /// <summary>
    ///     Gets a value indicating whether parallelization is possible.
    ///     True if any execution level contains more than one CTE.
    /// </summary>
    public bool CanParallelize => _executionLevels.Any(level => level.Count > 1);

    /// <summary>
    ///     Gets the total number of CTE nodes (excluding outer query).
    /// </summary>
    public int CteCount => _nodes.Count;

    /// <summary>
    ///     Checks if a CTE with the given name exists in the graph.
    /// </summary>
    /// <param name="name">The CTE name to check.</param>
    /// <returns>True if the CTE exists, false otherwise.</returns>
    public bool ContainsCte(string name)
    {
        return _nodes.ContainsKey(name);
    }

    /// <summary>
    ///     Gets the CTE node with the given name.
    /// </summary>
    /// <param name="name">The CTE name.</param>
    /// <returns>The CTE node if found.</returns>
    /// <exception cref="KeyNotFoundException">If the CTE does not exist.</exception>
    public CteGraphNode GetCte(string name)
    {
        return _nodes[name];
    }

    /// <summary>
    ///     Tries to get the CTE node with the given name.
    /// </summary>
    /// <param name="name">The CTE name.</param>
    /// <param name="node">The CTE node if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    public bool TryGetCte(string name, out CteGraphNode? node)
    {
        return _nodes.TryGetValue(name, out node);
    }

    private static List<IReadOnlyList<CteGraphNode>> ComputeExecutionLevels(Dictionary<string, CteGraphNode> nodes)
    {
        var levels = new List<IReadOnlyList<CteGraphNode>>();


        var reachableNodes = nodes.Values
            .Where(n => n.IsReachable && !n.IsOuterQuery && n.ExecutionLevel >= 0)
            .GroupBy(n => n.ExecutionLevel)
            .OrderBy(g => g.Key);

        foreach (var group in reachableNodes) levels.Add(group.ToList());

        return levels;
    }

    /// <summary>
    ///     Returns a string representation of the dependency graph.
    /// </summary>
    public override string ToString()
    {
        var lines = new List<string>
        {
            $"CteDependencyGraph: {CteCount} CTEs, {DeadCtes.Count} dead, CanParallelize={CanParallelize}"
        };

        foreach (var node in _nodes.Values) lines.Add($"  {node}");

        lines.Add($"  {OuterQuery}");

        return string.Join("\n", lines);
    }
}
