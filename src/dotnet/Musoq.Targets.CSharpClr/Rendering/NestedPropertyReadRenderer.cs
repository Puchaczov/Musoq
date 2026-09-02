using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

internal static class NestedPropertyReadRenderer
{
    internal static ExpressionSyntax Render(
        ExecutionFieldRead fieldRead,
        NestedClrPropertyAccess nestedProperty)
    {
        if (nestedProperty.PropertyPath.Contains('[', StringComparison.Ordinal))
            return RenderSafe(fieldRead, nestedProperty);

        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Nested CLR property field reads require a source alias.");

        var separator = nestedProperty.PropertyPath.StartsWith('[') ? string.Empty : ".";
        return NativeEnumReadNormalizer.Normalize(
            fieldRead,
            SyntaxFactory.ParseExpression(
                $"{EscapeIdentifier(fieldRead.Alias)}{separator}{nestedProperty.PropertyPath}"));
    }

    private static ExpressionSyntax RenderSafe(
        ExecutionFieldRead fieldRead,
        NestedClrPropertyAccess nestedProperty)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Nested CLR property field reads require a source alias.");

        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(SafeArrayAccess)),
                    SyntaxFactory.IdentifierName(nameof(SafeArrayAccess.GetNestedValue))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(CreateIdentifierName(fieldRead.Alias)),
                SyntaxFactory.Argument(CreateStringLiteral(nestedProperty.PropertyPath)),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(CreateTypeSyntax(fieldRead.ReturnType)))
            ])));

        if (fieldRead.EnumType != null)
            return NativeEnumReadNormalizer.Normalize(fieldRead, invocation, sourceValueIsBoxed: true);

        return fieldRead.ReturnType.RequireClrType() == typeof(void)
            ? invocation
            : SyntaxFactory.CastExpression(CreateTypeSyntax(fieldRead.ReturnType), invocation);
    }
}
