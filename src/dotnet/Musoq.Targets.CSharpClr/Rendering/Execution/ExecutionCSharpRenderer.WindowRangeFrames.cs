using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Targets.CSharpClr;

internal static class WindowRangeFrameSyntax
{
    internal static ExpressionSyntax CreateAggregateFrameExpression(
        ExecutionWindowAggregateKernel kernel,
        bool isStart,
        WindowPartitionPositionSyntax partition,
        string position
    ) =>
        CreateFrameExpression(
            kernel.Frame!,
            isStart,
            kernel.OrderKeys,
            kernel.OrderKeyArray?.Variable.Name ?? $"{kernel.Results.Name}OrderKeys",
            GetAggregateRangeKeysName(kernel),
            partition,
            position
        );

    internal static ExpressionSyntax CreateFrameExpression(
        ExecutionWindowFrame frame,
        bool isStart,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        string? sortedKeys,
        string? numericKeys,
        WindowPartitionPositionSyntax partition,
        string position
    )
    {
        var bound = isStart ? frame.Start : frame.End;
        var unbounded =
            bound.Kind
            is ExecutionWindowFrameBoundKind.UnboundedPreceding
                or ExecutionWindowFrameBoundKind.UnboundedFollowing;
        if (frame.Kind != ExecutionWindowFrameKind.Range || unbounded)
        {
            var endpoint = isStart
                ? ExecutionCSharpRenderer.CreateWindowAggregateFrameStartExpression(bound, position, partition.Count)
                : ExecutionCSharpRenderer.CreateWindowAggregateFrameEndExpression(bound, position, partition.Count);
            if (
                !frame.HasNumericRangeOffset
                || Nullable.GetUnderlyingType(orderKeys[0].Expression.ReturnType.RequireClrType()) == null
            )
                return endpoint;

            // A bounded RANGE on a null key uses the null peer group at both ends.
            var row = CreateElementAccess(
                SyntaxFactory.IdentifierName(partition.Indices),
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    SyntaxFactory.IdentifierName(partition.Start),
                    SyntaxFactory.IdentifierName(position)
                )
            );
            var key = CreateElementAccess(SyntaxFactory.IdentifierName(numericKeys!), row);
            return SyntaxFactory.ConditionalExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    key,
                    SyntaxFactory.IdentifierName("HasValue")
                ),
                endpoint,
                CreatePeerBoundary(isStart, sortedKeys!, partition, position)
            );
        }

        if (sortedKeys == null)
            throw new InvalidOperationException("RANGE frames require an order key array.");
        if (bound.Kind == ExecutionWindowFrameBoundKind.CurrentRow)
            return CreatePeerBoundary(isStart, sortedKeys, partition, position);
        if (orderKeys.Count != 1 || numericKeys == null)
            throw new InvalidOperationException("Offset RANGE frames require one numeric order key.");

        var orderKey = orderKeys[0];
        var offset = bound.Kind == ExecutionWindowFrameBoundKind.OffsetPreceding ? -bound.Offset : bound.Offset;
        return CreateWindowHelperInvocation(
            isStart
                ? nameof(WindowFunctionHelpers.ResolveRangeFrameStart)
                : nameof(WindowFunctionHelpers.ResolveRangeFrameEnd),
            SyntaxFactory.IdentifierName(numericKeys),
            SyntaxFactory.IdentifierName(sortedKeys),
            SyntaxFactory.IdentifierName(partition.Indices),
            SyntaxFactory.IdentifierName(partition.Start),
            SyntaxFactory.IdentifierName(partition.Count),
            SyntaxFactory.IdentifierName(position),
            CreateIntLiteral(offset),
            CreateBooleanLiteral(orderKey.Descending),
            CreateBooleanLiteral(
                orderKey.NullOrdering == NullOrdering.First
                    || orderKey.NullOrdering == NullOrdering.Default && !orderKey.Descending
            )
        );
    }

    private static ExpressionSyntax CreatePeerBoundary(
        bool isStart,
        string keys,
        WindowPartitionPositionSyntax partition,
        string position
    ) =>
        CreateWindowHelperInvocation(
            isStart
                ? nameof(WindowFunctionHelpers.ResolveRangePeerFrameStart)
                : nameof(WindowFunctionHelpers.ResolveRangePeerFrameEnd),
            SyntaxFactory.IdentifierName(keys),
            SyntaxFactory.IdentifierName(partition.Indices),
            SyntaxFactory.IdentifierName(partition.Start),
            SyntaxFactory.IdentifierName(partition.Count),
            SyntaxFactory.IdentifierName(position)
        );

    internal static string GetAggregateRangeKeysName(ExecutionWindowAggregateKernel kernel) =>
        $"{(kernel.BoundariesOwner ?? kernel.Results).Name}RangeKeys";

    internal static StatementSyntax CreateStreamingPeerRowsLoop(
        string resultName,
        string resultExpression,
        string orderKeysName,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName,
        string currentIndexName,
        string itemDeclarations,
        string accumulate
    )
    {
        var partitionIndexName = $"{resultName}PartitionIndex";
        var peerEndName = $"{resultName}PeerEnd";
        var peerIndexName = $"{resultName}PeerIndex";
        var peerValueName = $"{resultName}PeerValue";
        var source =
            $"for (int {partitionIndexName} = 0; {partitionIndexName} < {partitionCountName};)"
            + Environment.NewLine
            + "{"
            + Environment.NewLine
            + $"    var {peerEndName} = WindowFunctionHelpers.ResolveRangePeerFrameEnd({orderKeysName}, {partitionIndicesName}, {partitionStartName}, {partitionCountName}, {partitionIndexName});"
            + Environment.NewLine
            + $"    for (int {peerIndexName} = {partitionIndexName}; {peerIndexName} <= {peerEndName}; ++{peerIndexName})"
            + Environment.NewLine
            + "    {"
            + Environment.NewLine
            + $"        var {currentIndexName} = {partitionIndicesName}[{partitionStartName} + {peerIndexName}];"
            + Environment.NewLine
            + itemDeclarations
            + Environment.NewLine
            + $"        {accumulate}"
            + Environment.NewLine
            + "    }"
            + Environment.NewLine
            + $"    var {peerValueName} = {resultExpression};"
            + Environment.NewLine
            + $"    for (int {peerIndexName} = {partitionIndexName}; {peerIndexName} <= {peerEndName}; ++{peerIndexName})"
            + Environment.NewLine
            + "    {"
            + Environment.NewLine
            + $"        var {currentIndexName} = {partitionIndicesName}[{partitionStartName} + {peerIndexName}];"
            + Environment.NewLine
            + $"        {resultName}[{currentIndexName}] = {peerValueName};"
            + Environment.NewLine
            + "    }"
            + Environment.NewLine
            + $"    {partitionIndexName} = {peerEndName} + 1;"
            + Environment.NewLine
            + "}";

        return SyntaxFactory.ParseStatement(source);
    }

    private static InvocationExpressionSyntax CreateWindowHelperInvocation(
        string helperName,
        params ExpressionSyntax[] arguments
    )
    {
        return SyntaxFactory
            .InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(WindowFunctionHelpers)),
                    SyntaxFactory.IdentifierName(helperName)
                )
            )
            .WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument)))
            );
    }

    private static LiteralExpressionSyntax CreateIntLiteral(int value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
    }

    private static LiteralExpressionSyntax CreateBooleanLiteral(bool value)
    {
        return SyntaxFactory.LiteralExpression(
            value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression
        );
    }
}
