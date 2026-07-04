using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Optimization;

internal interface IPlanOptimizationPass<TPlan>
{
    string Name { get; }

    OptimizationResult<TPlan> Optimize(TPlan plan, OptimizationContext context);
}

internal interface IPreLogicalNormalizationPass : IPlanOptimizationPass<RootNode>
{
}

internal interface ILogicalNormalizationPass : IPlanOptimizationPass<LogicalNode>
{
}

internal interface ILogicalOptimizationPass : IPlanOptimizationPass<LogicalNode>
{
}

internal interface IPhysicalOptimizationPass : IPlanOptimizationPass<PhysicalNode>
{
}

internal interface IExecutionIrOptimizationPass : IPlanOptimizationPass<ExecutionPlan>
{
}

internal interface ICodegenReadabilityOptimizationPass : IPlanOptimizationPass<CompilationUnitSyntax>
{
}
