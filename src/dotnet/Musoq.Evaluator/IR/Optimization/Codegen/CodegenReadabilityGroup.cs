using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Codegen;

internal static class CodegenReadabilityGroup
{
    public static OptimizationPassPipeline<CompilationUnitSyntax> Pipeline { get; } = new(
        OptimizationStage.CodegenReadability,
        OptimizationPassRunMode.Once,
        [
            new(new DeterministicMemberOrderingPass(), "Order members deterministically for stable output."),
            new(new LocalDeclarationNormalizationPass(), "Normalize local declarations."),
            new(new DeadTemporaryCleanupPass(), "Remove dead temporary locals."),
            new(new ControlFlowNormalizationPass(), "Normalize control-flow shape for readability."),
            new(new HelperExtractionReadabilityPass(), "Extract repeated fragments into helpers."),
            new(new ReadabilityDecisionTracePass(), "Record readability decisions into the trace.")
        ]);

    public static IReadOnlyList<IPlanOptimizationPass<CompilationUnitSyntax>> Passes => Pipeline.Passes;
}

