using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class NestedPropertyReadRenderer
{
    internal static ExpressionSyntax Render(
        ExecutionFieldRead fieldRead,
        NestedClrPropertyAccess nestedProperty)
    {
        if (nestedProperty.PropertyPath.Contains('[', StringComparison.Ordinal))
            throw new InvalidOperationException("Indexed member reads must be bound to concrete operations before rendering.");

        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Nested CLR property field reads require a source alias.");

        var separator = nestedProperty.PropertyPath.StartsWith('[') ? string.Empty : ".";
        var path = nestedProperty.IsNullSafe
            ? nestedProperty.PropertyPath.Replace(".", "?.", StringComparison.Ordinal)
            : nestedProperty.PropertyPath;
        return NativeEnumReadNormalizer.Normalize(
            fieldRead,
            SyntaxFactory.ParseExpression(
                $"{EscapeIdentifier(fieldRead.Alias)}{separator}{path}"));
    }

}
