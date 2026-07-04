using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Codegen;

internal static class CodegenReadabilitySyntaxFacts
{
    public static readonly string[] LifecycleMethodNames =
    [
        "OnDataSourceProgress",
        "OnPhaseChanged",
        "Run"
    ];

    public static bool IsLifecycleMethodName(string name)
    {
        return LifecycleMethodNames.Contains(name, StringComparer.Ordinal);
    }

    public static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
    {
        return modifiers.Any(kind);
    }

    public static bool IsPrivateStaticHelper(MethodDeclarationSyntax method)
    {
        return method.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PrivateKeyword)) &&
               method.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.StaticKeyword)) &&
               !IsLifecycleMethodName(method.Identifier.ValueText);
    }

    public static IEnumerable<string> CollectInvocationNames(MethodDeclarationSyntax method)
    {
        return method
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(TryGetInvocationName)
            .OfType<string>()
            .Where(static name => !string.IsNullOrWhiteSpace(name));
    }

    public static string? TryGetInvocationName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => null
        };
    }

    public static bool IsSafelyRemovableInitializer(EqualsValueClauseSyntax? initializer)
    {
        return initializer is null || IsSafelyRemovableExpression(initializer.Value);
    }

    public static bool IsSafelyRemovableExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax => true,
            DefaultExpressionSyntax => true,
            ParenthesizedExpressionSyntax parenthesized => IsSafelyRemovableExpression(parenthesized.Expression),
            _ => false
        };
    }
}

