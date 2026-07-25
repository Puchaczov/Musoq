using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal sealed class JoinLoweringService(
    IJoinLoweringOperations operations) : IJoinLoweringService
{
    public LoweringAttempt<LoweredTable> BuildNestedLoopJoinTable(
        PhysicalNestedLoopJoinNode join, CteSupportedPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, LoweringScope scope) => operations.BuildNestedLoopJoinTable(join, pipeline, resultTableName,
            resultShapeName, cteIndexes, cteShapesByName, schemaFromIndex, scope);

    public LoweringAttempt<LoweredTable> BuildHashJoinTable(
        PhysicalHashJoinNode join, CteSupportedPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, LoweringScope scope) => operations.BuildHashJoinTable(join, pipeline, resultTableName,
            resultShapeName, cteIndexes, cteShapesByName, schemaFromIndex, scope);

    public LoweringAttempt<LoweredTable> BuildSortMergeJoinTable(
        PhysicalSortMergeJoinNode join, CteSupportedPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, LoweringScope scope) => operations.BuildSortMergeJoinTable(join, pipeline, resultTableName,
            resultShapeName, cteIndexes, cteShapesByName, schemaFromIndex, scope);
}
