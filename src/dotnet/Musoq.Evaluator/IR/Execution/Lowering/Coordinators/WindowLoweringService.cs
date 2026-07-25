using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal sealed class WindowLoweringService(
    IWindowLoweringOperations operations) : IWindowLoweringService
{
    public WindowPipeline? DecomposeWindowPipeline(PhysicalNode node) => operations.DecomposeWindowPipeline(node);

    public LoweringAttempt<ExecutionPlan> CreatePlanResult(string identifier, LoweringAttempt<LoweredTable> table) =>
        operations.CreatePlanResult(identifier, table);

    public LoweringAttempt<LoweredTable> BuildWindowTable(
        WindowPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, LoweringScope scope) =>
        operations.BuildWindowTable(pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName,
            schemaFromIndex, scope);
}
