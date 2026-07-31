using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Targets.Execution;

namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

internal sealed class CodegenReadabilityOptimizer
{
    public CodegenReadabilityOptimizationResult Optimize(
        CompilationUnitSyntax initialCode,
        TargetRenderProfile profile = TargetRenderProfile.StableArtifact)
    {
        ArgumentNullException.ThrowIfNull(initialCode);

        var trace = new OptimizationTrace();
        var pipeline = profile == TargetRenderProfile.ExecutionFast
            ? CodegenReadabilityGroup.ExecutionPipeline
            : CodegenReadabilityGroup.Pipeline;
        var result = new PlanOptimizationRunner<CompilationUnitSyntax>(
            pipeline).Run(
            initialCode,
            new OptimizationContext(OptimizationStage.CodegenReadability, trace),
            static passName => TargetRenderTelemetry.BeginPhase($"render.readability.{passName}"));

        return new CodegenReadabilityOptimizationResult(initialCode, result.Plan, trace);
    }
}

