using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderForEachStream(
        ExecutionForEach forEach,
        ExecutionRenderContext context)
    {
        if (ExecutionRowStreams.IsScalar(forEach.Source))
            return RenderScalarForEach(forEach, context);

        return ExecutionRowStreams.IsChunked(forEach.Source)
            ? RenderChunkedForEach(forEach, context)
            : RenderForEach(forEach, context);
    }

    private IEnumerable<StatementSyntax> RenderForEachWithOrdinalityStream(
        ExecutionForEachWithOrdinality forEach,
        ExecutionRenderContext context)
    {
        if (ExecutionRowStreams.IsScalar(forEach.Source))
            return [RenderScalarForEachWithOrdinality(forEach, context)];

        return ExecutionRowStreams.IsChunked(forEach.Source)
            ? RenderChunkedForEachWithOrdinality(forEach, context)
            : RenderForEachWithOrdinality(forEach, context);
    }

    private StatementSyntax RenderMaterializeListStream(
        ExecutionMaterializeList materialize,
        ExecutionRenderContext context)
    {
        return ExecutionRowStreams.IsChunked(materialize.Source)
            ? RenderMaterializeChunkedList(materialize, context)
            : RenderMaterializeList(materialize, context);
    }

    private IEnumerable<StatementSyntax> RenderMaterializeFilteredListStream(
        ExecutionMaterializeFilteredList materialize,
        ExecutionRenderContext context)
    {
        return ExecutionRowStreams.IsChunked(materialize.Source)
            ? RenderMaterializeFilteredChunkedList(materialize, context)
            : RenderMaterializeFilteredList(materialize, context);
    }

    private IEnumerable<StatementSyntax> RenderMaterializeExpandoListStream(
        ExecutionMaterializeExpandoList materialize,
        ExecutionRenderContext context)
    {
        return ExecutionRowStreams.IsChunked(materialize.Source)
            ? RenderMaterializeChunkedExpandoList(materialize, context)
            : RenderMaterializeExpandoList(materialize, context);
    }

    private StatementSyntax[] RenderSourceLoopStream(ExecutionSourceLoop sourceLoop)
    {
        var context = CreateIsolatedRenderContext();
        return sourceLoop switch
        {
            ExecutionForEach forEach => RenderForEachStream(forEach, context).ToArray(),
            _ => throw new InvalidOperationException(
                $"Source loop renderer cannot render loop '{sourceLoop.GetType().Name}'.")
        };
    }

    private IEnumerable<StatementSyntax> RenderChunkedForEach(
        ExecutionForEach forEach,
        ExecutionRenderContext context)
    {
        yield return RenderChunkedForEachCore(forEach.Item, forEach.Source, forEach.Body, forEach, context);
    }

    private IEnumerable<StatementSyntax> RenderScalarForEach(
        ExecutionForEach forEach,
        ExecutionRenderContext context)
    {
        yield return RenderScalarForEachCore(forEach.Item, forEach.Source, forEach.Body, forEach, context);
    }

    private StatementSyntax RenderScalarForEachWithOrdinality(
        ExecutionForEachWithOrdinality forEach,
        ExecutionRenderContext context)
    {
        return StatementEmitter.CreateBlock(
            CreateLocalDeclaration(
                CreateTypeSyntax(typeof(int)),
                forEach.Ordinal.Name,
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0))),
            RenderScalarForEachCore(forEach.Item, forEach.Source, forEach.Body, forEach, context));
    }

    private StatementSyntax RenderScalarForEachCore(
        ExecutionVariable item,
        ExecutionExpression source,
        ExecutionBlock body,
        ExecutionNode operatorNode,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var sourceExpression = RenderExpression(source, context);
        var bodyStatements = new List<StatementSyntax>();

        if (session.EmitChunkLoopCancellationChecks)
            bodyStatements.Add(QueryEmitter.GenerateCancellationCheck());

        bodyStatements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            item.Name,
            sourceExpression));
        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabledFor(context), session.OperatorCatalog, operatorNode));
        bodyStatements.AddRange(RenderBlock(body, context).Statements);

        if (operatorNode is ExecutionForEachWithOrdinality ordinality)
        {
            bodyStatements.Add(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.PreIncrementExpression,
                    SyntaxFactory.IdentifierName(ordinality.Ordinal.Name))));
        }

        return StatementEmitter.CreateIf(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                sourceExpression,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            StatementEmitter.CreateBlock(bodyStatements));
    }

    private IEnumerable<StatementSyntax> RenderChunkedForEachWithOrdinality(
        ExecutionForEachWithOrdinality forEach,
        ExecutionRenderContext context)
    {
        yield return RenderChunkedForEachWithOrdinalityCore(forEach.Item, forEach.Source, forEach.Ordinal, forEach.Body, forEach, context);
    }

    private StatementSyntax RenderChunkedForEachCore(
        ExecutionVariable item,
        ExecutionExpression source,
        ExecutionBlock body,
        ExecutionNode operatorNode,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        return CreateChunkedLoop(
            item,
            source,
            context,
            (itemAccessExpression, indexVariableName) =>
            {
                var bodyStatements = CreateChunkedLoopBodyPrefix(item, itemAccessExpression, indexVariableName, context);
                bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabledFor(context), session.OperatorCatalog, operatorNode));
                bodyStatements.AddRange(RenderBlock(body, context).Statements);
                return bodyStatements;
            });
    }

    private StatementSyntax RenderChunkedForEachWithOrdinalityCore(
        ExecutionVariable item,
        ExecutionExpression source,
        ExecutionVariable ordinal,
        ExecutionBlock body,
        ExecutionNode operatorNode,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        return StatementEmitter.CreateBlock(
            CreateLocalDeclaration(
                CreateTypeSyntax(typeof(int)),
                ordinal.Name,
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0))),
            CreateChunkedLoop(
                item,
                source,
                context,
                (itemAccessExpression, indexVariableName) =>
                {
                    var bodyStatements = CreateChunkedLoopBodyPrefix(item, itemAccessExpression, indexVariableName, context);
                    bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabledFor(context), session.OperatorCatalog, operatorNode));
                    bodyStatements.AddRange(RenderBlock(body, context).Statements);
                    bodyStatements.Add(SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.PrefixUnaryExpression(
                            SyntaxKind.PreIncrementExpression,
                            SyntaxFactory.IdentifierName(ordinal.Name))));
                    return bodyStatements;
                }));
    }

    private StatementSyntax CreateChunkedLoop(
        ExecutionVariable item,
        ExecutionExpression source,
        ExecutionRenderContext context,
        Func<ExpressionSyntax, string, List<StatementSyntax>> createBodyStatements)
    {
        return ChunkedLoopSyntaxFactory.Create(
            item,
            RenderExpression(source, context),
            createBodyStatements);
    }

    private List<StatementSyntax> CreateChunkedLoopBodyPrefix(
        ExecutionVariable item,
        ExpressionSyntax itemAccessExpression,
        string indexVariableName,
        ExecutionRenderContext context)
    {
        var bodyStatements = new List<StatementSyntax>();

        if (context.Session.EmitChunkLoopCancellationChecks)
            bodyStatements.Add(CreatePeriodicCancellationCheck(
                indexVariableName,
                context.Session.SkipInitialLoopCancellationCheck));

        bodyStatements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            item.Name,
            itemAccessExpression));

        return bodyStatements;
    }

    private static IfStatementSyntax CreatePeriodicCancellationCheck(
        string indexVariableName,
        bool skipInitial = false,
        int mask = 1023)
    {
        ExpressionSyntax condition = SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.BitwiseAndExpression,
                        SyntaxFactory.IdentifierName(indexVariableName),
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(mask)))),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0)));
        if (skipInitial)
        {
            condition = SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName(indexVariableName),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                condition);
        }

        return StatementEmitter.CreateIf(
            condition,
            StatementEmitter.CreateBlock(QueryEmitter.GenerateCancellationCheck()));
    }
}
