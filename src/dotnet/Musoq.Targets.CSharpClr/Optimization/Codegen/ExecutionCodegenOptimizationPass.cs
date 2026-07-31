using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

/// <summary>
/// Performs the execution-only cleanup and approved helper processing in one
/// syntax traversal. Stable artifact rendering keeps the individual passes so
/// its diagnostics and readability trace remain unchanged.
/// </summary>
internal sealed class ExecutionCodegenOptimizationPass : ICodegenReadabilityOptimizationPass
{
    public string Name => "ExecutionCodegenOptimization";

    public OptimizationResult<CompilationUnitSyntax> Optimize(
        CompilationUnitSyntax plan,
        OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var rewriter = new ExecutionCodegenOptimizationRewriter();
        var optimized = (CompilationUnitSyntax)rewriter.Visit(plan)!;
        var changed = rewriter.RemovedDeclarations > 0 ||
                      rewriter.ApprovedHelpers > 0 ||
                      rewriter.ApprovedCallSites > 0 ||
                      rewriter.ExtractedInlineBlocks > 0;

        var reason = $"Removed {rewriter.RemovedDeclarations} safe temporary declaration(s), " +
                     $"approved {rewriter.ApprovedHelpers} helper(s) and {rewriter.ApprovedCallSites} call site(s), " +
                     $"and extracted {rewriter.ExtractedInlineBlocks} inline helper block(s); " +
                     $"skipped {rewriter.SkippedDeclarations} declaration(s) and " +
                     $"{rewriter.SkippedInlineBlocks} inline block(s).";

        return changed
            ? OptimizationResult<CompilationUnitSyntax>.Changed(optimized, reason)
            : OptimizationResult<CompilationUnitSyntax>.NoChange(plan, reason);
    }

    private sealed class ExecutionCodegenOptimizationRewriter : CSharpSyntaxRewriter
    {
        private readonly HelperExtractionReadabilityApproval _approval = new();

        public int RemovedDeclarations { get; private set; }

        public int SkippedDeclarations { get; private set; }

        public int ApprovedHelpers { get; private set; }

        public int ApprovedCallSites { get; private set; }

        public int ExtractedInlineBlocks { get; private set; }

        public int SkippedInlineBlocks { get; private set; }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _approval.EnterClass(node);
            var visited = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
            var extractedHelpers = _approval.LeaveClass();

            return extractedHelpers.Count == 0
                ? visited
                : visited.WithMembers(visited.Members.AddRange(extractedHelpers));
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
            if (!CodegenHelperExtractionMetadata.TryGetCandidate(visited, out var info) ||
                visited.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind))
            {
                return visited;
            }

            ApprovedHelpers++;
            return visited.WithAdditionalAnnotations(_approval.CreateHelperAnnotation(info));
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
            if (!CodegenHelperExtractionMetadata.TryGetCallSite(visited, out var info) ||
                visited.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind))
            {
                return visited;
            }

            ApprovedCallSites++;
            return visited.WithAdditionalAnnotations(_approval.CreateHelperCallAnnotation(info));
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var visited = (BlockSyntax)base.VisitBlock(node)!;
            var statements = visited.Statements;
            var removed = RemoveUnusedDeclarations(statements);
            var rewritten = removed.Count == 0
                ? visited
                : visited.WithStatements(SyntaxFactory.List(
                    statements.Where((_, index) => !removed.Contains(index))));

            if (!CodegenHelperExtractionMetadata.TryGetInlineCandidate(rewritten, out var info) ||
                rewritten.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind))
            {
                return rewritten;
            }

            if (!_approval.TryExtractInlineBlock(rewritten, info, out var extracted))
            {
                SkippedInlineBlocks++;
                return rewritten;
            }

            ExtractedInlineBlocks++;
            return extracted;
        }

        private HashSet<int> RemoveUnusedDeclarations(SyntaxList<StatementSyntax> statements)
        {
            var removed = new HashSet<int>();
            var usedLater = new HashSet<string>(StringComparer.Ordinal);

            for (var index = statements.Count - 1; index >= 0; index--)
            {
                var statement = statements[index];
                if (statement is LocalDeclarationStatementSyntax local &&
                    CanRemove(local, usedLater))
                {
                    removed.Add(index);
                    RemovedDeclarations++;
                }

                foreach (var identifier in statement.DescendantNodes().OfType<IdentifierNameSyntax>())
                    usedLater.Add(identifier.Identifier.ValueText);
            }

            return removed;
        }

        private bool CanRemove(
            LocalDeclarationStatementSyntax local,
            HashSet<string> usedLater)
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

            if (variableNames.Length != local.Declaration.Variables.Count ||
                local.Declaration.Variables.Any(static variable =>
                    !CodegenReadabilitySyntaxFacts.IsSafelyRemovableInitializer(variable.Initializer)) ||
                variableNames.Any(usedLater.Contains))
            {
                SkippedDeclarations++;
                return false;
            }

            return true;
        }
    }
}
