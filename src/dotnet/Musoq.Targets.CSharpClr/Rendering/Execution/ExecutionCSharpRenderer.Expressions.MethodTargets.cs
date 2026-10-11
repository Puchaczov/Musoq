using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static SimpleNameSyntax CreateMethodNameSyntax(ExecutionMethodCall call)
    {
        var method = call.Method.RequireClrMethod();
        if (call.GeneratedGenericArguments.Count == 0)
            return CreateMethodNameSyntax(method);
        return SyntaxFactory.GenericName(method.Name).WithTypeArgumentList(
            SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(method.GetGenericArguments()
                .Select((type, index) => call.GeneratedGenericArguments.TryGetValue(index, out var name)
                    ? SyntaxFactory.ParseTypeName(name) : CreateTypeSyntax(type)))));
    }

    private static SimpleNameSyntax CreateMethodNameSyntax(MethodInfo method)
    {
        if (!method.IsGenericMethod || !method.IsConstructedGenericMethod)
            return SyntaxFactory.IdentifierName(method.Name);

        return SyntaxFactory.GenericName(method.Name)
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                method.GetGenericArguments().Select(CreateTypeSyntax))));
    }
}
