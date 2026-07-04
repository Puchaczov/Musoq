using System.Collections.Generic;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
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
                result.Supported,
                result.Nodes,
                result.RowShape.TypeName);
    }

    private TableBuildResult ApplyCteSidecarOptimizations(
        string definitionName,
        IReadOnlyList<CteSidecarIndexSpec> sidecarSpecs,
        IReadOnlyDictionary<string, CteReferenceClassification> cteReferenceClassifications,
        CteDefinitionPruningPlan pruningPlan,
        TableBuildResult result,
        PhysicalToExecutionLoweringSession session,
        out CteSidecarStorageDecision storage)
    {
        result = ApplyCteSidecarIndexes(result, sidecarSpecs, session);
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
