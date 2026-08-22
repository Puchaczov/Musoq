using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

public static partial class MethodDeclarationHelper
{
    public static EventFieldDeclarationSyntax CreateQueryProgressEvent()
    {
        return SyntaxFactory.EventFieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(nameof(QueryProgressEventHandler)))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("QueryProgress")))))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
    }

}
