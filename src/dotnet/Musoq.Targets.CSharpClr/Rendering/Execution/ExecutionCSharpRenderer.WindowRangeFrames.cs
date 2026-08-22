using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Targets.CSharpClr;

internal static class WindowRangeFrameSyntax
{
    internal static ExpressionSyntax CreateAggregateFrameStartExpressionForKernel(
        ExecutionWindowAggregateKernel kernel,
        ExecutionWindowFrameBound bound,
        string partitionIndicesName,
        string partitionStartName,
        string partitionIndexName,
        string partitionCountName)
    {
        return IsBoundedRangeBound(kernel, bound)
            ? CreateWindowAggregateRangeFrameExpression(
                kernel,
                nameof(WindowFunctionHelpers.ResolveRangeFrameStart),
                bound,
                partitionIndicesName,
                partitionStartName,
                partitionIndexName,
                partitionCountName)
            : ExecutionCSharpRenderer.CreateWindowAggregateFrameStartExpression(
                bound,
                partitionIndexName,
                partitionCountName);
    }

    internal static ExpressionSyntax CreateAggregateFrameEndExpressionForKernel(
        ExecutionWindowAggregateKernel kernel,
        ExecutionWindowFrameBound bound,
        string partitionIndicesName,
        string partitionStartName,
        string partitionIndexName,
        string partitionCountName)
    {
        return IsBoundedRangeBound(kernel, bound)
            ? CreateWindowAggregateRangeFrameExpression(
                kernel,
                nameof(WindowFunctionHelpers.ResolveRangeFrameEnd),
                bound,
                partitionIndicesName,
                partitionStartName,
                partitionIndexName,
                partitionCountName)
            : ExecutionCSharpRenderer.CreateWindowAggregateFrameEndExpression(
                bound,
                partitionIndexName,
                partitionCountName);
    }

    private static bool IsBoundedRangeBound(
        ExecutionWindowAggregateKernel kernel,
        ExecutionWindowFrameBound bound)
    {
        return kernel.Frame?.Kind == ExecutionWindowFrameKind.Range &&
               bound.Kind is ExecutionWindowFrameBoundKind.CurrentRow or
                   ExecutionWindowFrameBoundKind.OffsetPreceding or
                   ExecutionWindowFrameBoundKind.OffsetFollowing;
    }

    private static ExpressionSyntax CreateWindowAggregateRangeFrameExpression(
        ExecutionWindowAggregateKernel kernel,
        string helperName,
        ExecutionWindowFrameBound bound,
        string partitionIndicesName,
        string partitionStartName,
        string partitionIndexName,
        string partitionCountName)
    {
        var orderKeysName = kernel.OrderKeyArray?.Variable.Name ?? $"{kernel.Results.Name}OrderKeys";

        if (bound.Kind == ExecutionWindowFrameBoundKind.CurrentRow)
        {
            var peerHelperName = helperName == nameof(WindowFunctionHelpers.ResolveRangeFrameStart)
                ? nameof(WindowFunctionHelpers.ResolveRangePeerFrameStart)
                : nameof(WindowFunctionHelpers.ResolveRangePeerFrameEnd);

            return CreateWindowHelperInvocation(
                peerHelperName,
                SyntaxFactory.IdentifierName(orderKeysName),
                SyntaxFactory.IdentifierName(partitionIndicesName),
                SyntaxFactory.IdentifierName(partitionStartName),
                SyntaxFactory.IdentifierName(partitionCountName),
                SyntaxFactory.IdentifierName(partitionIndexName));
        }

        if (kernel.OrderKeys.Count != 1)
            throw new InvalidOperationException("Offset RANGE frames require exactly one order key.");

        var offset = bound.Kind switch
        {
            ExecutionWindowFrameBoundKind.OffsetPreceding => -bound.Offset,
            ExecutionWindowFrameBoundKind.OffsetFollowing => bound.Offset,
            _ => 0
        };

        return CreateWindowHelperInvocation(
            helperName,
            SyntaxFactory.IdentifierName(GetAggregateRangeKeysName(kernel)),
            SyntaxFactory.IdentifierName(orderKeysName),
            SyntaxFactory.IdentifierName(partitionIndicesName),
            SyntaxFactory.IdentifierName(partitionStartName),
            SyntaxFactory.IdentifierName(partitionCountName),
            SyntaxFactory.IdentifierName(partitionIndexName),
            CreateIntLiteral(offset),
            CreateBooleanLiteral(kernel.OrderKeys[0].Descending),
            CreateBooleanLiteral(IsNullsFirst(kernel.OrderKeys[0])));
    }

    private static bool IsNullsFirst(ExecutionWindowOrderKey orderKey)
    {
        return orderKey.NullOrdering == NullOrdering.First ||
               orderKey.NullOrdering == NullOrdering.Default && !orderKey.Descending;
    }

    internal static bool HasRangeOffsetBound(ExecutionWindowFrame frame)
    {
        return frame.Start.Kind is ExecutionWindowFrameBoundKind.OffsetPreceding or
                   ExecutionWindowFrameBoundKind.OffsetFollowing ||
               frame.End.Kind is ExecutionWindowFrameBoundKind.OffsetPreceding or
                   ExecutionWindowFrameBoundKind.OffsetFollowing;
    }

    internal static string GetAggregateRangeKeysName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}RangeKeys";
    }

    internal static ExpressionSyntax CreatePluginWindowFrameExpression(
        ExecutionComputePluginWindow plugin,
        ExecutionWindowFrameBound bound,
        bool isStart,
        ExecutionWindowKeyArray? orderKeys,
        ExecutionVariable? rangeKeys,
        string partitionIndicesName,
        string partitionStartName,
        string partitionIndexName,
        string partitionCountName)
    {
        if (plugin.Frame?.Kind != ExecutionWindowFrameKind.Range ||
            bound.Kind is not (ExecutionWindowFrameBoundKind.CurrentRow or
                ExecutionWindowFrameBoundKind.OffsetPreceding or
                ExecutionWindowFrameBoundKind.OffsetFollowing))
        {
            return isStart
                ? ExecutionCSharpRenderer.CreateWindowAggregateFrameStartExpression(
                    bound,
                    partitionIndexName,
                    partitionCountName)
                : ExecutionCSharpRenderer.CreateWindowAggregateFrameEndExpression(
                    bound,
                    partitionIndexName,
                    partitionCountName);
        }

        if (orderKeys == null)
            throw new InvalidOperationException("RANGE value windows require an order key array.");

        if (bound.Kind == ExecutionWindowFrameBoundKind.CurrentRow)
        {
            return CreateWindowHelperInvocation(
                isStart
                    ? nameof(WindowFunctionHelpers.ResolveRangePeerFrameStart)
                    : nameof(WindowFunctionHelpers.ResolveRangePeerFrameEnd),
                SyntaxFactory.IdentifierName(orderKeys.Variable.Name),
                SyntaxFactory.IdentifierName(partitionIndicesName),
                SyntaxFactory.IdentifierName(partitionStartName),
                SyntaxFactory.IdentifierName(partitionCountName),
                SyntaxFactory.IdentifierName(partitionIndexName));
        }

        if (plugin.OrderKeys.Count != 1 || rangeKeys == null)
            throw new InvalidOperationException("Offset RANGE value windows require one numeric order key.");

        var offset = bound.Kind == ExecutionWindowFrameBoundKind.OffsetPreceding
            ? -bound.Offset
            : bound.Offset;
        var orderKey = plugin.OrderKeys[0];
        return CreateWindowHelperInvocation(
            isStart
                ? nameof(WindowFunctionHelpers.ResolveRangeFrameStart)
                : nameof(WindowFunctionHelpers.ResolveRangeFrameEnd),
            SyntaxFactory.IdentifierName(rangeKeys.Name),
            SyntaxFactory.IdentifierName(orderKeys.Variable.Name),
            SyntaxFactory.IdentifierName(partitionIndicesName),
            SyntaxFactory.IdentifierName(partitionStartName),
            SyntaxFactory.IdentifierName(partitionCountName),
            SyntaxFactory.IdentifierName(partitionIndexName),
            CreateIntLiteral(offset),
            CreateBooleanLiteral(orderKey.Descending),
            CreateBooleanLiteral(IsNullsFirst(orderKey)));
    }

    internal static StatementSyntax CreateStreamingPluginPeerRowsLoop(
        string resultName,
        string functionName,
        string orderKeysName,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName,
        string currentIndexName,
        string itemDeclarations,
        string accumulate)
    {
        var partitionIndexName = $"{resultName}PartitionIndex";
        var peerEndName = $"{resultName}PeerEnd";
        var peerIndexName = $"{resultName}PeerIndex";
        var peerValueName = $"{resultName}PeerValue";
        var source =
            $"for (int {partitionIndexName} = 0; {partitionIndexName} < {partitionCountName};)" + Environment.NewLine +
            "{" + Environment.NewLine +
            $"    var {peerEndName} = WindowFunctionHelpers.ResolveRangePeerFrameEnd({orderKeysName}, {partitionIndicesName}, {partitionStartName}, {partitionCountName}, {partitionIndexName});" + Environment.NewLine +
            $"    for (int {peerIndexName} = {partitionIndexName}; {peerIndexName} <= {peerEndName}; ++{peerIndexName})" + Environment.NewLine +
            "    {" + Environment.NewLine +
            $"        var {currentIndexName} = {partitionIndicesName}[{partitionStartName} + {peerIndexName}];" + Environment.NewLine +
            itemDeclarations + Environment.NewLine +
            $"        {accumulate}" + Environment.NewLine +
            "    }" + Environment.NewLine +
            $"    var {peerValueName} = {functionName}.GetValue();" + Environment.NewLine +
            $"    for (int {peerIndexName} = {partitionIndexName}; {peerIndexName} <= {peerEndName}; ++{peerIndexName})" + Environment.NewLine +
            "    {" + Environment.NewLine +
            $"        var {currentIndexName} = {partitionIndicesName}[{partitionStartName} + {peerIndexName}];" + Environment.NewLine +
            $"        {resultName}[{currentIndexName}] = {peerValueName};" + Environment.NewLine +
            "    }" + Environment.NewLine +
            $"    {partitionIndexName} = {peerEndName} + 1;" + Environment.NewLine +
            "}";

        return SyntaxFactory.ParseStatement(source);
    }

    private static InvocationExpressionSyntax CreateWindowHelperInvocation(
        string helperName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(WindowFunctionHelpers)),
                    SyntaxFactory.IdentifierName(helperName)))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument))));
    }

    private static LiteralExpressionSyntax CreateIntLiteral(int value)
    {
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(value));
    }

    private static LiteralExpressionSyntax CreateBooleanLiteral(bool value)
    {
        return SyntaxFactory.LiteralExpression(
            value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
    }
}
