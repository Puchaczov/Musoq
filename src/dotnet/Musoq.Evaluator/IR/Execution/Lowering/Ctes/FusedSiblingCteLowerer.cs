using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed class FusedSiblingCteLowerer
{
    public delegate TableBuildResult BuildCteDefinitionTable(
        PhysicalCteDefinition definition,
        int index,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        int schemaFromIndex,
        CteDefinitionPruningPlan pruningPlan);

    public delegate TableBuildResult ApplySidecarOptimizations(
        string definitionName,
        IReadOnlyList<CteSidecarIndexSpec> sidecarSpecs,
        IReadOnlyDictionary<string, CteReferenceClassification> cteReferenceClassifications,
        CteDefinitionPruningPlan pruningPlan,
        TableBuildResult result,
        out CteSidecarStorageDecision storage);

    public delegate FusedSiblingCteBuildResult CreateBuildResult(
        IReadOnlyList<FusedSiblingCteCandidate> candidates,
        IReadOnlyList<RowShape> shapes,
        IReadOnlyDictionary<string, GeneratedRowShape> rowShapesByName);

    private readonly Func<PhysicalNode, PhysicalNode> _unwrapSingleStatement;
    private readonly Func<PhysicalCteNode, string, IReadOnlyList<CteSidecarIndexSpec>> _getSidecarIndexSpecs;
    private readonly BuildCteDefinitionTable _buildCteDefinitionTable;
    private readonly ApplySidecarOptimizations _applySidecarOptimizations;
    private readonly CreateBuildResult _createBuildResult;

    public FusedSiblingCteLowerer(
        Func<PhysicalNode, PhysicalNode> unwrapSingleStatement,
        Func<PhysicalCteNode, string, IReadOnlyList<CteSidecarIndexSpec>> getSidecarIndexSpecs,
        BuildCteDefinitionTable buildCteDefinitionTable,
        ApplySidecarOptimizations applySidecarOptimizations,
        CreateBuildResult createBuildResult)
    {
        _unwrapSingleStatement = unwrapSingleStatement;
        _getSidecarIndexSpecs = getSidecarIndexSpecs;
        _buildCteDefinitionTable = buildCteDefinitionTable;
        _applySidecarOptimizations = applySidecarOptimizations;
        _createBuildResult = createBuildResult;
    }

    public FusedSiblingCteBuildResult? TryBuild(
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
        if (!TryGetSimpleSiblingSourceCte(cte.Definitions[startIndex], out var sourceCteName) ||
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

            var sidecarSpecs = _getSidecarIndexSpecs(cte, definition.Name);
            if (sidecarSpecs.Count == 0)
                break;

            var result = _buildCteDefinitionTable(
                definition,
                index,
                cteDefinitionNames,
                cteIndexes,
                cteShapesByName,
                schemaFromIndexes[definition.Name],
                pruningPlan);
            result = _applySidecarOptimizations(
                definition.Name,
                sidecarSpecs,
                cteReferenceClassifications,
                pruningPlan,
                result,
                out var storage);

            if (!result.IsBuilt ||
                ContainsSideEffectSensitiveSiblingExpression(result.Nodes) ||
                !TryExtractFusibleBuild(result, sourceTableIndex, out var setupNodes, out var loop, out var storeIndexNodes))
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
            : _createBuildResult(candidates, shapes, rowShapesByName);
    }

    private bool TryGetSimpleSiblingSourceCte(
        PhysicalCteDefinition definition,
        out string sourceCteName)
    {
        sourceCteName = string.Empty;
        var plan = _unwrapSingleStatement(definition.Plan);
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
        return ExecutionIrAnalysis.CollectExpressions<ExecutionMethodCall>(block).Any();
    }

    private static bool TryExtractFusibleBuild(
        TableBuildResult result,
        int sourceTableIndex,
        out IReadOnlyList<ExecutionNode> setupNodes,
        [NotNullWhen(true)] out ExecutionForEach? loop,
        out IReadOnlyList<ExecutionNode> storeIndexNodes)
    {
        setupNodes = [];
        loop = null;
        storeIndexNodes = [];

        var nodes = result.Nodes.ToArray();
        var loopIndex = Array.FindIndex(nodes, static node => node is ExecutionForEach);
        if (loopIndex < 0 ||
            Array.FindIndex(nodes, loopIndex + 1, static node => node is ExecutionForEach) >= 0 ||
            nodes[loopIndex] is not ExecutionForEach currentLoop ||
            currentLoop.Source is not ExecutionStoredTableRows { TableIndex: var tableIndex } ||
            tableIndex != sourceTableIndex ||
            ContainsNestedLoop(currentLoop.Body))
        {
            return false;
        }

        var setup = nodes[..loopIndex];
        if (setup.Any(static node => !IsFusibleSetupNode(node)))
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

    private static bool IsFusibleSetupNode(ExecutionNode node)
    {
        return node switch
        {
            ExecutionPhaseBoundary => true,
            ExecutionCreateTable or ExecutionCreateHash or ExecutionCreateKeySet => true,
            ExecutionCteSidecarIndexBuildCandidate => true,
            ExecutionCteIndexOnlyStorageCandidate => true,
            _ => false
        };
    }

    private static bool ContainsNestedLoop(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectNodes<ExecutionForEach>(block).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionForEachIndexed>(block).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionParallelBlock>(block).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionParallelFilterProjectLoop>(block).Any();
    }
}
