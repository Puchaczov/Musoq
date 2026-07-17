using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool IsTrueLiteral(ExpressionSyntax expression)
    {
        return expression.Kind() == SyntaxKind.TrueLiteralExpression;
    }

    private ForStatementSyntax CreateWindowAggregateBoundedAssignmentLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var frame = kernel.Frame ??
                    throw new InvalidOperationException("Bounded ROWS window aggregate kernels require a frame.");
        var partitionIndexName = $"{kernel.Results.Name}PartitionIndex";
        var currentIndexName = $"{kernel.Results.Name}CurrentIndex";
        var frameStartName = $"{kernel.Results.Name}FrameStart";
        var frameEndName = $"{kernel.Results.Name}FrameEnd";
        var framePrefixStartName = $"{kernel.Results.Name}FramePrefixStart";
        var framePrefixEndName = $"{kernel.Results.Name}FramePrefixEnd";
        var body = new List<StatementSyntax>
        {
            CreateStreamingCurrentIndexDeclaration(
                partitionIndicesName,
                partitionStartName,
                partitionIndexName,
                currentIndexName),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                frameStartName,
                CreateWindowAggregateFrameStartExpressionForKernel(
                    kernel,
                    frame.Start,
                    partitionIndicesName,
                    partitionStartName,
                    partitionIndexName,
                    partitionCountName)),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                frameEndName,
                CreateWindowAggregateFrameEndExpressionForKernel(
                    kernel,
                    frame.End,
                    partitionIndicesName,
                    partitionStartName,
                    partitionIndexName,
                    partitionCountName)),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                framePrefixStartName,
                CreateMathInvocation(
                    nameof(Math.Max),
                    CreateIntLiteral(0),
                    SyntaxFactory.IdentifierName(frameStartName))),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                framePrefixEndName,
                CreateMathInvocation(
                    nameof(Math.Max),
                    CreateIntLiteral(0),
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        SyntaxFactory.IdentifierName(frameEndName),
                        CreateIntLiteral(1))))
        };

        body.AddRange(CreateWindowAggregateBoundedResultStatements(
            kernel,
            currentIndexName,
            framePrefixStartName,
            framePrefixEndName));

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(body));
    }

    private static IEnumerable<StatementSyntax> CreateWindowAggregateBoundedResultStatements(
        ExecutionWindowAggregateKernel kernel,
        string currentIndexName,
        string framePrefixStartName,
        string framePrefixEndName)
    {
        switch (kernel.Descriptor.Function)
        {
            case ExecutionWindowAggregateFunction.Sum:
                yield return CreateWindowResultAssignment(
                    kernel.Results.Name,
                    SyntaxFactory.IdentifierName(currentIndexName),
                    CreateWindowAggregatePrefixDelta(
                        CreateWindowAggregatePrefixSumName(kernel),
                        framePrefixStartName,
                        framePrefixEndName));
                break;
            case ExecutionWindowAggregateFunction.Count:
                yield return CreateWindowResultAssignment(
                    kernel.Results.Name,
                    SyntaxFactory.IdentifierName(currentIndexName),
                    CreateWindowAggregatePrefixDelta(
                        CreateWindowAggregatePrefixCountName(kernel),
                        framePrefixStartName,
                        framePrefixEndName));
                break;
            case ExecutionWindowAggregateFunction.Avg:
                var frameSumName = $"{kernel.Results.Name}FrameSum";
                var frameCountName = $"{kernel.Results.Name}FrameCount";
                yield return CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    frameSumName,
                    CreateWindowAggregatePrefixDelta(
                        CreateWindowAggregatePrefixSumName(kernel),
                        framePrefixStartName,
                        framePrefixEndName));
                yield return CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    frameCountName,
                    CreateWindowAggregatePrefixDelta(
                        CreateWindowAggregatePrefixCountName(kernel),
                        framePrefixStartName,
                        framePrefixEndName));
                yield return CreateWindowResultAssignment(
                    kernel.Results.Name,
                    SyntaxFactory.IdentifierName(currentIndexName),
                    SyntaxFactory.ConditionalExpression(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.GreaterThanExpression,
                            SyntaxFactory.IdentifierName(frameCountName),
                            CreateIntLiteral(0)),
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.DivideExpression,
                            SyntaxFactory.IdentifierName(frameSumName),
                            SyntaxFactory.IdentifierName(frameCountName)),
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0m))));
                break;
            default:
                throw UnsupportedShape.Of(
                    $"Window aggregate kernel {kernel.Descriptor.Function}");
        }
    }

    private static ExpressionSyntax CreateWindowAggregateFrameStartExpression(
        ExecutionWindowFrameBound bound,
        string partitionIndexName,
        string partitionCountName)
    {
        return bound.Kind switch
        {
            ExecutionWindowFrameBoundKind.UnboundedPreceding => CreateIntLiteral(0),
            ExecutionWindowFrameBoundKind.UnboundedFollowing => SyntaxFactory.BinaryExpression(
                SyntaxKind.SubtractExpression,
                SyntaxFactory.IdentifierName(partitionCountName),
                CreateIntLiteral(1)),
            ExecutionWindowFrameBoundKind.CurrentRow => SyntaxFactory.IdentifierName(partitionIndexName),
            ExecutionWindowFrameBoundKind.OffsetPreceding => CreateMathInvocation(
                nameof(Math.Max),
                CreateIntLiteral(0),
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.SubtractExpression,
                    SyntaxFactory.IdentifierName(partitionIndexName),
                    CreateIntLiteral(bound.Offset))),
            ExecutionWindowFrameBoundKind.OffsetFollowing => CreateMathInvocation(
                nameof(Math.Min),
                SyntaxFactory.IdentifierName(partitionCountName),
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    SyntaxFactory.IdentifierName(partitionIndexName),
                    CreateIntLiteral(bound.Offset))),
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null)
        };
    }

    private static ExpressionSyntax CreateWindowAggregateFrameEndExpression(
        ExecutionWindowFrameBound bound,
        string partitionIndexName,
        string partitionCountName)
    {
        var lastIndex = SyntaxFactory.BinaryExpression(
            SyntaxKind.SubtractExpression,
            SyntaxFactory.IdentifierName(partitionCountName),
            CreateIntLiteral(1));

        return bound.Kind switch
        {
            ExecutionWindowFrameBoundKind.UnboundedPreceding => CreateIntLiteral(0),
            ExecutionWindowFrameBoundKind.UnboundedFollowing => lastIndex,
            ExecutionWindowFrameBoundKind.CurrentRow => SyntaxFactory.IdentifierName(partitionIndexName),
            ExecutionWindowFrameBoundKind.OffsetPreceding => CreateMathInvocation(
                nameof(Math.Min),
                lastIndex,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.SubtractExpression,
                    SyntaxFactory.IdentifierName(partitionIndexName),
                    CreateIntLiteral(bound.Offset))),
            ExecutionWindowFrameBoundKind.OffsetFollowing => CreateMathInvocation(
                nameof(Math.Min),
                lastIndex,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    SyntaxFactory.IdentifierName(partitionIndexName),
                    CreateIntLiteral(bound.Offset))),
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null)
        };
    }

    private static ExpressionStatementSyntax CreateWindowAggregatePrefixAssignment(
        string prefixName,
        string partitionIndexName,
        ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(prefixName),
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        SyntaxFactory.IdentifierName(partitionIndexName),
                        CreateIntLiteral(1))),
                value));
    }

    private static ElementAccessExpressionSyntax CreateWindowAggregatePrefixCurrentAccess(
        string prefixName,
        string partitionIndexName)
    {
        return CreateElementAccess(
            SyntaxFactory.IdentifierName(prefixName),
            SyntaxFactory.IdentifierName(partitionIndexName));
    }

    private static BinaryExpressionSyntax CreateWindowAggregatePrefixDelta(
        string prefixName,
        string framePrefixStartName,
        string framePrefixEndName)
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.SubtractExpression,
            CreateElementAccess(
                SyntaxFactory.IdentifierName(prefixName),
                SyntaxFactory.IdentifierName(framePrefixEndName)),
            CreateElementAccess(
                SyntaxFactory.IdentifierName(prefixName),
                SyntaxFactory.IdentifierName(framePrefixStartName)));
    }

    private static bool RequiresWindowAggregateSumPrefix(ExecutionWindowAggregateKernel kernel)
    {
        return kernel.Descriptor.Function is ExecutionWindowAggregateFunction.Sum
            or ExecutionWindowAggregateFunction.Avg;
    }

    private static bool RequiresWindowAggregateCountPrefix(ExecutionWindowAggregateKernel kernel)
    {
        return kernel.Descriptor.Function is ExecutionWindowAggregateFunction.Count
            or ExecutionWindowAggregateFunction.Avg;
    }
}
