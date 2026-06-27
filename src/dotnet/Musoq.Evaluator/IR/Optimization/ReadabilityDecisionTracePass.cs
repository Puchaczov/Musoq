using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class ReadabilityDecisionTracePass : IPlanOptimizationPass<CompilationUnitSyntax>
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
