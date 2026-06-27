using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class ControlFlowNormalizationPass : IPlanOptimizationPass<CompilationUnitSyntax>
{
    public string Name => "ControlFlowNormalization";

    public OptimizationResult<CompilationUnitSyntax> Optimize(CompilationUnitSyntax plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var rewriter = new ControlFlowNormalizationRewriter();
        var optimized = (CompilationUnitSyntax)rewriter.Visit(plan)!;

        if (rewriter.RemovedEmptyStatements == 0)
        {
            return OptimizationResult<CompilationUnitSyntax>.NoChange(
                plan,
                "No empty block statements were safe to remove.");
        }

        return OptimizationResult<CompilationUnitSyntax>.Changed(
            optimized,
            $"Removed {rewriter.RemovedEmptyStatements} empty block statement(s).");
    }

    private sealed class ControlFlowNormalizationRewriter : CSharpSyntaxRewriter
    {
        public int RemovedEmptyStatements { get; private set; }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var visited = (BlockSyntax)base.VisitBlock(node)!;
            var statements = visited.Statements;
            var normalized = statements
                .Where(statement => statement is not EmptyStatementSyntax)
                .ToArray();

            if (normalized.Length == statements.Count)
                return visited;

            RemovedEmptyStatements += statements.Count - normalized.Length;
            return visited.WithStatements(SyntaxFactory.List(normalized));
        }
    }
}
