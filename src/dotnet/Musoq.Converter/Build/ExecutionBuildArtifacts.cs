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
/// Typed view of the execution stage output: the lowered execution plan and its
/// build result, including the optional textual dump.
/// </summary>
internal sealed record ExecutionBuildArtifacts
{
    public ExecutionPlanBuildResult? ExecutionPlanBuildResult { get; init; }

    public ExecutionPlan? InitialExecutionPlan { get; init; }

    public ExecutionPlan? OptimizedExecutionPlan { get; init; }

    public ExecutionPlan? ExecutionPlan { get; init; }

    public string? ExecutionPlanText { get; init; }
}
