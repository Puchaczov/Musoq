using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record FusedSiblingCteBuildResult(
        int DefinitionCount,
        IReadOnlyList<RowShape> Shapes,
        ExecutionFusedCteProducer Producer,
        IReadOnlyDictionary<string, GeneratedRowShape> RowShapesByName);

    private sealed record FusedSiblingCteCandidate(
        string DefinitionName,
        int TableIndex,
        TableBuildResult Result,
        CteSidecarStorageDecision Storage,
        IReadOnlyList<ExecutionNode> SetupNodes,
        ExecutionForEach Loop,
        IReadOnlyList<ExecutionNode> StoreIndexNodes);

    private FusedSiblingCteBuildResult? TryBuildFusedSiblingCteProducers(
        PhysicalCteNode cte,
        int startIndex,
        int exclusiveEndIndex,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        IReadOnlyDictionary<string, int> schemaFromIndexes,
        CteDefinitionPruningPlan pruningPlan,
        IReadOnlyDictionary<string, CteReferenceClassification> cteReferenceClassifications,
        IReadOnlyDictionary<string, FusedCteHashBuildSource> fusedHashBuildSources)
    {
        if (!_compilationOptions.UseCteSidecarIndexes ||
            !TryGetSimpleSiblingSourceCte(cte.Definitions[startIndex], out var sourceCteName) ||
            !cteIndexes.TryGetValue(sourceCteName, out var sourceTableIndex) ||
            !cteShapesByName.ContainsKey(sourceCteName))
        {
            return null;
        }

        var candidates = new List<FusedSiblingCteCandidate>();
        var shapes = new List<RowShape>();
        var rowShapesByName = new Dictionary<string, GeneratedRowShape>(StringComparer.OrdinalIgnoreCase);

        for (var index = startIndex; index < exclusiveEndIndex; index++)
        {
            var definition = cte.Definitions[index];
            if (fusedHashBuildSources.ContainsKey(definition.Name) ||
                !TryGetSimpleSiblingSourceCte(definition, out var currentSourceCteName) ||
                !string.Equals(sourceCteName, currentSourceCteName, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var sidecarSpecs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
            if (sidecarSpecs.Count == 0)
                break;

            var result = BuildCteDefinitionTable(
                definition,
                index,
                cteDefinitionNames,
                cteIndexes,
                cteShapesByName,
                schemaFromIndexes[definition.Name],
                pruningPlan);
            result = ApplyCteSidecarOptimizations(
                definition.Name,
                sidecarSpecs,
                cteReferenceClassifications,
                pruningPlan,
                result,
                out var storage);

            if (!result.Supported ||
                ContainsSideEffectSensitiveSiblingExpression(result.Nodes) ||
                !TryExtractFusibleSiblingBuild(result, sourceTableIndex, out var setupNodes, out var loop, out var storeIndexNodes))
            {
                break;
            }

            shapes.AddRange(result.Shapes);
            rowShapesByName[definition.Name] = result.RowShape;
            candidates.Add(new FusedSiblingCteCandidate(
                definition.Name,
                index,
                result,
                storage,
                setupNodes,
                loop,
                storeIndexNodes));
        }

        return candidates.Count < 2
            ? null
            : CreateFusedSiblingCteBuildResult(candidates, shapes, rowShapesByName);
    }

    private static bool TryGetSimpleSiblingSourceCte(
        PhysicalCteDefinition definition,
        out string sourceCteName)
    {
        sourceCteName = string.Empty;
        var plan = UnwrapSingleStatement(definition.Plan);
        if (plan is not PhysicalProjectNode { IsDistinct: false } project)
            return false;

        var source = project.Input is PhysicalFilterNode filter
            ? filter.Input
            : project.Input;
        if (source is not PhysicalCteRefNode cteRef)
            return false;

        sourceCteName = cteRef.CteName;
        return true;
    }

    private static bool ContainsSideEffectSensitiveSiblingExpression(IReadOnlyList<ExecutionNode> nodes)
    {
        var block = new ExecutionBlock(nodes);
        return ExecutionIrAnalysis.CollectExpressions<ExecutionMethodCall>(block).Any() ||
               ExecutionIrAnalysis.CollectExpressions<ExecutionRawExpression>(block).Any();
    }

    private static bool TryExtractFusibleSiblingBuild(
        TableBuildResult result,
        int sourceTableIndex,
        out IReadOnlyList<ExecutionNode> setupNodes,
        out ExecutionForEach loop,
        out IReadOnlyList<ExecutionNode> storeIndexNodes)
    {
        setupNodes = [];
        loop = null!;
        storeIndexNodes = [];

        var nodes = result.Nodes.ToArray();
        var loopIndex = Array.FindIndex(nodes, static node => node is ExecutionForEach);
        if (loopIndex < 0 ||
            Array.FindIndex(nodes, loopIndex + 1, static node => node is ExecutionForEach) >= 0 ||
            nodes[loopIndex] is not ExecutionForEach currentLoop ||
            currentLoop.Source is not ExecutionStoredTableRows { TableIndex: var tableIndex } ||
            tableIndex != sourceTableIndex ||
            ContainsNestedSiblingLoop(currentLoop.Body))
        {
            return false;
        }

        var setup = nodes[..loopIndex];
        if (setup.Any(static node => !IsFusibleSiblingSetupNode(node)))
            return false;

        var stores = nodes[(loopIndex + 1)..];
        if (stores.Length == 0 ||
            stores.Any(static node => node is not ExecutionCteSidecarIndexStoreCandidate))
        {
            return false;
        }

        setupNodes = setup;
        loop = currentLoop;
        storeIndexNodes = stores;
        return true;
    }

    private static bool IsFusibleSiblingSetupNode(ExecutionNode node)
    {
        return node switch
        {
            ExecutionCreateTable or ExecutionCreateHash or ExecutionCreateKeySet => true,
            ExecutionCteSidecarIndexBuildCandidate => true,
            ExecutionCteIndexOnlyStorageCandidate => true,
            _ => false
        };
    }

    private static bool ContainsNestedSiblingLoop(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectNodes<ExecutionForEach>(block).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionForEachIndexed>(block).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionParallelBlock>(block).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionParallelFilterProjectLoop>(block).Any();
    }

    private static FusedSiblingCteBuildResult CreateFusedSiblingCteBuildResult(
        IReadOnlyList<FusedSiblingCteCandidate> candidates,
        IReadOnlyList<RowShape> shapes,
        IReadOnlyDictionary<string, GeneratedRowShape> rowShapesByName)
    {
        var canonicalLoop = candidates[0].Loop;
        var bodyNodes = new List<ExecutionNode>();

        foreach (var candidate in candidates)
        {
            var body = RewriteFusedSiblingLoopBody(
                candidate.Loop.Body,
                candidate.Loop.Item,
                canonicalLoop.Item);
            bodyNodes.AddRange(body.Nodes);
        }

        var producerBody = new List<ExecutionNode>();
        producerBody.AddRange(candidates.SelectMany(static candidate => candidate.SetupNodes));
        producerBody.Add(canonicalLoop with { Body = new ExecutionBlock(bodyNodes) });
        producerBody.AddRange(candidates.SelectMany(static candidate => candidate.StoreIndexNodes));

        var outputs = candidates
            .Select(static candidate => new ExecutionFusedCteOutput(
                candidate.TableIndex,
                candidate.Result.Table,
                candidate.Result.RowShape,
                candidate.Storage.StoreRows))
            .ToArray();
        var producer = new ExecutionFusedCteProducer(outputs, new ExecutionBlock(producerBody));

        return new FusedSiblingCteBuildResult(
            candidates.Count,
            shapes,
            producer,
            rowShapesByName);
    }

}
