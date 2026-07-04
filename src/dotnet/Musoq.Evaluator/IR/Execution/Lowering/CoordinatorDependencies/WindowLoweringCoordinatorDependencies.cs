using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private WindowLoweringCoordinator CreateWindowLoweringCoordinator()
    {
        return new WindowLoweringCoordinator(
            BuildWindowPipeline,
            BuildWindowTable);
    }

    private delegate ExecutionPlanBuildResult BuildWindowPlanDelegate(
        WindowPipeline pipeline,
        string identifier,
        PhysicalToExecutionLoweringSession session);

    private delegate TableBuildResult BuildWindowTableDelegate(
        WindowPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session);
}
