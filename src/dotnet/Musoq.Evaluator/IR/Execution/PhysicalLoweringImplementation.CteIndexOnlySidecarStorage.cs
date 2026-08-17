using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private CteSidecarStorageDecision CreateCteSidecarStorageDecision(
        string definitionName,
        IReadOnlyList<CteSidecarIndexSpec> sidecarSpecs,
        IReadOnlyDictionary<string, CteReferenceClassification> classifications,
        TableBuildResult result)
    {
        return new CteSidecarStoragePlanner(_compilationOptions.UseCteSidecarIndexes)
            .CreateStorageDecision(
                definitionName,
                sidecarSpecs,
                classifications,
                result.IsBuilt,
                result.Nodes,
                result.RowShape.TypeName);
    }

    private TableBuildResult ApplyCteSidecarOptimizations(
        string definitionName,
        IReadOnlyList<CteSidecarIndexSpec> sidecarSpecs,
        IReadOnlyDictionary<string, CteReferenceClassification> cteReferenceClassifications,
        CteDefinitionPruningPlan pruningPlan,
        TableBuildResult result,
        LoweringScope scope,
        out CteSidecarStorageDecision storage,
        out LoweringScope updatedScope)
    {
        result = ApplyCteSidecarIndexes(result, sidecarSpecs, scope, out updatedScope);
        result = ApplyCteContextPruning(result, definitionName, pruningPlan);
        storage = CreateCteSidecarStorageDecision(
            definitionName,
            sidecarSpecs,
            cteReferenceClassifications,
            result);

        return ApplyIndexOnlyCteSidecarStorage(result, storage);
    }

    private static TableBuildResult ApplyIndexOnlyCteSidecarStorage(
        TableBuildResult result,
        CteSidecarStorageDecision decision)
    {
        if (decision.StoreRows)
            return result;

        return result with
        {
            Nodes = new CteSidecarStoragePlanner(useCteSidecarIndexes: true)
                .ApplyIndexOnlyStorage(result.Table.Name, result.RowShape.TypeName, result.Nodes, decision)
        };
    }
}
