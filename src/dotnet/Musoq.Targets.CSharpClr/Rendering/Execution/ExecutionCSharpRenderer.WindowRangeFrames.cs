using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ExpressionSyntax CreateWindowAggregateFrameStartExpressionForKernel(
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
            : CreateWindowAggregateFrameStartExpression(bound, partitionIndexName, partitionCountName);
    }

    private static ExpressionSyntax CreateWindowAggregateFrameEndExpressionForKernel(
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
            : CreateWindowAggregateFrameEndExpression(bound, partitionIndexName, partitionCountName);
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
        if (kernel.OrderKeys.Count != 1)
            throw new InvalidOperationException("Bounded RANGE frames require exactly one order key.");

        var offset = bound.Kind switch
        {
            ExecutionWindowFrameBoundKind.OffsetPreceding => -bound.Offset,
            ExecutionWindowFrameBoundKind.OffsetFollowing => bound.Offset,
            _ => 0
        };

        return CreateWindowHelperInvocation(
            helperName,
            SyntaxFactory.IdentifierName(GetWindowAggregateRangeKeysName(kernel)),
            SyntaxFactory.IdentifierName(partitionIndicesName),
            SyntaxFactory.IdentifierName(partitionStartName),
            SyntaxFactory.IdentifierName(partitionCountName),
            SyntaxFactory.IdentifierName(partitionIndexName),
            CreateIntLiteral(offset),
            CreateBooleanLiteral(kernel.OrderKeys[0].Descending));
    }

    private static string GetWindowAggregateRangeKeysName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}RangeKeys";
    }
}
