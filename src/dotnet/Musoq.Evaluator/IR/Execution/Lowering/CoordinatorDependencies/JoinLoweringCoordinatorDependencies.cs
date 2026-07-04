using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private JoinLoweringCoordinator CreateJoinLoweringCoordinator()
    {
        return new JoinLoweringCoordinator(
            BuildNestedLoopJoinTable,
            BuildHashJoinTable,
            BuildSortMergeJoinTable);
    }

    private delegate TableBuildResult BuildNestedLoopJoinTableDelegate(
        PhysicalNestedLoopJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session);

    private delegate TableBuildResult BuildHashJoinTableDelegate(
        PhysicalHashJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session);

    private delegate TableBuildResult BuildSortMergeJoinTableDelegate(
        PhysicalSortMergeJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session);
}
