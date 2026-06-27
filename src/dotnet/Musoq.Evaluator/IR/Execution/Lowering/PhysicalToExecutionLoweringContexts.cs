using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record PhysicalToExecutionLoweringContext(PhysicalNode Plan, string Identifier);

    private sealed record PhysicalToExecutionTableLoweringContext(
        PhysicalNode Plan,
        string ResultTableName,
        string ResultShapeName,
        IReadOnlyDictionary<string, int> CteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? CteShapesByName,
        int SchemaFromIndex,
        bool ScopeAggregateVariables);
}
