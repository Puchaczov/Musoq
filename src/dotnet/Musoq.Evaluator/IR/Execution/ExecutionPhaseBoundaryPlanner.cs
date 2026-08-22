using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionPhaseBoundaryPlanner
{
    public static ExecutionPlan AddRootBoundaries(PhysicalNode physicalPlan, ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(plan);

        plan = AddCteScopeBoundaries(physicalPlan, plan);
        var rootPhysicalPlan = physicalPlan is PhysicalCteNode cte
            ? cte.Query
            : physicalPlan;
        var body = InsertClauseBoundaries(
            rootPhysicalPlan,
            plan.Body,
            string.Empty,
            includeBegin: true,
            searchDescendants: false,
            finalTableName: plan.FinalResult?.TableName);
        return plan with { Body = body };
    }

    internal static ExecutionPlan RepositionRootBoundaries(PhysicalNode physicalPlan, ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(plan);

        var rootNodes = plan.Body.Nodes
            .Where(static node => node is not ExecutionPhaseBoundary { QueryIdSuffix: "" })
            .ToArray();
        var rootPhysicalPlan = physicalPlan is PhysicalCteNode cte
            ? cte.Query
            : physicalPlan;
        var body = InsertClauseBoundaries(
            rootPhysicalPlan,
            new ExecutionBlock(rootNodes),
            string.Empty,
            includeBegin: true,
            searchDescendants: false,
            finalTableName: plan.FinalResult?.TableName);
        return plan with { Body = body };
    }

    internal static IReadOnlyList<ExecutionNode> AddScopeClauseBoundaries(
        PhysicalNode physicalPlan,
        IReadOnlyList<ExecutionNode> nodes,
        string queryIdSuffix)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(queryIdSuffix);

        return InsertClauseBoundaries(
                physicalPlan,
                new ExecutionBlock(nodes),
                queryIdSuffix,
                includeBegin: false,
                searchDescendants: true,
                finalTableName: null)
            .Nodes;
    }

    internal static IReadOnlyList<ExecutionNode> AddCteScopeBoundaries(
        PhysicalNode physicalPlan,
        IReadOnlyList<ExecutionNode> nodes,
        string queryIdSuffix)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(queryIdSuffix);

        var body = AddScopeClauseBoundaries(physicalPlan, nodes, queryIdSuffix);
        return
        [
            new ExecutionPhaseBoundary(QueryPhase.Begin, queryIdSuffix),
            ..body,
            new ExecutionPhaseBoundary(QueryPhase.End, queryIdSuffix)
        ];
    }

    private static ExecutionPlan AddCteScopeBoundaries(PhysicalNode physicalPlan, ExecutionPlan plan)
    {
        if (physicalPlan is not PhysicalCteNode cte)
            return plan;

        var body = plan.Body;
        var additions = new Dictionary<int, List<ExecutionNode>>();
        var previousStoreIndex = -1;
        for (var index = 0; index < body.Nodes.Count; index++)
        {
            if (body.Nodes[index] is not ExecutionStoreTable store)
                continue;

            var suffix = CreateCteSuffix(store.TableIndex);
            if (!ContainsPhaseBoundarySuffix(body, suffix, QueryPhase.Begin))
                AddNode(additions, previousStoreIndex + 1, new ExecutionPhaseBoundary(QueryPhase.Begin, suffix));
            if (!ContainsPhaseBoundarySuffix(body, suffix, QueryPhase.End))
                AddNode(additions, index + 1, new ExecutionPhaseBoundary(QueryPhase.End, suffix));

            previousStoreIndex = index;
        }

        var cteIndexes = cte.Definitions
            .Select((definition, index) => (definition.Name, Index: index))
            .ToDictionary(static item => item.Name, static item => item.Index, StringComparer.OrdinalIgnoreCase);
        var rewrittenNodes = body.Nodes
            .Select(node => AddParallelCteTaskBoundaries(node, cteIndexes))
            .ToArray();

        if (additions.Count == 0 && rewrittenNodes.SequenceEqual(body.Nodes))
            return plan;

        var result = new List<ExecutionNode>(rewrittenNodes.Length + additions.Values.Sum(static nodes => nodes.Count));
        for (var index = 0; index <= rewrittenNodes.Length; index++)
        {
            if (additions.TryGetValue(index, out var nodes))
                result.AddRange(nodes);
            if (index < rewrittenNodes.Length)
                result.Add(rewrittenNodes[index]);
        }

        return plan with { Body = new ExecutionBlock(result) };
    }

    private static ExecutionBlock InsertClauseBoundaries(
        PhysicalNode physicalPlan,
        ExecutionBlock body,
        string queryIdSuffix,
        bool includeBegin,
        bool searchDescendants,
        string? finalTableName)
    {
        var boundariesByIndex = new Dictionary<int, List<ExecutionNode>>();
        if (includeBegin)
            AddNode(boundariesByIndex, 0, new ExecutionPhaseBoundary(QueryPhase.Begin, queryIdSuffix));

        var selectIndex = FindSelectIndex(body, finalTableName, searchDescendants);
        var sourceIndex = -1;
        if (!searchDescendants && ContainsPhysicalCteReference(physicalPlan))
        {
            sourceIndex = FindRootSourceIndex(body, selectIndex);
        }
        else
        {
            sourceIndex = FindTopLevelIndex(body, IsSourceOperation, searchDescendants);
        }

        if (sourceIndex >= 0)
            AddNode(boundariesByIndex, sourceIndex, new ExecutionPhaseBoundary(QueryPhase.From, queryIdSuffix));

        var nextClauseIndex = sourceIndex >= 0 ? sourceIndex + 1 : 0;
        if (ContainsSqlWhere(physicalPlan))
        {
            var whereIndex = FindTopLevelIndex(
                body,
                IsRowProcessingOperation,
                searchDescendants);
            if (!searchDescendants && ContainsPhysicalCteReference(physicalPlan))
            {
                whereIndex = FindTopLevelIndexFrom(body, IsRowProcessingOperation, sourceIndex);
                whereIndex = whereIndex >= 0 ? FindEnclosingScopeStart(body, whereIndex) : selectIndex;
            }

            AddNode(
                boundariesByIndex,
                Math.Max(nextClauseIndex, whereIndex),
                new ExecutionPhaseBoundary(QueryPhase.Where, queryIdSuffix));
        }

        if (ContainsExplicitGroupBy(physicalPlan))
        {
            var groupingIndex = FindTopLevelIndex(
                body,
                IsGroupingOperation,
                searchDescendants);
            if (!searchDescendants && ContainsPhysicalCteReference(physicalPlan))
            {
                groupingIndex = FindTopLevelIndexFrom(body, IsGroupingOperation, sourceIndex);
                groupingIndex = groupingIndex >= 0 ? FindEnclosingScopeStart(body, groupingIndex) : selectIndex;
            }

            AddNode(
                boundariesByIndex,
                Math.Max(nextClauseIndex, groupingIndex),
                new ExecutionPhaseBoundary(QueryPhase.GroupBy, queryIdSuffix));
        }

        AddNode(
            boundariesByIndex,
            Math.Max(nextClauseIndex, !searchDescendants && ContainsPhysicalCteReference(physicalPlan)
                ? FindEnclosingScopeStart(body, selectIndex)
                : selectIndex),
            new ExecutionPhaseBoundary(QueryPhase.Select, queryIdSuffix));

        var nodes = new List<ExecutionNode>(
            body.Nodes.Count + boundariesByIndex.Values.Sum(static phaseNodes => phaseNodes.Count));
        for (var index = 0; index < body.Nodes.Count; index++)
        {
            if (boundariesByIndex.TryGetValue(index, out var phaseNodes))
                nodes.AddRange(phaseNodes);

            nodes.Add(body.Nodes[index]);
        }

        if (boundariesByIndex.TryGetValue(body.Nodes.Count, out var trailingPhaseNodes))
            nodes.AddRange(trailingPhaseNodes);

        return new ExecutionBlock(nodes);
    }

    private static int FindTopLevelIndexFrom(
        ExecutionBlock block,
        Func<ExecutionNode, bool> predicate,
        int startIndex)
    {
        startIndex = Math.Clamp(startIndex, 0, block.Nodes.Count);
        for (var index = startIndex; index < block.Nodes.Count; index++)
        {
            if (predicate(block.Nodes[index]))
                return index;
        }

        return -1;
    }

    private static int FindRootSourceIndex(ExecutionBlock block, int selectIndex)
    {
        var scopeDepth = 0;
        var firstScopedOperation = -1;
        var end = Math.Clamp(selectIndex, 0, block.Nodes.Count);

        for (var index = 0; index < block.Nodes.Count; index++)
        {
            var node = block.Nodes[index];
            if (node is ExecutionPhaseBoundary boundary &&
                boundary.QueryIdSuffix is { Length: > 0 } suffix &&
                suffix is not ":left" and not ":right")
            {
                if (boundary.Phase == QueryPhase.Begin)
                {
                    if (firstScopedOperation < 0)
                        firstScopedOperation = index;

                    scopeDepth++;
                }
                else if (boundary.Phase == QueryPhase.End)
                {
                    scopeDepth = Math.Max(0, scopeDepth - 1);
                }

                continue;
            }

            if (scopeDepth == 0 && IsSourceOperation(node))
                return index;

            if (scopeDepth == 0 && firstScopedOperation < 0 && IsRootSourceEnclosingOperation(node))
                firstScopedOperation = index;

            if (index >= end && firstScopedOperation >= 0)
                break;
        }

        return firstScopedOperation >= 0
            ? firstScopedOperation
            : Math.Clamp(selectIndex, 0, block.Nodes.Count);
    }

    private static int FindEnclosingScopeStart(ExecutionBlock block, int index)
    {
        var scopes = new List<(string Suffix, int Start)>();
        var end = Math.Clamp(index, 0, block.Nodes.Count);
        for (var current = 0; current < end; current++)
        {
            if (block.Nodes[current] is not ExecutionPhaseBoundary boundary ||
                boundary.QueryIdSuffix is not { Length: > 0 } suffix ||
                suffix is ":left" or ":right")
                continue;

            if (boundary.Phase == QueryPhase.Begin)
            {
                scopes.Add((suffix, current));
            }
            else if (boundary.Phase == QueryPhase.End)
            {
                for (var scopeIndex = scopes.Count - 1; scopeIndex >= 0; scopeIndex--)
                {
                    if (!string.Equals(scopes[scopeIndex].Suffix, suffix, StringComparison.Ordinal))
                        continue;

                    scopes.RemoveAt(scopeIndex);
                    break;
                }
            }
        }

        return scopes.Count == 0 ? index : scopes[^1].Start;
    }

    private static ExecutionNode AddParallelCteTaskBoundaries(
        ExecutionNode node,
        IReadOnlyDictionary<string, int> cteIndexes)
    {
        if (node is not ExecutionParallelBlock parallel)
            return node;

        var changed = false;
        var tasks = new ExecutionParallelTask[parallel.Tasks.Count];
        for (var index = 0; index < parallel.Tasks.Count; index++)
        {
            var task = parallel.Tasks[index];
            if (!cteIndexes.TryGetValue(task.Name, out var cteIndex))
            {
                tasks[index] = task;
                continue;
            }

            var suffix = CreateCteSuffix(cteIndex);
            if (ContainsPhaseBoundarySuffix(task.Body, suffix, QueryPhase.Begin) &&
                ContainsPhaseBoundarySuffix(task.Body, suffix, QueryPhase.End))
            {
                tasks[index] = task;
                continue;
            }

            var nodes = new List<ExecutionNode>(task.Body.Nodes.Count + 2)
            {
                new ExecutionPhaseBoundary(QueryPhase.Begin, suffix)
            };
            nodes.AddRange(task.Body.Nodes);
            nodes.Add(new ExecutionPhaseBoundary(QueryPhase.End, suffix));
            tasks[index] = task with { Body = new ExecutionBlock(nodes) };
            changed = true;
        }

        return changed
            ? parallel with { Tasks = tasks }
            : parallel;
    }

    private static void AddNode(
        IDictionary<int, List<ExecutionNode>> additions,
        int index,
        ExecutionNode node)
    {
        if (!additions.TryGetValue(index, out var nodes))
        {
            nodes = [];
            additions[index] = nodes;
        }

        nodes.Add(node);
    }

    private static bool ContainsPhaseBoundarySuffix(
        ExecutionBlock block,
        string suffix,
        QueryPhase phase)
    {
        foreach (var node in block.Nodes)
        {
            if (node is ExecutionPhaseBoundary boundary &&
                boundary.Phase == phase &&
                string.Equals(boundary.QueryIdSuffix, suffix, StringComparison.Ordinal))
                return true;

            if (GetChildBlocks(node).Any(child => ContainsPhaseBoundarySuffix(child, suffix, phase)))
                return true;
        }

        return false;
    }

    internal static string CreateCteSuffix(int tableIndex)
    {
        return $":cte{tableIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }

    private static bool ContainsSqlWhere(PhysicalNode node)
    {
        return node switch
        {
            PhysicalFilterNode => true,
            PhysicalSchemaScanNode scan when scan.PushedPredicates.Length > 0 => true,
            _ => node.Children.Any(ContainsSqlWhere)
        };
    }

    private static bool ContainsExplicitGroupBy(PhysicalNode node)
    {
        // The semantic layer represents an aggregate without GROUP BY as one
        // synthetic constant-key group named "1". Explicit ordinal grouping is
        // normalized to the selected field name before this stage, so only a
        // real grouped aggregate receives the user-visible marker.
        return node is PhysicalSingleKeyAggregateNode { GroupKeyName: not "1" } or
               PhysicalValueTupleAggregateNode ||
               node.Children.Any(ContainsExplicitGroupBy);
    }

    private static bool IsSourceOperation(ExecutionNode node)
    {
        return node is ExecutionSourceScan or
            ExecutionCreateValuesRows or
            ExecutionInterpretSource or
            ExecutionEnumerableSource or
            ExecutionSourceLoop { Source: ExecutionStoredTableRows };
    }

    private static bool IsRowProcessingOperation(ExecutionNode node)
    {
        return node is ExecutionSourceLoop or
            ExecutionForEach or
            ExecutionForEachWithOrdinality or
            ExecutionForEachIndexed or
            ExecutionParallelFilterProjectLoop or
            ExecutionParallelSingleKeyAggregateLoop;
    }

    private static bool IsGroupingOperation(ExecutionNode node)
    {
        return node is ExecutionCreateAggregateContext or
            ExecutionCreateSingleKeyAggregateContext or
            ExecutionCreateValueTupleAggregateContext or
            ExecutionGetOrAddSingleKeyAggregateGroup or
            ExecutionGetOrAddValueTupleAggregateGroup or
            ExecutionEnsureAggregateGroup or
            ExecutionParallelSingleKeyAggregateLoop;
    }

    private static int FindSelectIndex(
        ExecutionBlock block,
        string? finalTableName,
        bool searchDescendants)
    {
        if (!string.IsNullOrWhiteSpace(finalTableName))
        {
            var targetIndex = searchDescendants
                ? FindTopLevelIndex(block, node => ContainsFinalOutput(node, finalTableName), searchDescendants)
                : FindFinalOutputEnclosingIndex(block, finalTableName);
            if (targetIndex >= 0)
                return targetIndex;
        }

        var outputIndex = FindTopLevelIndex(
            block,
            searchDescendants ? ContainsAnyOutput : IsRootOutputOperation,
            searchDescendants);
        if (outputIndex >= 0)
            return outputIndex;

        var setOperationIndex = FindTopLevelIndex(
            block,
            static node => node is ExecutionSetOperation,
            searchDescendants);
        if (setOperationIndex >= 0)
            return setOperationIndex;

        var projectIndex = FindTopLevelIndex(
            block,
            static node => node is ExecutionProjectTable,
            searchDescendants);
        return projectIndex >= 0 ? projectIndex : block.Nodes.Count;
    }

    private static int FindFinalOutputEnclosingIndex(ExecutionBlock block, string tableName)
    {
        for (var index = 0; index < block.Nodes.Count; index++)
        {
            var node = block.Nodes[index];
            if (node is ExecutionPhaseBoundary
                {
                    Phase: QueryPhase.Begin,
                    QueryIdSuffix: { Length: > 0 } suffix
                } &&
                suffix is not ":left" and not ":right" &&
                TryFindMatchingPhaseEnd(block.Nodes, index, suffix, out var endIndex))
            {
                var scopedBlock = new ExecutionBlock(
                    block.Nodes.Skip(index + 1).Take(endIndex - index - 1).ToArray());
                if (scopedBlock.Nodes.Any(node => ContainsFinalOutput(node, tableName)))
                    return index;

                index = endIndex;
                continue;
            }

            if (ContainsFinalOutput(node, tableName))
                return index;
        }

        return -1;
    }

    private static bool TryFindMatchingPhaseEnd(
        IReadOnlyList<ExecutionNode> nodes,
        int beginIndex,
        string suffix,
        out int endIndex)
    {
        for (var index = beginIndex + 1; index < nodes.Count; index++)
        {
            if (nodes[index] is ExecutionPhaseBoundary
                {
                    Phase: QueryPhase.End,
                    QueryIdSuffix: var candidateSuffix
                } && string.Equals(candidateSuffix, suffix, StringComparison.Ordinal))
            {
                endIndex = index;
                return true;
            }
        }

        endIndex = -1;
        return false;
    }

    private static bool ContainsFinalOutput(ExecutionNode node, string tableName)
    {
        return node switch
        {
            ExecutionAppendRow append when string.Equals(append.Table.Name, tableName, StringComparison.Ordinal) => true,
            ExecutionAppendExistingRow append when string.Equals(append.Table.Name, tableName, StringComparison.Ordinal) => true,
            _ => GetChildBlocks(node).Any(block => block.Nodes.Any(child => ContainsFinalOutput(child, tableName)))
        };
    }

    private static bool ContainsAnyOutput(ExecutionNode node)
    {
        return node is ExecutionAppendRow or ExecutionAppendExistingRow or ExecutionAppendRecord or ExecutionRecursiveCteAppend ||
               GetChildBlocks(node).Any(block => block.Nodes.Any(ContainsAnyOutput));
    }

    private static bool IsRootOutputOperation(ExecutionNode node)
    {
        return node is ExecutionSourceLoop or
            ExecutionParallelBlock or
            ExecutionSetOperation or
            ExecutionProjectTable or
            ExecutionAppendRow or
            ExecutionAppendExistingRow or
            ExecutionAppendRecord or
            ExecutionRecursiveCteAppend;
    }

    private static bool IsRootSourceEnclosingOperation(ExecutionNode node)
    {
        return node is ExecutionParallelBlock or
            ExecutionFusedCteProducer or
            ExecutionCteReadOnceFusionCandidate or
            ExecutionSingleUsePipelineFusionCandidate or
            ExecutionCteFusedProducerCandidate;
    }

    private static bool ContainsPhysicalCteReference(PhysicalNode node)
    {
        return node is PhysicalCteRefNode || node.Children.Any(ContainsPhysicalCteReference);
    }

    private static int FindTopLevelIndex(
        ExecutionBlock block,
        Func<ExecutionNode, bool> predicate,
        bool searchDescendants)
    {
        for (var index = 0; index < block.Nodes.Count; index++)
        {
            if (predicate(block.Nodes[index]) ||
                (searchDescendants && ContainsNode(block.Nodes[index], predicate)))
                return index;
        }

        return -1;
    }

    private static bool ContainsNode(ExecutionNode node, Func<ExecutionNode, bool> predicate)
    {
        foreach (var childBlock in GetChildBlocks(node))
        {
            foreach (var child in childBlock.Nodes)
            {
                if (predicate(child) || ContainsNode(child, predicate))
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<ExecutionBlock> GetChildBlocks(ExecutionNode node)
    {
        return ExecutionNodeRegistry.GetChildBlocks(node);
    }
}
