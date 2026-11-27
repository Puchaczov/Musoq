using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal static ExpressionSyntax CreateWindowAggregateFrameStartExpression(
        ExecutionWindowFrameBound bound,
        string partitionIndexName,
        string partitionCountName
    )
    {
        return bound.Kind switch
        {
            ExecutionWindowFrameBoundKind.UnboundedPreceding => CreateIntLiteral(0),
            ExecutionWindowFrameBoundKind.UnboundedFollowing => SyntaxFactory.BinaryExpression(
                SyntaxKind.SubtractExpression,
                SyntaxFactory.IdentifierName(partitionCountName),
                CreateIntLiteral(1)
            ),
            ExecutionWindowFrameBoundKind.CurrentRow => SyntaxFactory.IdentifierName(partitionIndexName),
            ExecutionWindowFrameBoundKind.OffsetPreceding => CreateMathInvocation(
                nameof(Math.Max),
                CreateIntLiteral(0),
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.SubtractExpression,
                    SyntaxFactory.IdentifierName(partitionIndexName),
                    CreateIntLiteral(bound.Offset)
                )
            ),
            ExecutionWindowFrameBoundKind.OffsetFollowing => CreateFollowingFrameBound(
                partitionIndexName,
                bound.Offset,
                SyntaxFactory.IdentifierName(partitionCountName)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null),
        };
    }

    internal static ExpressionSyntax CreateWindowAggregateFrameEndExpression(
        ExecutionWindowFrameBound bound,
        string partitionIndexName,
        string partitionCountName
    )
    {
        var lastIndex = SyntaxFactory.BinaryExpression(
            SyntaxKind.SubtractExpression,
            SyntaxFactory.IdentifierName(partitionCountName),
            CreateIntLiteral(1)
        );

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
                    CreateIntLiteral(bound.Offset)
                )
            ),
            ExecutionWindowFrameBoundKind.OffsetFollowing => CreateFollowingFrameBound(
                partitionIndexName,
                bound.Offset,
                lastIndex
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null),
        };
    }

    private static ExpressionSyntax CreateFollowingFrameBound(
        string partitionIndexName,
        int offset,
        ExpressionSyntax limit
    )
    {
        var index = SyntaxFactory.IdentifierName(partitionIndexName);
        var remaining = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, limit, index);
        var distance = CreateMathInvocation(nameof(Math.Min), CreateIntLiteral(offset), remaining);
        return SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, index, distance);
    }
}
