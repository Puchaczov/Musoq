using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Lowering;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    WindowPipeline? IWindowLoweringService.DecomposeWindowPipeline(PhysicalNode node) => DecomposeWindowPipeline(node);

    LoweringAttempt<LoweredTable> IWindowLoweringService.BuildWindowTable(
        WindowPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildWindowTable(pipeline, resultTableName, resultShapeName,
            cteIndexes, cteShapesByName, schemaFromIndex, scope));

    LoweringAttempt<ExecutionPlan> IWindowLoweringService.CreatePlanResult(
        string identifier, LoweringAttempt<LoweredTable> table) =>
        table.Kind switch
        {
            LoweringAttemptKind.Built => LoweringAttempt<ExecutionPlan>.Built(
                CreateTableResultPlan(identifier, table.RequireValue().ToCompatibilityResult())),
            LoweringAttemptKind.Unsupported => LoweringAttempt<ExecutionPlan>.Unsupported(table.RequireUnsupportedReason()),
            _ => LoweringAttempt<ExecutionPlan>.NoMatch()
        };
}
