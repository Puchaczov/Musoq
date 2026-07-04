using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private CteDefinitionPrefixBuildResult BuildCteDefinitionPrefix(
        PhysicalCteNode cte,
        PhysicalCteDefinition[] definitions,
        int exclusiveEndIndex,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        Dictionary<string, GeneratedRowShape> cteShapesByName,
        Dictionary<string, int> schemaFromIndexes,
        CteDefinitionPruningPlan pruningPlan,
        bool applySidecarIndexes,
        PhysicalToExecutionLoweringSession session)
    {
        var shapes = new List<RowShape>();
        var nodes = new List<ExecutionNode>();
        var cteReferenceClassifications = applySidecarIndexes
            ? ClassifyCteReferences(cte)
            : new Dictionary<string, CteReferenceClassification>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < exclusiveEndIndex;)
        {
            var definition = definitions[index];
            if (applySidecarIndexes &&
                TryBuildFusedPrefixSiblings(
                    cte,
                    index,
                    exclusiveEndIndex,
                    cteDefinitionNames,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndexes,
                    pruningPlan,
                    cteReferenceClassifications,
                    shapes,
                    nodes,
                    session,
                    out var fusedDefinitionCount))
            {
                index += fusedDefinitionCount;
                continue;
            }

            var result = BuildCteDefinitionTable(
                definition,
                index,
                cteDefinitionNames,
                cteIndexes,
                cteShapesByName,
                schemaFromIndexes[definition.Name],
                pruningPlan,
                session);

            if (applySidecarIndexes)
            {
                var sidecarSpecs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
                result = ApplyCteSidecarOptimizations(
                    definition.Name,
                    sidecarSpecs,
                    cteReferenceClassifications,
                    pruningPlan,
                    result,
                    session,
                    out var storage);

                if (!result.Supported)
                    return CteDefinitionPrefixBuildResult.Unsupported(result.UnsupportedReason);

                cteShapesByName[definition.Name] = result.RowShape;
                shapes.AddRange(result.Shapes);
                nodes.AddRange(result.Nodes);
                if (storage.StoreRows)
                    nodes.Add(new ExecutionStoreTable(result.Table, index));

                index++;
                continue;
            }

            if (!result.Supported)
                return CteDefinitionPrefixBuildResult.Unsupported(result.UnsupportedReason);

            cteShapesByName[definition.Name] = result.RowShape;
            shapes.AddRange(result.Shapes);
            nodes.AddRange(result.Nodes);
            nodes.Add(new ExecutionStoreTable(result.Table, index));
            index++;
        }

        return CteDefinitionPrefixBuildResult.Success(shapes, nodes);
    }

    private bool TryBuildFusedPrefixSiblings(
        PhysicalCteNode cte,
        int index,
        int exclusiveEndIndex,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        Dictionary<string, GeneratedRowShape> cteShapesByName,
        Dictionary<string, int> schemaFromIndexes,
        CteDefinitionPruningPlan pruningPlan,
        IReadOnlyDictionary<string, CteReferenceClassification> cteReferenceClassifications,
        List<RowShape> shapes,
        List<ExecutionNode> nodes,
        PhysicalToExecutionLoweringSession session,
        out int definitionCount)
    {
        definitionCount = 0;
        var siblingFusion = TryBuildFusedSiblingCteProducers(
            cte,
            index,
            exclusiveEndIndex,
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes,
            pruningPlan,
            cteReferenceClassifications,
            new Dictionary<string, FusedCteHashBuildSource>(StringComparer.OrdinalIgnoreCase),
            session);

        if (siblingFusion == null)
            return false;

        foreach (var (name, rowShape) in siblingFusion.RowShapesByName)
            cteShapesByName[name] = rowShape;

        var producer = CteSourceBackedSiblingFusion.TryRewrite(
            nodes,
            shapes,
            cteIndexes,
            cteReferenceClassifications,
            SourceInteractionPlans,
            siblingFusion.Producer) ?? siblingFusion.Producer;
        shapes.AddRange(siblingFusion.Shapes);
        nodes.Add(CreateCteFusedProducerCandidate(producer));
        definitionCount = siblingFusion.DefinitionCount;
        return true;
    }

    private static ExecutionCteFusedProducerCandidate CreateCteFusedProducerCandidate(
        ExecutionFusedCteProducer producer)
    {
        return new ExecutionCteFusedProducerCandidate(producer.Outputs, producer.Body);
    }
}
