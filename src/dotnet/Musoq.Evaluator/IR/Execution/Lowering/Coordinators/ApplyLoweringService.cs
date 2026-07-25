using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal interface IApplyLoweringService
{
    LoweringAttempt<LoweredTable> BuildApplyTable(
        PhysicalNestedLoopApplyNode apply, CteSupportedPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, IReadOnlyDictionary<string, RowShape>? inheritedSourceLookup, LoweringScope scope);
}

internal interface IApplyLoweringOperations : IApplyLoweringService
{
}

internal sealed class ApplyLoweringService(
    IApplyLoweringOperations operations) : IApplyLoweringService
{
    public LoweringAttempt<LoweredTable> BuildApplyTable(
        PhysicalNestedLoopApplyNode apply, CteSupportedPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, IReadOnlyDictionary<string, RowShape>? inheritedSourceLookup, LoweringScope scope) =>
        operations.BuildApplyTable(apply, pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName,
            schemaFromIndex, inheritedSourceLookup, scope);
}
