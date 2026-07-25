using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

public static partial class MethodDeclarationHelper
{
    public static PropertyDeclarationSyntax CreateSourceRuntimeSettingsBySourceContextIdProperty()
    {
        return SyntaxFactory.PropertyDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            CreateSourceRuntimeSettingsBySourceContextIdType()
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(nameof(IQueryRunnable.SourceRuntimeSettingsBySourceContextId)),
            SyntaxFactory.AccessorList(
                SyntaxFactory.List([
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                ])),
            null,
            null);
    }

    public static PropertyDeclarationSyntax CreateSourceRuntimeSettingDescriptionsBySourceContextIdProperty()
    {
        return SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName("IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>"),
                nameof(IQueryRunnable.SourceRuntimeSettingDescriptionsBySourceContextId))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List([
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    ])));
    }
}
