using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class PagingCountSyntax
{
    internal static ExpressionSyntax Read(PagingCount count) => count.Constant is { } value
        ? SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value))
        : SyntaxFactory.IdentifierName(PagingCountExecution.Variable(count).Name);
}
