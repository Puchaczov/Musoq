using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Builds a CTE dependency graph from a CteExpressionNode.
///     The graph represents dependencies between CTEs and the outer query,
///     enabling dead CTE elimination and parallelization analysis.
/// </summary>
public class CteDependencyGraphBuilder
{
    /// <summary>
    ///     Builds a CTE dependency graph from the given CTE expression node.
    /// </summary>
    /// <param name="cteExpression">The CTE expression node to analyze.</param>
    /// <returns>A dependency graph representing CTE relationships.</returns>
    public CteDependencyGraph Build(CteExpressionNode cteExpression)
    {
        return Build(cteExpression, CancellationToken.None);
    }

    /// <summary>
    ///     Builds a CTE dependency graph and cooperatively observes cancellation.
    /// </summary>
    /// <param name="cteExpression">The CTE expression node to analyze.</param>
    /// <param name="cancellationToken">Token used to cancel dependency analysis.</param>
    /// <returns>A dependency graph representing CTE relationships.</returns>
    public CteDependencyGraph Build(
        CteExpressionNode cteExpression,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cteExpression);
        cancellationToken.ThrowIfCancellationRequested();
        var cteNames = cteExpression.InnerExpression
            .Select(inner => inner.Name)
            .ToHashSet();


        var nodes = new Dictionary<string, CteGraphNode>();
        foreach (var inner in cteExpression.InnerExpression)
        {
            cancellationToken.ThrowIfCancellationRequested();
            nodes[inner.Name] = new CteGraphNode(inner.Name, inner);
        }


        var outerNode = new CteGraphNode(CteGraphNode.OuterQueryNodeName, null);


        foreach (var inner in cteExpression.InnerExpression)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var references = ExtractCteReferences(inner.Value, cteNames, cancellationToken);
            foreach (var dep in references)
            {
                cancellationToken.ThrowIfCancellationRequested();
                nodes[inner.Name].Dependencies.Add(dep);
                nodes[dep].Dependents.Add(inner.Name);
            }
        }


        var outerReferences = ExtractCteReferences(cteExpression.OuterExpression, cteNames, cancellationToken);
        foreach (var dep in outerReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            outerNode.Dependencies.Add(dep);
            nodes[dep].Dependents.Add(CteGraphNode.OuterQueryNodeName);
        }


        MarkReachableFromOuter(outerNode, nodes, cancellationToken);


        ComputeExecutionLevels(nodes, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        return new CteDependencyGraph(nodes, outerNode);
    }

    private static IReadOnlySet<string> ExtractCteReferences(
        Node queryNode,
        HashSet<string> cteNames,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var extractor = new CteReferenceExtractor(cteNames);
        var traverser = new CteReferenceExtractorTraverser(extractor);
        queryNode.Accept(traverser);
        cancellationToken.ThrowIfCancellationRequested();
        return extractor.FoundReferences;
    }

    private static void MarkReachableFromOuter(
        CteGraphNode outerNode,
        Dictionary<string, CteGraphNode> nodes,
        CancellationToken cancellationToken)
    {
        var stack = new Stack<string>(outerNode.Dependencies);

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cteName = stack.Pop();
            if (!nodes.TryGetValue(cteName, out var node) || node.IsReachable)
                continue;

            node.IsReachable = true;


            foreach (var dep in node.Dependencies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                stack.Push(dep);
            }
        }
    }

    private static void ComputeExecutionLevels(
        Dictionary<string, CteGraphNode> nodes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reachableNodes = new List<CteGraphNode>();
        foreach (var node in nodes.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node.IsReachable)
                reachableNodes.Add(node);
        }

        if (reachableNodes.Count == 0)
            return;


        var inDegree = new Dictionary<string, int>();
        foreach (var node in reachableNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reachableDeps =
                node.Dependencies.Count(d =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return nodes.TryGetValue(d, out var depNode) && depNode.IsReachable;
                });
            inDegree[node.Name] = reachableDeps;
        }


        var queue = new Queue<string>();


        foreach (var (name, degree) in inDegree)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (degree == 0)
            {
                nodes[name].ExecutionLevel = 0;
                queue.Enqueue(name);
            }
        }


        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = queue.Dequeue();
            var currentNode = nodes[current];
            var currentLevel = currentNode.ExecutionLevel;

            foreach (var dependent in currentNode.Dependents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dependent == CteGraphNode.OuterQueryNodeName || !nodes.TryGetValue(dependent, out var depNode))
                    continue;


                if (!depNode.IsReachable)
                    continue;

                inDegree[dependent]--;


                depNode.ExecutionLevel = depNode.ExecutionLevel < 0
                    ? currentLevel + 1
                    : Math.Max(depNode.ExecutionLevel, currentLevel + 1);

                if (inDegree[dependent] == 0) queue.Enqueue(dependent);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
