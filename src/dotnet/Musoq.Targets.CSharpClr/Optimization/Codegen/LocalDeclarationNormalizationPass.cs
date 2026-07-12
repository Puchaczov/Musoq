using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

internal sealed class LocalDeclarationNormalizationPass : ICodegenReadabilityOptimizationPass
{
    public string Name => "LocalDeclarationNormalization";

    public OptimizationResult<CompilationUnitSyntax> Optimize(CompilationUnitSyntax plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var rewriter = new LocalDeclarationNormalizationRewriter();
        var optimized = (CompilationUnitSyntax)rewriter.Visit(plan)!;

        if (rewriter.SplitDeclarations == 0)
        {
            return OptimizationResult<CompilationUnitSyntax>.NoChange(
                plan,
                "No multi-variable local declarations were safe to split.");
        }

        return OptimizationResult<CompilationUnitSyntax>.Changed(
            optimized,
            $"Split {rewriter.SplitDeclarations} multi-variable local declaration(s).");
    }

    private sealed class LocalDeclarationNormalizationRewriter : CSharpSyntaxRewriter
    {
        public int SplitDeclarations { get; private set; }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var visited = (BlockSyntax)base.VisitBlock(node)!;
            var statements = visited.Statements;
            var normalized = statements.SelectMany(SplitIfSafe).ToArray();

            if (normalized.Length == statements.Count && normalized.SequenceEqual(statements))
                return visited;

            return visited.WithStatements(SyntaxFactory.List(normalized));
        }

        private IEnumerable<StatementSyntax> SplitIfSafe(StatementSyntax statement)
        {
            if (statement is not LocalDeclarationStatementSyntax local ||
                local.Declaration.Variables.Count <= 1 ||
                !CanSplit(local))
            {
                yield return statement;
                yield break;
            }

            SplitDeclarations++;
            var variables = local.Declaration.Variables;
            for (var index = 0; index < variables.Count; index++)
            {
                var variable = variables[index];
                var split = local
                    .WithDeclaration(local.Declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(variable)))
                    .WithLeadingTrivia(index == 0 ? local.GetLeadingTrivia() : SyntaxFactory.TriviaList());

                yield return split;
            }
        }

        private static bool CanSplit(LocalDeclarationStatementSyntax local)
        {
            return local.UsingKeyword.RawKind == 0 &&
                   local.AwaitKeyword.RawKind == 0 &&
                   local.AttributeLists.Count == 0;
        }
    }
}

