using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal interface IPipelineLoweringOperations : IPipelineLoweringService
{
}

internal sealed class PipelineLoweringService(IPipelineLoweringOperations operations) : IPipelineLoweringService
{
    public CteSupportedPipeline? DecomposeSupportedPipeline(PhysicalNode node) =>
        operations.DecomposeSupportedPipeline(node);

    public SetOperationPipeline? DecomposeSetOperationPipeline(PhysicalNode node) =>
        operations.DecomposeSetOperationPipeline(node);

    public LoweringAttempt<ExecutionPlan> BuildPipeline(
        CteSupportedPipeline pipeline,
        string identifier,
        LoweringScope scope) => operations.BuildPipeline(pipeline, identifier, scope);

    public LoweringAttempt<ExecutionPlan> BuildSetOperation(
        SetOperationPipeline pipeline,
        string identifier,
        LoweringScope scope) => operations.BuildSetOperation(pipeline, identifier, scope);

    public LoweringAttempt<LoweredTable> BuildTable(
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope) => operations.BuildTable(
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope);

    public LoweringAttempt<LoweredTable> BuildSetOperationTable(
        SetOperationPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope) => operations.BuildSetOperationTable(
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope);
}
