using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult? TryBuildReadOnceCteProjectionTable(
        PhysicalCteNode cte,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyCollection<string> cteDefinitionNames,
        Dictionary<string, GeneratedRowShape> cteShapesByName,
        Dictionary<string, int> schemaFromIndexes,
        IReadOnlyList<ParallelCteLevel>? parallelLevels,
        CteDefinitionPruningPlan pruningPlan,
        PhysicalToExecutionLoweringSession session)
    {
        if (parallelLevels != null || cte.Definitions.Length == 0)
            return null;

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(cte.Query));
        if (finalPipeline is not { Source: PhysicalCteRefNode finalCteRef } ||
            finalPipeline.PostOperations.Count != 0)
        {
            return null;
        }

        var strategy = ExecutionStrategies.GetCteStrategy(cte);
        var lowerer = new ReadOnceCteProjectionLowerer(
            UnwrapSingleStatement,
            DecomposeSupportedPipeline,
            RewriteFinalJoinProjection,
            CanInlineFinalProjectionSource);
        var fusion = lowerer.TryCreateFusion(cte.Definitions, finalPipeline, finalCteRef, strategy);
        if (fusion == null)
            return null;

        var prefix = BuildCteDefinitionPrefix(
            cte,
            cte.Definitions,
            fusion.RootDefinitionIndex,
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes,
            pruningPlan,
            applySidecarIndexes: false,
            session);
        if (!prefix.Supported)
            return TableBuildResult.Unsupported(prefix.UnsupportedReason);

        var rootDefinition = cte.Definitions[fusion.RootDefinitionIndex];
        var result = BuildTable(
            fusion.Pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes[rootDefinition.Name],
            session: session);
        if (!result.Supported)
            return result;

        return TableBuildResult.Success(
            [..prefix.Shapes, ..result.Shapes],
            [..prefix.Nodes, ..ReadOnceCteProjectionLowerer.CreateCandidates(fusion.FusedDefinitionIndexes, result.Nodes)],
            result.Table,
            result.RowShape);
    }

}
