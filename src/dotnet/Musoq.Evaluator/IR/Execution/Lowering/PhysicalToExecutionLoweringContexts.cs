using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering;

internal sealed record PhysicalToExecutionLoweringContext(PhysicalNode Plan, string Identifier, LoweringScope Scope)
{
}

internal sealed record PhysicalToExecutionTableLoweringContext(
    PhysicalNode Plan, string ResultTableName, string ResultShapeName,
    IReadOnlyDictionary<string, int> CteIndexes,
    IReadOnlyDictionary<string, GeneratedRowShape>? CteShapesByName,
    int SchemaFromIndex, bool ScopeAggregateVariables, LoweringScope Scope)
{
}
