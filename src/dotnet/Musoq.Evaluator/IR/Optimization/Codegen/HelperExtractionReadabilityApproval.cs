using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Codegen;

internal sealed class HelperExtractionReadabilityApproval
{
    public const string AnnotationKind = "Musoq.HelperExtractionReadability";

    private readonly Stack<ClassExtractionState> _classStates = new();

    public void EnterClass(ClassDeclarationSyntax node)
    {
        _classStates.Push(new ClassExtractionState(node.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(static method => method.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal)));
    }

    public IReadOnlyList<MemberDeclarationSyntax> LeaveClass()
    {
        return _classStates.Pop().ExtractedHelpers;
    }

    public SyntaxAnnotation CreateHelperAnnotation(CodegenHelperExtractionInfo info)
    {
        return new SyntaxAnnotation(
            AnnotationKind,
            $"helper:{info.HelperName};role:{info.Role};phase:{info.PhaseBoundary};mutation:{info.MutationBoundary};progress:safe;captures:metadata-owned;queryStats:preserved");
    }

    public SyntaxAnnotation CreateHelperCallAnnotation(CodegenHelperExtractionInfo info)
    {
        return new SyntaxAnnotation(
            AnnotationKind,
            $"call:{info.HelperName};role:{info.Role};phase:{info.PhaseBoundary};mutation:{info.MutationBoundary};progress:safe;captures:metadata-owned;queryStats:preserved");
    }

    public bool TryExtractInlineBlock(
        BlockSyntax block,
        CodegenHelperExtractionInfo info,
        out BlockSyntax rewritten)
    {
        rewritten = block;
        if (_classStates.Count == 0 ||
            info.CandidateKind != CodegenHelperExtractionCandidateKind.InlineBlock ||
            !string.Equals(info.CaptureBoundary, "none", StringComparison.Ordinal) ||
            !string.Equals(info.ReturnBoundary, "void", StringComparison.Ordinal) ||
            !SyntaxFacts.IsValidIdentifier(info.HelperName) ||
            ContainsLifecycleInvocation(block) ||
            ContainsUnsupportedControlFlow(block))
        {
            return false;
        }

        var state = _classStates.Peek();
        var helperName = state.ReserveHelperName(info.HelperName);
        var helperInfo = info with { HelperName = helperName, OrderingKey = helperName };
        state.ExtractedHelpers.Add(CreateInlineHelper(helperName, block, helperInfo));

        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(helperName));
        var statement = SyntaxFactory.ExpressionStatement(invocation.WithAdditionalAnnotations(
            CreateHelperCallAnnotation(helperInfo)));
        rewritten = SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(statement))
            .WithTriviaFrom(block);
        return true;
    }

    private MethodDeclarationSyntax CreateInlineHelper(
        string helperName,
        BlockSyntax block,
        CodegenHelperExtractionInfo info)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helperName)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithBody(RemoveInlineCandidateAnnotations(block))
            .WithAdditionalAnnotations(CreateHelperAnnotation(info));
    }

    private static BlockSyntax RemoveInlineCandidateAnnotations(BlockSyntax block)
    {
        var annotations = block
            .GetAnnotations(CodegenHelperExtractionMetadata.InlineCandidateAnnotationKind)
            .Concat(block.GetAnnotations(AnnotationKind))
            .ToArray();

        return annotations.Length == 0 ? block : block.WithoutAnnotations(annotations);
    }

    private static bool ContainsLifecycleInvocation(BlockSyntax block)
    {
        return block
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(CodegenReadabilitySyntaxFacts.TryGetInvocationName)
            .OfType<string>()
            .Any(CodegenReadabilitySyntaxFacts.IsLifecycleMethodName);
    }

    private static bool ContainsUnsupportedControlFlow(BlockSyntax block)
    {
        return block.DescendantNodes().Any(static node => node is
            ReturnStatementSyntax or
            YieldStatementSyntax or
            BreakStatementSyntax or
            ContinueStatementSyntax or
            GotoStatementSyntax or
            ThrowStatementSyntax);
    }

    private sealed class ClassExtractionState(HashSet<string> methodNames)
    {
        public List<MemberDeclarationSyntax> ExtractedHelpers { get; } = [];

        public string ReserveHelperName(string requestedName)
        {
            if (methodNames.Add(requestedName))
                return requestedName;

            for (var index = 1;; index++)
            {
                var candidate = $"{requestedName}_{index}";
                if (methodNames.Add(candidate))
                    return candidate;
            }
        }
    }
}

