using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static SimpleNameSyntax CreateMethodNameSyntax(MethodInfo method)
    {
        if (!method.IsGenericMethod || !method.IsConstructedGenericMethod)
            return SyntaxFactory.IdentifierName(method.Name);

        return SyntaxFactory.GenericName(method.Name)
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                method.GetGenericArguments().Select(CreateTypeSyntax))));
    }
}
