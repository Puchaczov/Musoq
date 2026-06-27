using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.Helpers;

public static partial class MethodDeclarationHelper
{
    #region DataSource Progress Members

    /// <summary>
    ///     Creates the DataSourceProgress event declaration for the query runnable interface implementation.
    /// </summary>
    /// <returns>Event field declaration for DataSourceProgress</returns>
    public static EventFieldDeclarationSyntax CreateDataSourceProgressEvent()
    {
        return SyntaxFactory.EventFieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(nameof(DataSourceEventHandler)))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("DataSourceProgress")))))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
    }

    /// <summary>
    ///     Creates the OnDataSourceProgress helper method that safely invokes the DataSourceProgress event.
    ///     This method is passed to RuntimeContext as a callback.
    /// </summary>
    /// <returns>Method declaration for OnDataSourceProgress</returns>
    public static MethodDeclarationSyntax CreateOnDataSourceProgressMethod()
    {
        var body = SyntaxFactory.Block(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName("DataSourceProgress"),
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberBindingExpression(
                                SyntaxFactory.IdentifierName("Invoke")))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                [
                                    SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("e"))
                                ]))))));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                SyntaxFactory.Identifier("OnDataSourceProgress"))
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("sender"))
                            .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("e"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(DataSourceEventArgs)))
                    ])))
            .WithBody(body);
    }

    private static AttributeListSyntax CreateAggressiveInliningAttribute()
    {
        return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
            SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Runtime.CompilerServices.MethodImpl"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParseName("System.Runtime.CompilerServices.MethodImplOptions"),
                        SyntaxFactory.IdentifierName("AggressiveInlining"))))))));
    }

    #endregion
}
