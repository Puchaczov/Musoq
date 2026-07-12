using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

internal sealed class HelperExtractionReadabilityPass : ICodegenReadabilityOptimizationPass
{
    public const string HelperExtractionAnnotationKind = HelperExtractionReadabilityApproval.AnnotationKind;

    public string Name => "HelperExtractionReadability";

    public OptimizationResult<CompilationUnitSyntax> Optimize(CompilationUnitSyntax plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var rewriter = new HelperExtractionCandidateRewriter();
        var optimized = (CompilationUnitSyntax)rewriter.Visit(plan)!;
        var classes = plan.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
        var helperCount = 0;
        var linkedHelperCount = 0;
        var helperCallSiteCount = 0;
        var candidateHelperCount = plan
            .DescendantNodes()
            .Count(static node => CodegenHelperExtractionMetadata.TryGetCandidate(node, out _));
        var candidateCallSiteCount = plan
            .DescendantNodes()
            .Count(static node => CodegenHelperExtractionMetadata.TryGetCallSite(node, out _));
        var inlineCandidateCount = plan
            .DescendantNodes()
            .Count(static node => CodegenHelperExtractionMetadata.TryGetInlineCandidate(node, out _));

        foreach (var @class in classes)
        {
            var methods = @class.Members.OfType<MethodDeclarationSyntax>().ToArray();
            var helperNames = methods
                .Where(CodegenReadabilitySyntaxFacts.IsPrivateStaticHelper)
                .Select(static method => method.Identifier.ValueText)
                .ToHashSet(StringComparer.Ordinal);

            if (helperNames.Count == 0)
                continue;

            helperCount += helperNames.Count;
            var callSites = methods
                .Where(static method => !CodegenReadabilitySyntaxFacts.IsLifecycleMethodName(method.Identifier.ValueText))
                .SelectMany(CodegenReadabilitySyntaxFacts.CollectInvocationNames)
                .Where(helperNames.Contains)
                .ToArray();

            linkedHelperCount += callSites.Distinct(StringComparer.Ordinal).Count();
            helperCallSiteCount += callSites.Length;
        }

        if (rewriter.ApprovedHelpers > 0 ||
            rewriter.ApprovedCallSites > 0 ||
            rewriter.ExtractedInlineBlocks > 0)
        {
            return OptimizationResult<CompilationUnitSyntax>.Changed(
                optimized,
                $"Approved {rewriter.ApprovedHelpers} metadata-backed helper extraction candidate(s), {rewriter.ApprovedCallSites} candidate call site(s), and extracted {rewriter.ExtractedInlineBlocks} metadata-approved inline helper block(s) for readability-owned helper extraction; observed {candidateHelperCount} explicit candidate helper(s), {candidateCallSiteCount} candidate call site(s), {inlineCandidateCount} inline candidate block(s), {rewriter.SkippedInlineBlocks} skipped inline candidate block(s), and {linkedHelperCount} linked helper(s).");
        }

        if (candidateHelperCount == 0 && candidateCallSiteCount == 0 && inlineCandidateCount == 0)
        {
            return OptimizationResult<CompilationUnitSyntax>.NoChange(
                plan,
                helperCount == 0
                    ? "No metadata-approved helper extraction candidates were found."
                    : $"Observed {helperCount} renderer-extracted private static helper method(s), but none carried helper extraction candidate metadata.");
        }

        return OptimizationResult<CompilationUnitSyntax>.NoChange(
            plan,
            $"Observed {helperCount} renderer-extracted private static helper method(s), {linkedHelperCount} helper(s) referenced from generated methods, {helperCallSiteCount} helper call site(s), {candidateHelperCount} explicit candidate helper(s), {candidateCallSiteCount} candidate call site(s), {inlineCandidateCount} inline candidate block(s), and {rewriter.SkippedInlineBlocks} skipped inline candidate block(s); metadata-approved helper extraction annotations were already present or unsafe to apply.");
    }

    private sealed class HelperExtractionCandidateRewriter : CSharpSyntaxRewriter
    {
        private readonly HelperExtractionReadabilityApproval _approval = new();

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
                visited.HasAnnotations(HelperExtractionAnnotationKind))
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
                visited.HasAnnotations(HelperExtractionAnnotationKind))
            {
                return visited;
            }

            ApprovedCallSites++;
            return visited.WithAdditionalAnnotations(_approval.CreateHelperCallAnnotation(info));
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var visited = (BlockSyntax)base.VisitBlock(node)!;
            if (!CodegenHelperExtractionMetadata.TryGetInlineCandidate(visited, out var info) ||
                visited.HasAnnotations(HelperExtractionAnnotationKind))
            {
                return visited;
            }

            if (!_approval.TryExtractInlineBlock(visited, info, out var rewritten))
            {
                SkippedInlineBlocks++;
                return visited;
            }

            ExtractedInlineBlocks++;
            return rewritten;
        }
    }
}

