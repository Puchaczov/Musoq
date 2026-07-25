using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
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
        LoweringScope scope)
    {
        var shapes = new List<RowShape>();
        var nodes = new List<ExecutionNode>();
        var cteReferenceClassifications = applySidecarIndexes
            ? ClassifyCteReferences(cte)
            : new Dictionary<string, CteReferenceClassification>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < exclusiveEndIndex;)
        {
            var definition = definitions[index];
            if (definition.Plan is PhysicalRecursiveCteNode recursive)
            {
                recursive = ApplyRecursiveCteDefinitionPruning(definition.Name, recursive, pruningPlan);
                var recursiveResult = BuildRecursiveCteDefinitionTable(
                    cte,
                    recursive,
                    index,
                    cteDefinitionNames,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndexes[definition.Name],
                    scope);
                if (!recursiveResult.IsBuilt)
                    return CteDefinitionPrefixBuildResult.Unsupported(recursiveResult.UnsupportedReason) with
                    {
                        UpdatedScope = scope
                    };

                cteShapesByName[definition.Name] = recursiveResult.RowShape;
                shapes.AddRange(recursiveResult.Shapes);
                nodes.AddRange(recursiveResult.Nodes);
                nodes.Add(new ExecutionStoreTable(recursiveResult.Table, index));
                index++;
                continue;
            }

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
                    scope,
                    out var fusedDefinitionCount,
                    out var fusedUpdatedScope))
            {
                scope = fusedUpdatedScope;
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
                scope);

            if (applySidecarIndexes)
            {
                var sidecarSpecs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
                result = ApplyCteSidecarOptimizations(
                    definition.Name,
                    sidecarSpecs,
                    cteReferenceClassifications,
                    pruningPlan,
                    result,
                    scope,
                    out var storage,
                    out var sidecarUpdatedScope);
                scope = sidecarUpdatedScope;

                if (!result.IsBuilt)
                    return CteDefinitionPrefixBuildResult.Unsupported(result.UnsupportedReason) with
                    {
                        UpdatedScope = scope
                    };

                cteShapesByName[definition.Name] = result.RowShape;
                shapes.AddRange(result.Shapes);
                nodes.AddRange(result.Nodes);
                if (storage.StoreRows)
                    nodes.Add(new ExecutionStoreTable(result.Table, index));

                index++;
                continue;
            }

            if (!result.IsBuilt)
                return CteDefinitionPrefixBuildResult.Unsupported(result.UnsupportedReason) with
                {
                    UpdatedScope = scope
                };

            cteShapesByName[definition.Name] = result.RowShape;
            shapes.AddRange(result.Shapes);
            nodes.AddRange(result.Nodes);
            nodes.Add(new ExecutionStoreTable(result.Table, index));
            index++;
        }

        return CteDefinitionPrefixBuildResult.Success(shapes, nodes) with
        {
            UpdatedScope = scope
        };
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
        LoweringScope scope,
        out int definitionCount,
        out LoweringScope updatedScope)
    {
        definitionCount = 0;
        updatedScope = scope;
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
            scope,
            out updatedScope);

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
