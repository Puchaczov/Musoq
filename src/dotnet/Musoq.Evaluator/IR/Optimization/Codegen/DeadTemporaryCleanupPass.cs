using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Codegen;

internal sealed class DeadTemporaryCleanupPass : ICodegenReadabilityOptimizationPass
{
    public string Name => "DeadTemporaryCleanup";

    public OptimizationResult<CompilationUnitSyntax> Optimize(CompilationUnitSyntax plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var rewriter = new DeadTemporaryCleanupRewriter();
        var optimized = (CompilationUnitSyntax)rewriter.Visit(plan)!;

        if (rewriter.RemovedDeclarations == 0)
        {
            return OptimizationResult<CompilationUnitSyntax>.NoChange(
                plan,
                $"No unused literal/default local declaration was safe to remove; skipped {rewriter.SkippedDeclarations} local declaration(s).");
        }

        return OptimizationResult<CompilationUnitSyntax>.Changed(
            optimized,
            $"Removed {rewriter.RemovedDeclarations} unused literal/default local declaration(s); skipped {rewriter.SkippedDeclarations} local declaration(s).");
    }

    private sealed class DeadTemporaryCleanupRewriter : CSharpSyntaxRewriter
    {
        public int RemovedDeclarations { get; private set; }

        public int SkippedDeclarations { get; private set; }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var visited = (BlockSyntax)base.VisitBlock(node)!;
            var statements = visited.Statements;
            var rewritten = new List<StatementSyntax>(statements.Count);

            for (var index = 0; index < statements.Count; index++)
            {
                var statement = statements[index];
                if (statement is LocalDeclarationStatementSyntax local && CanRemove(local, statements, index))
                {
                    RemovedDeclarations++;
                    continue;
                }

                rewritten.Add(statement);
            }

            if (rewritten.Count == statements.Count)
                return visited;

            return visited.WithStatements(SyntaxFactory.List(rewritten));
        }

        private bool CanRemove(
            LocalDeclarationStatementSyntax local,
            SyntaxList<StatementSyntax> blockStatements,
            int statementIndex)
        {
            if (local.UsingKeyword.RawKind != 0 ||
                local.AwaitKeyword.RawKind != 0 ||
                local.Declaration.Variables.Count == 0)
            {
                SkippedDeclarations++;
                return false;
            }

            var variableNames = local.Declaration.Variables
                .Select(static variable => variable.Identifier.ValueText)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

            if (variableNames.Length != local.Declaration.Variables.Count)
            {
                SkippedDeclarations++;
                return false;
            }

            if (local.Declaration.Variables.Any(static variable =>
                    !CodegenReadabilitySyntaxFacts.IsSafelyRemovableInitializer(variable.Initializer)))
            {
                SkippedDeclarations++;
                return false;
            }

            if (IsUsedLater(variableNames, blockStatements, statementIndex))
            {
                SkippedDeclarations++;
                return false;
            }

            return true;
        }

        private static bool IsUsedLater(
            IReadOnlyCollection<string> variableNames,
            SyntaxList<StatementSyntax> blockStatements,
            int statementIndex)
        {
            for (var index = statementIndex + 1; index < blockStatements.Count; index++)
            {
                var identifiers = blockStatements[index]
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>();

                if (identifiers.Any(identifier => variableNames.Contains(identifier.Identifier.ValueText)))
                    return true;
            }

            return false;
        }
    }
}

