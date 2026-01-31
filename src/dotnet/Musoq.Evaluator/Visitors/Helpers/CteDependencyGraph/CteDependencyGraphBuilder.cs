using System;
using System.Collections.Generic;
using System.Linq;
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
        var cteNames = cteExpression.InnerExpression
            .Select(inner => inner.Name)
            .ToHashSet();


        var nodes = new Dictionary<string, CteGraphNode>();
        foreach (var inner in cteExpression.InnerExpression) nodes[inner.Name] = new CteGraphNode(inner.Name, inner);


        var outerNode = new CteGraphNode(CteGraphNode.OuterQueryNodeName, null);


        foreach (var inner in cteExpression.InnerExpression)
        {
            var references = ExtractCteReferences(inner.Value, cteNames);
            foreach (var dep in references)
            {
                nodes[inner.Name].Dependencies.Add(dep);
                nodes[dep].Dependents.Add(inner.Name);
            }
        }


        var outerReferences = ExtractCteReferences(cteExpression.OuterExpression, cteNames);
        foreach (var dep in outerReferences)
        {
            outerNode.Dependencies.Add(dep);
            nodes[dep].Dependents.Add(CteGraphNode.OuterQueryNodeName);
        }


        MarkReachableFromOuter(outerNode, nodes);


        ComputeExecutionLevels(nodes);

        return new CteDependencyGraph(nodes, outerNode);
    }

    private static IReadOnlySet<string> ExtractCteReferences(Node queryNode, HashSet<string> cteNames)
    {
        var extractor = new CteReferenceExtractor(cteNames);
        var traverser = new CteReferenceExtractorTraverser(extractor);
        queryNode.Accept(traverser);
        return extractor.FoundReferences;
    }

    private static void MarkReachableFromOuter(CteGraphNode outerNode, Dictionary<string, CteGraphNode> nodes)
    {
        var stack = new Stack<string>(outerNode.Dependencies);

        while (stack.Count > 0)
        {
            var cteName = stack.Pop();
            if (!nodes.TryGetValue(cteName, out var node) || node.IsReachable)
                continue;

            node.IsReachable = true;


            foreach (var dep in node.Dependencies) stack.Push(dep);
        }
    }

    private static void ComputeExecutionLevels(Dictionary<string, CteGraphNode> nodes)
    {
        var reachableNodes = nodes.Values.Where(n => n.IsReachable).ToList();

        if (reachableNodes.Count == 0)
            return;


        var inDegree = new Dictionary<string, int>();
        foreach (var node in reachableNodes)
        {
            var reachableDeps =
                node.Dependencies.Count(d => nodes.TryGetValue(d, out var depNode) && depNode.IsReachable);
            inDegree[node.Name] = reachableDeps;
        }


        var queue = new Queue<string>();


        foreach (var (name, degree) in inDegree)
            if (degree == 0)
            {
                nodes[name].ExecutionLevel = 0;
                queue.Enqueue(name);
            }


        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentNode = nodes[current];
            var currentLevel = currentNode.ExecutionLevel;

            foreach (var dependent in currentNode.Dependents)
            {
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
    }
}
