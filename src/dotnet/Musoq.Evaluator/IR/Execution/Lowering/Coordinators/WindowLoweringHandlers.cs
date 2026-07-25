using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal interface IWindowLoweringService
{
    WindowPipeline? DecomposeWindowPipeline(PhysicalNode node);
    LoweringAttempt<ExecutionPlan> CreatePlanResult(string identifier, LoweringAttempt<LoweredTable> table);
    LoweringAttempt<LoweredTable> BuildWindowTable(
        WindowPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, LoweringScope scope);
}

internal interface IWindowLoweringOperations : IWindowLoweringService
{
}
