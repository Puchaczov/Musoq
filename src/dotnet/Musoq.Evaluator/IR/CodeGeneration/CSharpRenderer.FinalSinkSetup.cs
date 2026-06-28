using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using ExecutionCSharpRenderer = Musoq.Evaluator.IR.Execution.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.IR.CodeGeneration;

public sealed partial class CSharpRenderer
{
    private sealed record FinalSinkRenderSetup(
        FinalProjectionSinkPlan SinkPlan,
        TypedProjectionLoop ProjectionLoop,
        IReadOnlyList<StatementSyntax> SourceSetupStatements);

    private static bool TryCreateFinalSinkMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        FinalProjectionSinkPlan sinkPlan,
        IEnumerable<StatementSyntax> leadingStatements,
        Func<FinalSinkRenderSetup, MethodDeclarationSyntax> createMethod,
        out MethodDeclarationSyntax method,
        out QueryMethodRenderMetadata metadata)
    {
        method = null!;
        metadata = default;
        if (!sinkPlan.IsAccepted)
        {
            metadata = CreateMaterializedTableRowsMetadata(sinkPlan);
            return false;
        }

        using var renderingScope = executionRenderer.EnterTypedSinkRendering(plan);
        var statements = new List<StatementSyntax>(leadingStatements);
        statements.AddRange(executionRenderer.CreateTypedSinkEntryStatements(plan));
        foreach (var sourceScan in sinkPlan.SourceScans)
            statements.AddRange(executionRenderer.RenderSourceScanForTypedSink(sourceScan));
        foreach (var setupNode in sinkPlan.SetupNodes)
            statements.AddRange(executionRenderer.RenderSetupNodeForTypedSink(setupNode));

        var setup = new FinalSinkRenderSetup(
            sinkPlan,
            sinkPlan.ProjectionLoop!,
            statements);
        method = createMethod(setup);
        metadata = sinkPlan.ResultMetadata;
        return true;
    }

    private static LocalDeclarationStatementSyntax CreateSourceRowsLocalDeclaration(
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            sourceRowsName,
            executionRenderer.RenderExpressionForTypedSink(projectionLoop.SourceRows));
    }

    private static LocalDeclarationStatementSyntax CreateParallelRowsProbeDeclaration(
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        string parallelRowsName)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            parallelRowsName,
            CreateGetParallelRowsInvocation(projectionLoop, sourceRowsName));
    }

    private static bool CanUseChunkedParallelProjection(TypedProjectionLoop projectionLoop)
    {
        return ExecutionRowStreams.IsChunked(projectionLoop.SourceRows);
    }

    private static ExpressionSyntax CreateStreamingChunkedRowsCondition(
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        var sourceType = CreateSourceTypeSyntax(projectionLoop.Source);
        var reusableChunksType = SyntaxFactory.GenericName(nameof(IReadOnlyList<object>))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                SyntaxFactory.GenericName(nameof(IReadOnlyList<object>))
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(sourceType))))));

        return SyntaxFactory.IsPatternExpression(
            SyntaxFactory.IdentifierName(sourceRowsName),
            SyntaxFactory.UnaryPattern(SyntaxFactory.DeclarationPattern(
                reusableChunksType,
                SyntaxFactory.DiscardDesignation())));
    }

    private static BinaryExpressionSyntax CreateParallelRowsAvailableCondition(string parallelRowsName)
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.GreaterThanExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(parallelRowsName),
                SyntaxFactory.IdentifierName(nameof(IReadOnlyCollection<object>.Count))),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
    }
}
