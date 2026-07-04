using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed class JoinLoweringCoordinator(
        BuildNestedLoopJoinTableDelegate buildNestedLoopJoinTable,
        BuildHashJoinTableDelegate buildHashJoinTable,
        BuildSortMergeJoinTableDelegate buildSortMergeJoinTable)
    {
        public TableBuildResult Build(
            SupportedPipeline pipeline,
            string resultTableName,
            string resultShapeName,
            IReadOnlyDictionary<string, int> cteIndexes,
            IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
            int schemaFromIndex,
            PhysicalToExecutionLoweringSession session)
        {
            return pipeline.Source switch
            {
                PhysicalNestedLoopJoinNode nestedLoopJoin => buildNestedLoopJoinTable(
                    nestedLoopJoin,
                    pipeline,
                    resultTableName,
                    resultShapeName,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndex,
                    session),
                PhysicalHashJoinNode hashJoin => buildHashJoinTable(
                    hashJoin,
                    pipeline,
                    resultTableName,
                    resultShapeName,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndex,
                    session),
                PhysicalSortMergeJoinNode sortMergeJoin => buildSortMergeJoinTable(
                    sortMergeJoin,
                    pipeline,
                    resultTableName,
                    resultShapeName,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndex,
                    session),
                _ => TableBuildResult.Unsupported(
                    $"Execution IR join lowering does not support source node '{pipeline.Source.GetType().Name}'.")
            };
        }
    }
}
