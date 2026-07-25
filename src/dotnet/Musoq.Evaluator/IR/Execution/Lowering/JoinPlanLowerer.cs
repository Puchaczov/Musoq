using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;
namespace Musoq.Evaluator.IR.Execution.Lowering;

internal sealed class JoinPlanLowerer(
    IJoinLoweringService service)
{
    public LoweringAttempt<LoweredTable> Build(
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        return pipeline.Source switch
        {
            PhysicalNestedLoopJoinNode nestedLoopJoin => service.BuildNestedLoopJoinTable(
                nestedLoopJoin,
                pipeline,
                resultTableName,
                resultShapeName,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                scope),
            PhysicalHashJoinNode hashJoin => service.BuildHashJoinTable(
                hashJoin,
                pipeline,
                resultTableName,
                resultShapeName,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                scope),
            PhysicalSortMergeJoinNode sortMergeJoin => service.BuildSortMergeJoinTable(
                sortMergeJoin,
                pipeline,
                resultTableName,
                resultShapeName,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                scope),
            _ => LoweringAttempt<LoweredTable>.Unsupported(
                $"Execution IR join lowering does not support source node '{pipeline.Source.GetType().Name}'.")
        };
    }
}
