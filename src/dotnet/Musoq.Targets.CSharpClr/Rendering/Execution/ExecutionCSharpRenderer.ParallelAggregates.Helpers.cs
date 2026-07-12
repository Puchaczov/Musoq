using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IfStatementSyntax CreatePeriodicCancellationCheck(
        string indexName,
        string cancellationTokenName)
    {
        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(
                    SyntaxKind.BitwiseAndExpression,
                    SyntaxFactory.IdentifierName(indexName),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1023)))),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
            StatementEmitter.CreateBlock(SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(cancellationTokenName),
                        SyntaxFactory.IdentifierName(nameof(CancellationToken.ThrowIfCancellationRequested)))))));
    }

    private static BinaryExpressionSyntax CreateShardBoundaryExpression(
        string rowsName,
        ExpressionSyntax shardIndex,
        string workerCountName)
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.DivideExpression,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.MultiplyExpression,
                CreateRowsCountExpression(rowsName),
                shardIndex),
            SyntaxFactory.IdentifierName(workerCountName));
    }

    private static BinaryExpressionSyntax CreateShardBoundaryExpression(
        string rowsName,
        string shardIndexName,
        string workerCountName)
    {
        return CreateShardBoundaryExpression(
            rowsName,
            SyntaxFactory.IdentifierName(shardIndexName),
            workerCountName);
    }

    private static MemberAccessExpressionSyntax CreateRowsCountExpression(string rowsName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(rowsName),
            SyntaxFactory.IdentifierName(nameof(IReadOnlyCollection<>.Count)));
    }

    private static GenericNameSyntax CreateReadOnlyListTypeSyntax(TypeSyntax itemType)
    {
        return SyntaxFactory.GenericName(nameof(IReadOnlyList<>))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(itemType)));
    }

    private static TypeSyntax CreateChunkedRowsTypeSyntax(TypeSyntax itemType)
    {
        return CreateEnumerableTypeSyntax(CreateReadOnlyListTypeSyntax(itemType));
    }

    private static bool IsChunkedParallelSingleKeyAggregate(ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        return ExecutionRowStreams.IsChunked(parallelAggregate.SourceRows);
    }

    private static ArrayTypeSyntax CreateSingleDimensionArrayTypeSyntax(TypeSyntax itemType)
    {
        return SyntaxFactory.ArrayType(itemType)
            .WithRankSpecifiers(SyntaxFactory.SingletonList(
                SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.OmittedArraySizeExpression()))));
    }

    private static AggregateGroupKeyField GetSingleAggregateGroupKey(AggregateGroupShape shape)
    {
        if (shape.Keys.Count != 1)
        {
            throw new InvalidOperationException(
                $"Parallel single-key aggregate group '{shape.TypeName}' expected exactly one key, but found {shape.Keys.Count.ToString(CultureInfo.InvariantCulture)}.");
        }

        return shape.Keys[0];
    }

    private static void ValidateParallelSingleKeyAggregateShape(ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        if (parallelAggregate.GroupShape.RequiresParentLinks)
        {
            throw new InvalidOperationException(
                $"Parallel single-key aggregate group '{parallelAggregate.GroupShape.TypeName}' cannot use parent prefix groups yet.");
        }

        _ = GetSingleAggregateGroupKey(parallelAggregate.GroupShape);
    }

    private static string CreateParallelNullGroupName()
    {
        return "nullGroup";
    }

    private string CreateParallelSingleKeyAggregateShardFunctionName(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        return CreateParallelSingleKeyAggregateRelatedName(
            parallelAggregate,
            context,
            "ParallelSingleKeyAggregateShard_",
            "Shard");
    }

    private string CreateParallelSingleKeyAggregateWorkerTypeName(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        return CreateParallelSingleKeyAggregateRelatedName(
            parallelAggregate,
            context,
            "ParallelSingleKeyAggregateWorker_",
            "Worker");
    }

    private string CreateParallelSingleKeyAggregateChunkFunctionName(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        return CreateParallelSingleKeyAggregateRelatedName(
            parallelAggregate,
            context,
            "ParallelSingleKeyAggregateChunk_",
            "Chunk");
    }

    private string CreateParallelSingleKeyAggregateChunkWorkerTypeName(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        return CreateParallelSingleKeyAggregateRelatedName(
            parallelAggregate,
            context,
            "ParallelSingleKeyAggregateChunkWorker_",
            "ChunkWorker");
    }

    private string CreateParallelSingleKeyAggregateRelatedName(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context,
        string sharedPrefix,
        string fallbackSuffix)
    {
        var functionName = CreateParallelSingleKeyAggregateFunctionName(parallelAggregate, context);
        const string prefix = "ParallelSingleKeyAggregate_";

        return functionName.StartsWith(prefix, StringComparison.Ordinal)
            ? $"{sharedPrefix}{functionName[prefix.Length..]}"
            : $"{functionName}{fallbackSuffix}";
    }
}
