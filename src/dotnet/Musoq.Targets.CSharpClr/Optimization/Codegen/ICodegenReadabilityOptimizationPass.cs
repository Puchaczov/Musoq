using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

internal interface ICodegenReadabilityOptimizationPass : IPlanOptimizationPass<CompilationUnitSyntax>
{
}
