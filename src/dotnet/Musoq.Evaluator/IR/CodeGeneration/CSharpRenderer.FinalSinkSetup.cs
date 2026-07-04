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
        IReadOnlyList<StatementSyntax> SourceSetupStatements,
        ExecutionRenderContext RenderContext);

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

        using var renderingScope = executionRenderer.EnterTypedSinkRenderContext(plan);
        var renderContext = renderingScope.Context;
        var statements = new List<StatementSyntax>(leadingStatements);
        statements.AddRange(executionRenderer.CreateTypedSinkEntryStatements(plan, renderContext));
        foreach (var sourceScan in sinkPlan.SourceScans)
            statements.AddRange(executionRenderer.RenderSourceScanForTypedSink(sourceScan, renderContext));
        foreach (var setupNode in sinkPlan.SetupNodes)
            statements.AddRange(executionRenderer.RenderSetupNodeForTypedSink(setupNode, renderContext));

        var setup = new FinalSinkRenderSetup(
            sinkPlan,
            sinkPlan.ProjectionLoop!,
            statements,
            renderContext);
        method = createMethod(setup);
        metadata = sinkPlan.ResultMetadata;
        return true;
    }

    private static LocalDeclarationStatementSyntax CreateSourceRowsLocalDeclaration(
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            sourceRowsName,
            RenderTypedSinkExpression(executionRenderer, projectionLoop.SourceRows, renderContext));
    }

    private static ExpressionSyntax RenderTypedSinkExpression(
        ExecutionCSharpRenderer executionRenderer,
        ExecutionExpression expression,
        ExecutionRenderContext? renderContext = null) =>
        renderContext == null
            ? executionRenderer.RenderExpressionForTypedSink(expression)
            : executionRenderer.RenderExpressionForTypedSink(expression, renderContext);

    private static ParenthesizedLambdaExpressionSyntax RenderTypedSinkOptionalGeneratedRowProjection(
        ExecutionCSharpRenderer executionRenderer,
        ExecutionParallelFilterProjectLoop optionalProjectorLoop,
        ExecutionRenderContext? renderContext = null) =>
        renderContext == null
            ? executionRenderer.RenderOptionalGeneratedRowProjectionForTypedSink(optionalProjectorLoop)
            : executionRenderer.RenderOptionalGeneratedRowProjectionForTypedSink(optionalProjectorLoop, renderContext);

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
