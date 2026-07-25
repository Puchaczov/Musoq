using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

/// <summary>
/// Typed capability boundary for source pipelines and set operations.
/// </summary>
internal interface IPipelineLoweringService
{
    CteSupportedPipeline? DecomposeSupportedPipeline(PhysicalNode node);

    SetOperationPipeline? DecomposeSetOperationPipeline(PhysicalNode node);

    LoweringAttempt<ExecutionPlan> BuildPipeline(
        CteSupportedPipeline pipeline,
        string identifier,
        LoweringScope scope);

    LoweringAttempt<ExecutionPlan> BuildSetOperation(
        SetOperationPipeline pipeline,
        string identifier,
        LoweringScope scope);

    LoweringAttempt<LoweredTable> BuildTable(
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope);

    LoweringAttempt<LoweredTable> BuildSetOperationTable(
        SetOperationPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope);
}
