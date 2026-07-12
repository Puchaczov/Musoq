using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private MethodDeclarationSyntax CreateQueryMethod(
        string methodName,
        BlockSyntax body,
        ExecutionRenderContext context)
    {
        if (!IsInstrumentationEnabled)
        {
            return context.Session.UseQueryRunContext
                ? SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName("Table"), SyntaxFactory.Identifier(methodName))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                    .WithParameterList(MethodDeclarationHelper.CreateTypedRunContextParameterList())
                    .WithBody(body)
                : MethodDeclarationHelper.CreateStandardPrivateMethod(methodName, body);
        }

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName("Table"),
                SyntaxFactory.Identifier(methodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateStandardParameterList()
                .AddParameters(CreateProfileRecorderParameter()))
            .WithBody(body);
    }

    private static ParameterSyntax CreateProfileRecorderParameter()
    {
        return SyntaxFactory.Parameter(SyntaxFactory.Identifier(ProfileRecorderVariableName))
            .WithType(SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder)));
    }
}
