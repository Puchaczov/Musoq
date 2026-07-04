using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Codegen;

internal sealed class CodegenReadabilityOptimizer
{
    public CodegenReadabilityOptimizationResult Optimize(CompilationUnitSyntax initialCode)
    {
        ArgumentNullException.ThrowIfNull(initialCode);

        var trace = new OptimizationTrace();
        var result = new PlanOptimizationRunner<CompilationUnitSyntax>(
            CodegenReadabilityGroup.Pipeline).Run(
            initialCode,
            new OptimizationContext(OptimizationStage.CodegenReadability, trace));

        return new CodegenReadabilityOptimizationResult(initialCode, result.Plan, trace);
    }
}

