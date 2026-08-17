using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class NestedPositionalFieldReadRenderer
{
    internal static ExpressionSyntax Render(
        ExecutionFieldRead fieldRead,
        NestedPositionalAccess nestedPositional)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Nested positional field reads require a source alias.");

        if (string.IsNullOrWhiteSpace(fieldRead.GeneratedTypeName))
            throw new InvalidOperationException("Nested positional field reads require the indexed cell type.");

        var indexedCell = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(
            SyntaxFactory.ParseTypeName(fieldRead.GeneratedTypeName),
            ExecutionSyntaxFactory.CreateElementAccess(
                ExecutionSyntaxFactory.CreateIdentifierName(fieldRead.Alias),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(nestedPositional.Index)))));

        var separator = nestedPositional.PropertyPath.StartsWith("[", StringComparison.Ordinal) ||
                         nestedPositional.PropertyPath.StartsWith(".", StringComparison.Ordinal)
            ? string.Empty
            : ".";
        var value = SyntaxFactory.ParseExpression(
            $"{indexedCell}{separator}{nestedPositional.PropertyPath}");

        return fieldRead.ReturnType.RequireClrType() == typeof(object)
            ? value
            : SyntaxFactory.CastExpression(
                ExecutionSyntaxFactory.CreateTypeSyntax(fieldRead.ReturnType),
                value);
    }
}
