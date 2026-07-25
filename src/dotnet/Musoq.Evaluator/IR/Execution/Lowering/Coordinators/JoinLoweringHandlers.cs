using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

/// <summary>
/// The join lowering boundary.  Domain lowerers depend on this capability
/// contract, never on the physical-lowering composition type.
/// </summary>
internal interface IJoinLoweringService
{
    LoweringAttempt<LoweredTable> BuildNestedLoopJoinTable(
        PhysicalNestedLoopJoinNode join,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope);

    LoweringAttempt<LoweredTable> BuildHashJoinTable(
        PhysicalHashJoinNode join,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope);

    LoweringAttempt<LoweredTable> BuildSortMergeJoinTable(
        PhysicalSortMergeJoinNode join,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope);
}

internal interface IJoinLoweringOperations : IJoinLoweringService
{
}
