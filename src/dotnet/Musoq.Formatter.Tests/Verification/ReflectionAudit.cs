using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Musoq.Formatter.Tests.Verification;

internal static class ReflectionAudit
{
    internal static IReadOnlyList<string> Violations(CSharpCompilation compilation)
    {
        var violations = new List<string>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var expression in tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>())
            {
                if (expression.Ancestors().Any(ancestor => ancestor is AttributeSyntax))
                    continue;
                var operation = model.GetOperation(expression);
                if (operation is IDynamicMemberReferenceOperation or IDynamicInvocationOperation or IDynamicIndexerAccessOperation)
                    violations.Add("Dynamic member access: " + expression);
                var symbol = model.GetSymbolInfo(expression).Symbol;
                if (symbol is IMethodSymbol method && expression.Parent is not MemberAccessExpressionSyntax && Forbidden(method, expression))
                    violations.Add("Runtime discovery/invocation: " + expression);
                if (symbol is IPropertySymbol property && ForbiddenProperty(property))
                    violations.Add("Runtime metadata: " + expression);
            }
        }
        return violations;
    }

    private static bool Forbidden(IMethodSymbol method, ExpressionSyntax expression)
    {
        var type = method.ContainingType.ToDisplayString();
        var space = method.ContainingNamespace.ToDisplayString();
        if (space.StartsWith("System.Reflection", StringComparison.Ordinal))
            return true;
        if (type is "System.Type" or "System.Activator" or "System.AppDomain" or "System.Runtime.Loader.AssemblyLoadContext" or
            "System.ComponentModel.TypeDescriptor" || type == "System.Delegate" && method.Name is "DynamicInvoke" or "CreateDelegate")
            return !IsTypeEquality(method);
        if (method.Name == "GetType" && type == "object")
        {
            var invocation = expression as InvocationExpressionSyntax ?? expression.Parent as InvocationExpressionSyntax;
            return invocation == null || !ExactTypeComparison(invocation);
        }
        return type == "System.Enum" && method.Name is "GetNames" or "GetValues" or "GetName" or "Parse" or "TryParse" or "ToObject";
    }

    private static bool ForbiddenProperty(IPropertySymbol property) =>
        property.ContainingNamespace.ToDisplayString().StartsWith("System.Reflection", StringComparison.Ordinal) ||
        property.ContainingType.ToDisplayString() is "System.Type" or "System.Delegate";

    private static bool IsTypeEquality(IMethodSymbol method) => method.Name is "Equals" or "GetHashCode" or "op_Equality" or "op_Inequality";

    private static bool ExactTypeComparison(ExpressionSyntax expression) => expression.Parent is BinaryExpressionSyntax binary &&
        binary.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression &&
        (binary.Left is TypeOfExpressionSyntax || binary.Right is TypeOfExpressionSyntax);
}
