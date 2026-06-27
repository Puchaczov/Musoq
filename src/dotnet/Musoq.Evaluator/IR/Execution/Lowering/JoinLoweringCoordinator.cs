using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed class JoinLoweringCoordinator(PhysicalToExecutionPlanBuilder builder)
    {
        public PhysicalToExecutionPlanBuilder.TableBuildResult Build(
            PhysicalToExecutionPlanBuilder.SupportedPipeline pipeline,
            string resultTableName,
            string resultShapeName,
            IReadOnlyDictionary<string, int> cteIndexes,
            IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
            int schemaFromIndex)
        {
            return pipeline.Source switch
            {
                PhysicalNestedLoopJoinNode nestedLoopJoin => builder.BuildNestedLoopJoinTable(
                    nestedLoopJoin,
                    pipeline,
                    resultTableName,
                    resultShapeName,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndex),
                PhysicalHashJoinNode hashJoin => builder.BuildHashJoinTable(
                    hashJoin,
                    pipeline,
                    resultTableName,
                    resultShapeName,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndex),
                PhysicalSortMergeJoinNode sortMergeJoin => builder.BuildSortMergeJoinTable(
                    sortMergeJoin,
                    pipeline,
                    resultTableName,
                    resultShapeName,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndex),
                _ => PhysicalToExecutionPlanBuilder.TableBuildResult.Unsupported(
                    $"Execution IR join lowering does not support source node '{pipeline.Source.GetType().Name}'.")
            };
        }
    }
}
