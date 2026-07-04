using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Codegen;

internal sealed class ReadabilityDecisionTracePass : ICodegenReadabilityOptimizationPass
{
    public string Name => "ReadabilityDecisionTrace";

    public OptimizationResult<CompilationUnitSyntax> Optimize(CompilationUnitSyntax plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return OptimizationResult<CompilationUnitSyntax>.NoChange(
            plan,
            "Readability pass group completed; strategy decisions remain owned by logical, physical, and Execution IR optimizers.");
    }
}

