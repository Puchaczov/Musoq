#nullable enable annotations

using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Represents a node in the CTE dependency graph.
///     Can represent either a CTE definition or the outer query.
/// </summary>
public class CteGraphNode
{
    /// <summary>
    ///     The name used to identify the outer query pseudo-node.
    /// </summary>
    public const string OuterQueryNodeName = "__OUTER__";

    /// <summary>
    ///     Initializes a new instance of the <see cref="CteGraphNode" /> class.
    /// </summary>
    /// <param name="name">The name of the CTE, or "__OUTER__" for the outer query.</param>
    /// <param name="astNode">The AST node for this CTE (null for outer query pseudo-node).</param>
    public CteGraphNode(string name, CteInnerExpressionNode? astNode)
    {
        Name = name;
        AstNode = astNode;
    }

    /// <summary>
    ///     Gets the name of the CTE, or "__OUTER__" for the outer query.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the AST node for this CTE (null for outer query pseudo-node).
    /// </summary>
    public CteInnerExpressionNode? AstNode { get; }

    /// <summary>
    ///     Gets the names of CTEs that this node depends on (references in FROM clauses).
    /// </summary>
    public HashSet<string> Dependencies { get; } = new();

    /// <summary>
    ///     Gets the names of CTEs that depend on this node (reverse edges).
    /// </summary>
    public HashSet<string> Dependents { get; } = new();

    /// <summary>
    ///     Gets or sets a value indicating whether this CTE is reachable from the outer query.
    ///     Used for dead code elimination.
    /// </summary>
    public bool IsReachable { get; set; }

    /// <summary>
    ///     Gets or sets the execution level for parallelization.
    ///     Level 0 = no dependencies on other CTEs, can run first.
    ///     Higher levels must wait for lower levels to complete.
    ///     -1 indicates level has not been computed.
    /// </summary>
    public int ExecutionLevel { get; set; } = -1;

    /// <summary>
    ///     Gets a value indicating whether this is the outer query pseudo-node.
    /// </summary>
    public bool IsOuterQuery => Name == OuterQueryNodeName;

    /// <summary>
    ///     Gets a value indicating whether this CTE has any dependencies on other CTEs.
    /// </summary>
    public bool HasDependencies => Dependencies.Count > 0;

    /// <summary>
    ///     Gets a value indicating whether this CTE has any dependents (other CTEs or outer query that depend on it).
    /// </summary>
    public bool HasDependents => Dependents.Count > 0;

    /// <summary>
    ///     Returns a string representation of this node.
    /// </summary>
    public override string ToString()
    {
        var status = IsReachable ? "reachable" : "dead";
        var level = ExecutionLevel >= 0 ? $"level {ExecutionLevel}" : "level unknown";
        return $"CteGraphNode({Name}, {status}, {level}, deps=[{string.Join(", ", Dependencies)}])";
    }
}
