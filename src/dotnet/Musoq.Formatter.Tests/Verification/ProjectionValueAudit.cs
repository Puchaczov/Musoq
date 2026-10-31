using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Formatter.Tests.Verification;

internal static class ProjectionValueAudit
{
    internal static void Verify(SemanticModel model, InvocationExpressionSyntax call, ITypeSymbol type, string expression)
    {
        if (expression == "value.InnerQueryText" && type.SpecialType == SpecialType.System_String)
        {
            VerifyEmbeddedQuery(model, call, expression);
            return;
        }
        var operation = Operation(type);
        ProjectionAudit.RequireMethod(model, call, operation);
        if (call.ArgumentList.Arguments[0].Expression.ToString() != expression)
            throw new InvalidOperationException("Projection must consume the labeled member: " + expression);
        if (operation is "Write" or "Unbound")
        {
            VerifyLeaf(model, call, type, operation);
            return;
        }
        VerifyChildren(model, call, type, operation);
    }

    private static void VerifyLeaf(SemanticModel model, InvocationExpressionSyntax call, ITypeSymbol type, string operation)
    {
        var method = (IMethodSymbol)model.GetSymbolInfo(call).Symbol!;
        var parameter = method.Parameters.Single().Type;
        if (operation == "Unbound" || SymbolEqualityComparer.Default.Equals(parameter, type))
            return;
        for (var current = type.BaseType; current != null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(parameter, current))
                return;
        throw new InvalidOperationException("Unsupported scalar/child shape cannot use an object fallback: " + type);
    }

    private static void VerifyChildren(SemanticModel model, InvocationExpressionSyntax call, ITypeSymbol type, string operation)
    {
        var children = ChildTypes(type, operation);
        if (call.ArgumentList.Arguments.Count != children.Length + 1)
            throw new InvalidOperationException("Missing typed collection traversal.");
        for (var index = 0; index < children.Length; index++)
        {
            var lambda = call.ArgumentList.Arguments[index + 1].Expression as ParenthesizedLambdaExpressionSyntax ??
                throw new InvalidOperationException("Child traversal must be an explicit typed lambda.");
            var parameter = lambda.ParameterList.Parameters.Last().Identifier.ValueText;
            if (operation == "Pair")
            {
                VerifyTuple(model, lambda, (INamedTypeSymbol)type, parameter);
                continue;
            }
            var nested = lambda.Body as InvocationExpressionSyntax ?? throw new InvalidOperationException("Missing child projection.");
            Verify(model, nested, children[index], parameter);
        }
    }

    private static void VerifyTuple(SemanticModel model, ParenthesizedLambdaExpressionSyntax lambda, INamedTypeSymbol type, string parameter)
    {
        var statements = (lambda.Body as BlockSyntax)?.Statements ?? throw new InvalidOperationException("Tuple members must be explicit.");
        if (statements.Count != type.TupleElements.Length * 2)
            throw new InvalidOperationException("Missing tuple member traversal.");
        for (var index = 0; index < type.TupleElements.Length; index++)
        {
            var field = type.TupleElements[index];
            var label = ProjectionAudit.Invocation(statements[index * 2]);
            ProjectionAudit.RequireMethod(model, label, "Member");
            if (!Equals(model.GetConstantValue(label.ArgumentList.Arguments.Single().Expression).Value, field.Name))
                throw new InvalidOperationException("Incorrect tuple member label.");
            Verify(model, ProjectionAudit.Invocation(statements[index * 2 + 1]), field.Type, parameter + "." + field.Name);
        }
    }

    private static string Operation(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
            return "Sequence";
        if (type is INamedTypeSymbol { IsTupleType: true })
            return "Pair";
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return "Optional";
        if (Dictionary(type) != null)
            return "Dictionary";
        if (type.SpecialType != SpecialType.System_String && Sequence(type) != null)
            return "Sequence";
        if (type.ContainingNamespace.ToDisplayString() == "System.Reflection")
            return "Unbound";
        return "Write";
    }

    private static ITypeSymbol[] ChildTypes(ITypeSymbol type, string operation) => operation switch
    {
        "Sequence" => new[] { type is IArrayTypeSymbol array ? array.ElementType : Sequence(type)!.TypeArguments.Single() },
        "Dictionary" => Dictionary(type)!.TypeArguments.ToArray(),
        "Optional" => ((INamedTypeSymbol)type).TypeArguments.ToArray(),
        "Pair" => new[] { type },
        _ => throw new InvalidOperationException("Unknown child traversal operation.")
    };

    private static INamedTypeSymbol? Dictionary(ITypeSymbol type) => Interfaces(type).FirstOrDefault(candidate =>
        candidate.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>");
    private static INamedTypeSymbol? Sequence(ITypeSymbol type) => Interfaces(type).FirstOrDefault(candidate =>
        candidate.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
    private static INamedTypeSymbol[] Interfaces(ITypeSymbol type) => type.AllInterfaces.Prepend(type).OfType<INamedTypeSymbol>().ToArray();

    private static void VerifyEmbeddedQuery(SemanticModel model, InvocationExpressionSyntax call, string expression)
    {
        ProjectionAudit.RequireMethod(model, call, "Write");
        if (call.ArgumentList.Arguments.Single().Expression is not InvocationExpressionSyntax parse ||
            model.GetSymbolInfo(parse).Symbol is not IMethodSymbol { Name: "Parse", ContainingType.Name: "AstParsing" } ||
            parse.ArgumentList.Arguments.Single().Expression.ToString() != expression)
            throw new InvalidOperationException("Diagnostic commands must recursively project their parsed query.");
    }
}
