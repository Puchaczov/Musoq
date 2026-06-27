using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

/// <summary>
/// Typed view of the planning stage output: logical and physical plans together
/// with the planner result and its textual diagnostics.
/// </summary>
internal sealed record PlanningBuildArtifacts
{
    public LogicalNode? InitialLogicalPlan { get; init; }

    public LogicalNode? OptimizedLogicalPlan { get; init; }

    public LogicalNode? LogicalPlan { get; init; }

    public PlanningResult? PlanningResult { get; init; }

    public string? PlanningText { get; init; }

    public PhysicalNode? InitialPhysicalPlan { get; init; }

    public PhysicalNode? OptimizedPhysicalPlan { get; init; }

    public PhysicalNode? PhysicalPlan { get; init; }
}
