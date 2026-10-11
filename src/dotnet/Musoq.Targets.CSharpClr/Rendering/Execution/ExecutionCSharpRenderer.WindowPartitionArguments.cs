using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static CastExpressionSyntax CreateIntCastExpression(ExpressionSyntax expression) =>
        SyntaxFactory.CastExpression(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
            SyntaxFactory.ParenthesizedExpression(expression));

    private static ExpressionSyntax CreatePartitionKeysArgument(ExecutionVariable? partitionKeys)
    {
        return partitionKeys == null
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            : SyntaxFactory.IdentifierName(partitionKeys.Name);
    }
}
