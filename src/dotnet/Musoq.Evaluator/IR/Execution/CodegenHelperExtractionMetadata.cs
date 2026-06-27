using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

internal static class CodegenHelperExtractionMetadata
{
    public const string CandidateAnnotationKind = "Musoq.HelperExtractionCandidate";
    public const string CallSiteAnnotationKind = "Musoq.HelperExtractionCallSiteCandidate";
    public const string InlineCandidateAnnotationKind = "Musoq.HelperExtractionInlineCandidate";

    public static MethodDeclarationSyntax AnnotateCandidate(
        MethodDeclarationSyntax method,
        CodegenHelperExtractionRole role,
        string phaseBoundary,
        string mutationBoundary)
    {
        return method.WithAdditionalAnnotations(CreateAnnotation(
            CandidateAnnotationKind,
            role,
            method.Identifier.ValueText,
            CodegenHelperExtractionCandidateKind.ExistingHelper,
            new CodegenHelperExtractionBoundaries(
                phaseBoundary,
                mutationBoundary,
                "no-movement",
                "no-movement",
                "no-movement",
                "explicit",
                "existing-helper",
                method.Identifier.ValueText)));
    }

    public static MethodDeclarationSyntax AnnotateHelperExtractionCandidate(
        this MethodDeclarationSyntax method,
        CodegenHelperExtractionRole role)
    {
        return AnnotateCandidate(
            method,
            role,
            CreateBoundaries(role) with { OrderingKey = method.Identifier.ValueText });
    }

    public static IReadOnlyList<MemberDeclarationSyntax> AnnotateCandidateMembers(
        IEnumerable<MemberDeclarationSyntax> members)
    {
        return members.Select(AnnotateCandidateMember).ToArray();
    }

    public static MemberDeclarationSyntax AnnotateCandidateMember(MemberDeclarationSyntax member)
    {
        if (member is MethodDeclarationSyntax method &&
            TryInferRole(method.Identifier.ValueText, out var role))
        {
            return method.AnnotateHelperExtractionCandidate(role);
        }

        return member;
    }

    public static MethodDeclarationSyntax AnnotateHelperExtractionCandidate(
        this MethodDeclarationSyntax method,
        CodegenHelperExtractionRole role,
        string phaseBoundary,
        string mutationBoundary)
    {
        return AnnotateCandidate(method, role, phaseBoundary, mutationBoundary);
    }

    public static InvocationExpressionSyntax AnnotateCallSite(
        InvocationExpressionSyntax invocation,
        CodegenHelperExtractionRole role,
        string helperName)
    {
        return AnnotateCallSite(
            invocation,
            role,
            helperName,
            CreateBoundaries(role) with { OrderingKey = helperName });
    }

    public static InvocationExpressionSyntax AnnotateCallSite(
        InvocationExpressionSyntax invocation,
        string helperName)
    {
        return TryInferRole(helperName, out var role)
            ? AnnotateCallSite(invocation, role, helperName)
            : invocation;
    }

    public static InvocationExpressionSyntax AnnotateCallSite(
        InvocationExpressionSyntax invocation,
        CodegenHelperExtractionRole role,
        string helperName,
        string phaseBoundary,
        string mutationBoundary)
    {
        return invocation.WithAdditionalAnnotations(CreateAnnotation(
            CallSiteAnnotationKind,
            role,
            helperName,
            CodegenHelperExtractionCandidateKind.ExistingHelper,
            new CodegenHelperExtractionBoundaries(
                phaseBoundary,
                mutationBoundary,
                "no-movement",
                "no-movement",
                "no-movement",
                "explicit",
                "existing-helper",
                helperName)));
    }

    public static InvocationExpressionSyntax AnnotateCallSite(
        InvocationExpressionSyntax invocation,
        CodegenHelperExtractionRole role,
        string helperName,
        CodegenHelperExtractionBoundaries boundaries)
    {
        return invocation.WithAdditionalAnnotations(CreateAnnotation(
            CallSiteAnnotationKind,
            role,
            helperName,
            CodegenHelperExtractionCandidateKind.ExistingHelper,
            boundaries));
    }

    public static MethodDeclarationSyntax AnnotateCandidate(
        MethodDeclarationSyntax method,
        CodegenHelperExtractionRole role,
        CodegenHelperExtractionBoundaries boundaries)
    {
        return method.WithAdditionalAnnotations(CreateAnnotation(
            CandidateAnnotationKind,
            role,
            method.Identifier.ValueText,
            CodegenHelperExtractionCandidateKind.ExistingHelper,
            boundaries));
    }

    public static BlockSyntax AnnotateInlineCandidate(
        BlockSyntax block,
        CodegenHelperExtractionRole role,
        string helperName,
        CodegenHelperExtractionBoundaries boundaries)
    {
        return block.WithAdditionalAnnotations(CreateAnnotation(
            InlineCandidateAnnotationKind,
            role,
            helperName,
            CodegenHelperExtractionCandidateKind.InlineBlock,
            boundaries));
    }

    public static bool TryGetCandidate(SyntaxNode node, out CodegenHelperExtractionInfo info)
    {
        return TryGetInfo(node, CandidateAnnotationKind, out info);
    }

    public static bool TryGetCallSite(SyntaxNode node, out CodegenHelperExtractionInfo info)
    {
        return TryGetInfo(node, CallSiteAnnotationKind, out info);
    }

    public static bool TryGetInlineCandidate(SyntaxNode node, out CodegenHelperExtractionInfo info)
    {
        return TryGetInfo(node, InlineCandidateAnnotationKind, out info);
    }

    private static SyntaxAnnotation CreateAnnotation(
        string kind,
        CodegenHelperExtractionRole role,
        string helperName,
        CodegenHelperExtractionCandidateKind candidateKind,
        CodegenHelperExtractionBoundaries boundaries)
    {
        return new SyntaxAnnotation(
            kind,
            $"kind:{candidateKind};role:{role};helper:{helperName};phase:{boundaries.PhaseBoundary};mutation:{boundaries.MutationBoundary};cancellation:{boundaries.CancellationBoundary};progress:{boundaries.ProgressBoundary};queryStats:{boundaries.QueryStatisticsBoundary};captures:{boundaries.CaptureBoundary};return:{boundaries.ReturnBoundary};ordering:{boundaries.OrderingKey}");
    }

    private static bool TryInferRole(string helperName, out CodegenHelperExtractionRole role)
    {
        if (helperName.StartsWith("BuildCte", StringComparison.Ordinal))
        {
            role = CodegenHelperExtractionRole.StoredTableBuild;
            return true;
        }

        if (helperName.StartsWith("Build", StringComparison.Ordinal) &&
            helperName.EndsWith("Sorted", StringComparison.Ordinal))
        {
            role = CodegenHelperExtractionRole.WindowSortedCopy;
            return true;
        }

        if (helperName.StartsWith("Build", StringComparison.Ordinal) &&
            helperName.Contains("Hash", StringComparison.Ordinal))
        {
            role = CodegenHelperExtractionRole.HashJoinBuild;
            return true;
        }

        if (helperName.StartsWith("Append", StringComparison.Ordinal) &&
            (helperName.Contains("Hash", StringComparison.Ordinal) ||
             helperName.EndsWith("JoinRows", StringComparison.Ordinal)))
        {
            role = CodegenHelperExtractionRole.HashJoinProbe;
            return true;
        }

        if (helperName.StartsWith("Build", StringComparison.Ordinal) &&
            helperName.Contains("Keys", StringComparison.Ordinal))
        {
            role = CodegenHelperExtractionRole.KeySetBuild;
            return true;
        }

        if (helperName.StartsWith("Extract", StringComparison.Ordinal) &&
            helperName.EndsWith("WindowKeys", StringComparison.Ordinal))
        {
            role = CodegenHelperExtractionRole.WindowRankingKeyExtraction;
            return true;
        }

        if (helperName.StartsWith("Populate", StringComparison.Ordinal))
        {
            role = CodegenHelperExtractionRole.AggregatePopulate;
            return true;
        }

        if (helperName.StartsWith("Finalize", StringComparison.Ordinal))
        {
            role = CodegenHelperExtractionRole.AggregateFinalize;
            return true;
        }

        role = default;
        return false;
    }

    internal static CodegenHelperExtractionBoundaries CreateBoundaries(
        CodegenHelperExtractionRole role)
    {
        var (phaseBoundary, mutationBoundary) = role switch
        {
            CodegenHelperExtractionRole.StoredTableBuild => ("same-phase", "returns-table"),
            CodegenHelperExtractionRole.HashJoinBuild => ("same-phase", "hash-target"),
            CodegenHelperExtractionRole.HashJoinProbe => ("same-phase", "hash-and-append-targets"),
            CodegenHelperExtractionRole.KeySetBuild => ("same-phase", "keyset-target"),
            CodegenHelperExtractionRole.KeySetProbe => ("same-phase", "keyset-and-append-targets"),
            CodegenHelperExtractionRole.WindowAppendRows => ("window-boundary", "window-buffer-and-table-targets"),
            CodegenHelperExtractionRole.WindowRankingKeyExtraction => ("window-boundary", "window-key-targets"),
            CodegenHelperExtractionRole.WindowSortedCopy => ("window-boundary", "returns-table"),
            CodegenHelperExtractionRole.AggregatePopulate => ("same-phase", "aggregate-context"),
            CodegenHelperExtractionRole.AggregateFinalize => ("same-phase", "aggregate-result-table"),
            _ => ("same-phase", "explicit-targets")
        };

        return new CodegenHelperExtractionBoundaries(
            phaseBoundary,
            mutationBoundary,
            "no-movement",
            "no-movement",
            "no-movement",
            "explicit",
            "existing-helper",
            role.ToString());
    }

    private static bool TryGetInfo(
        SyntaxNode node,
        string kind,
        out CodegenHelperExtractionInfo info)
    {
        var data = node.GetAnnotations(kind).FirstOrDefault()?.Data;
        if (data == null)
        {
            info = null!;
            return false;
        }

        var values = ParseData(data);
        if (!values.TryGetValue("role", out var roleText) ||
            !Enum.TryParse<CodegenHelperExtractionRole>(roleText, out var role) ||
            !values.TryGetValue("helper", out var helperName) ||
            !values.TryGetValue("phase", out var phaseBoundary) ||
            !values.TryGetValue("mutation", out var mutationBoundary))
        {
            info = null!;
            return false;
        }

        var candidateKind = values.TryGetValue("kind", out var kindText) &&
                            Enum.TryParse<CodegenHelperExtractionCandidateKind>(kindText, out var parsedKind)
            ? parsedKind
            : CodegenHelperExtractionCandidateKind.ExistingHelper;

        info = new CodegenHelperExtractionInfo(
            role,
            helperName,
            candidateKind,
            phaseBoundary,
            mutationBoundary,
            values.GetValueOrDefault("cancellation", "no-movement"),
            values.GetValueOrDefault("progress", "no-movement"),
            values.GetValueOrDefault("queryStats", "no-movement"),
            values.GetValueOrDefault("captures", "explicit"),
            values.GetValueOrDefault("return", "existing-helper"),
            values.GetValueOrDefault("ordering", helperName));
        return true;
    }

    private static Dictionary<string, string> ParseData(string data)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
                continue;

            values[part[..separator]] = part[(separator + 1)..];
        }

        return values;
    }
}
