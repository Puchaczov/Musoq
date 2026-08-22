using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

public static partial class MethodDeclarationHelper
{
    public static MethodDeclarationSyntax CreateContextRunMethodWithBody(BlockSyntax body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(nameof(Table)),
                SyntaxFactory.Identifier(nameof(IContextTableRunnable.Run)))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("queryContext"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(QueryRunContext)))))
                )
            .WithBody(body);
    }

    public static MethodDeclarationSyntax CreateContextProfiledRunMethodWithBody(BlockSyntax body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(nameof(Table)),
                SyntaxFactory.Identifier(nameof(IContextProfiledRunnable.RunWithProfile)))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("queryContext"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(QueryRunContext))),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("profileRecorder"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder)))
                    ])))
            .WithBody(body);
    }
}
