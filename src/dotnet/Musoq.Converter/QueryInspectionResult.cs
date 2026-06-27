using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Converter;

public sealed record QueryInspectionResult(
    LogicalNode LogicalPlan,
    PhysicalNode PhysicalPlan,
    string LogicalPlanText,
    string PhysicalPlanText,
    string GeneratedCSharpCode)
{
    public string PlanningText { get; init; } = string.Empty;

    public string ExecutionPlanText { get; init; } = string.Empty;

    public ExecutionPlan? ExecutionPlan { get; init; }

    public string InitialLogicalPlanText { get; init; } = string.Empty;

    public string OptimizedLogicalPlanText { get; init; } = string.Empty;

    public string InitialPhysicalPlanText { get; init; } = string.Empty;

    public string OptimizedPhysicalPlanText { get; init; } = string.Empty;

    public string InitialExecutionPlanText { get; init; } = string.Empty;

    public string OptimizedExecutionPlanText { get; init; } = string.Empty;

    public string OptimizerTraceText { get; init; } = string.Empty;

    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    public IReadOnlyList<Diagnostic> Warnings { get; init; } = [];
}
