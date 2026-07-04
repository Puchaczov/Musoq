using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
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
        IReadOnlyDictionary<string, FusedCteHashBuildSource> fusedHashBuildSources,
        PhysicalToExecutionLoweringSession session)
    {
        if (!_compilationOptions.UseCteSidecarIndexes)
            return null;

        var lowerer = new FusedSiblingCteLowerer(
            UnwrapSingleStatement,
            (node, definitionName) => ExecutionStrategies.GetCteSidecarIndexSpecs(node, definitionName),
            (definition, index, definitionNames, indexes, shapesByName, schemaFromIndex, plan) =>
                BuildCteDefinitionTable(definition, index, definitionNames, indexes, shapesByName, schemaFromIndex, plan, session),
            (
                string definitionName,
                IReadOnlyList<CteSidecarIndexSpec> specs,
                IReadOnlyDictionary<string, CteReferenceClassification> classifications,
                CteDefinitionPruningPlan plan,
                TableBuildResult result,
                out CteSidecarStorageDecision storage) =>
                ApplyCteSidecarOptimizations(definitionName, specs, classifications, plan, result, session, out storage),
            CreateFusedSiblingCteBuildResult);

        return lowerer.TryBuild(
            cte,
            startIndex,
            exclusiveEndIndex,
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes,
            pruningPlan,
            cteReferenceClassifications,
            fusedHashBuildSources);
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
