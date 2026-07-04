using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record PhysicalToExecutionLoweringContext(PhysicalNode Plan, string Identifier, PhysicalToExecutionLoweringSession Session);

internal sealed record PhysicalToExecutionTableLoweringContext(
    PhysicalNode Plan, string ResultTableName, string ResultShapeName,
    IReadOnlyDictionary<string, int> CteIndexes,
    IReadOnlyDictionary<string, GeneratedRowShape>? CteShapesByName,
    int SchemaFromIndex, bool ScopeAggregateVariables, PhysicalToExecutionLoweringSession Session);
