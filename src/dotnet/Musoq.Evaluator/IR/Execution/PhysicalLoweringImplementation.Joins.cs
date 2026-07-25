using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildJoinTable(
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        var attempt = _joinPlanLowerer.Build(
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope);

        return attempt.Kind switch
        {
            LoweringAttemptKind.Built => attempt.RequireValue().ToCompatibilityResult(),
            LoweringAttemptKind.Unsupported => TableBuildResult.Unsupported(
                attempt.RequireUnsupportedReason()),
            _ => TableBuildResult.Unsupported("Execution IR join lowering did not claim the source node.")
        };
    }

}
