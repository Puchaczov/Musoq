using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderForEachStream(ExecutionForEach forEach)
    {
        if (ExecutionRowStreams.IsScalar(forEach.Source))
            return RenderScalarForEach(forEach);

        return ExecutionRowStreams.IsChunked(forEach.Source)
            ? RenderChunkedForEach(forEach)
            : RenderForEach(forEach);
    }

    private IEnumerable<StatementSyntax> RenderForEachWithOrdinalityStream(ExecutionForEachWithOrdinality forEach)
    {
        if (ExecutionRowStreams.IsScalar(forEach.Source))
            return [RenderScalarForEachWithOrdinality(forEach)];

        return ExecutionRowStreams.IsChunked(forEach.Source)
            ? RenderChunkedForEachWithOrdinality(forEach)
            : RenderForEachWithOrdinality(forEach);
    }

    private StatementSyntax RenderMaterializeListStream(ExecutionMaterializeList materialize)
    {
        return ExecutionRowStreams.IsChunked(materialize.Source)
            ? RenderMaterializeChunkedList(materialize)
            : RenderMaterializeList(materialize);
    }

    private IEnumerable<StatementSyntax> RenderMaterializeFilteredListStream(ExecutionMaterializeFilteredList materialize)
    {
        return ExecutionRowStreams.IsChunked(materialize.Source)
            ? RenderMaterializeFilteredChunkedList(materialize)
            : RenderMaterializeFilteredList(materialize);
    }

    private IEnumerable<StatementSyntax> RenderMaterializeExpandoListStream(ExecutionMaterializeExpandoList materialize)
    {
        return ExecutionRowStreams.IsChunked(materialize.Source)
            ? RenderMaterializeChunkedExpandoList(materialize)
            : RenderMaterializeExpandoList(materialize);
    }

    private StatementSyntax[] RenderSourceLoopStream(ExecutionSourceLoop sourceLoop)
    {
        return sourceLoop switch
        {
            ExecutionForEach forEach => RenderForEachStream(forEach).ToArray(),
            _ => throw new InvalidOperationException(
                $"Source loop renderer cannot render loop '{sourceLoop.GetType().Name}'.")
        };
    }

    private IEnumerable<StatementSyntax> RenderChunkedForEach(ExecutionForEach forEach)
    {
        yield return RenderChunkedForEachCore(forEach.Item, forEach.Source, forEach.Body, forEach);
    }

    private IEnumerable<StatementSyntax> RenderScalarForEach(ExecutionForEach forEach)
    {
        yield return RenderScalarForEachCore(forEach.Item, forEach.Source, forEach.Body, forEach);
    }

    private StatementSyntax RenderScalarForEachWithOrdinality(ExecutionForEachWithOrdinality forEach)
    {
        return StatementEmitter.CreateBlock(
            CreateLocalDeclaration(
                CreateTypeSyntax(typeof(int)),
                forEach.Ordinal.Name,
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0))),
            RenderScalarForEachCore(forEach.Item, forEach.Source, forEach.Body, forEach));
    }

    private StatementSyntax RenderScalarForEachCore(
        ExecutionVariable item,
        ExecutionExpression source,
        ExecutionBlock body,
        ExecutionNode operatorNode)
    {
        var sourceExpression = RenderExpression(source);
        var bodyStatements = new List<StatementSyntax>();

        if (_emitChunkLoopCancellationChecks)
            bodyStatements.Add(QueryEmitter.GenerateCancellationCheck());

        bodyStatements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            item.Name,
            sourceExpression));
        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabled, _operatorCatalog, operatorNode));
        bodyStatements.AddRange(RenderBlock(body).Statements);

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

    private IEnumerable<StatementSyntax> RenderChunkedForEachWithOrdinality(ExecutionForEachWithOrdinality forEach)
    {
        yield return RenderChunkedForEachWithOrdinalityCore(forEach.Item, forEach.Source, forEach.Ordinal, forEach.Body, forEach);
    }

    private StatementSyntax RenderChunkedForEachCore(
        ExecutionVariable item,
        ExecutionExpression source,
        ExecutionBlock body,
        ExecutionNode operatorNode)
    {
        return CreateChunkedLoop(
            item,
            source,
            (itemAccessExpression, indexVariableName) =>
            {
                var bodyStatements = CreateChunkedLoopBodyPrefix(item, itemAccessExpression, indexVariableName);
                bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabled, _operatorCatalog, operatorNode));
                bodyStatements.AddRange(RenderBlock(body).Statements);
                return bodyStatements;
            });
    }

    private StatementSyntax RenderChunkedForEachWithOrdinalityCore(
        ExecutionVariable item,
        ExecutionExpression source,
        ExecutionVariable ordinal,
        ExecutionBlock body,
        ExecutionNode operatorNode)
    {
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
                (itemAccessExpression, indexVariableName) =>
                {
                    var bodyStatements = CreateChunkedLoopBodyPrefix(item, itemAccessExpression, indexVariableName);
                    bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabled, _operatorCatalog, operatorNode));
                    bodyStatements.AddRange(RenderBlock(body).Statements);
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
        Func<ExpressionSyntax, string, List<StatementSyntax>> createBodyStatements)
    {
        return ChunkedLoopSyntaxFactory.Create(
            item,
            RenderExpression(source),
            createBodyStatements);
    }

    private List<StatementSyntax> CreateChunkedLoopBodyPrefix(
        ExecutionVariable item,
        ExpressionSyntax itemAccessExpression,
        string indexVariableName)
    {
        var bodyStatements = new List<StatementSyntax>();

        if (_emitChunkLoopCancellationChecks)
            bodyStatements.Add(CreatePeriodicCancellationCheck(indexVariableName));

        bodyStatements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            item.Name,
            itemAccessExpression));

        return bodyStatements;
    }

    private static IfStatementSyntax CreatePeriodicCancellationCheck(string indexVariableName)
    {
        return StatementEmitter.CreateIf(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.BitwiseAndExpression,
                        SyntaxFactory.IdentifierName(indexVariableName),
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(1023)))),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0))),
            StatementEmitter.CreateBlock(QueryEmitter.GenerateCancellationCheck()));
    }
}
