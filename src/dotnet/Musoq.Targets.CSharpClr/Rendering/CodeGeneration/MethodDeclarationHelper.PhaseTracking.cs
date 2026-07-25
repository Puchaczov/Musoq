using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

public static partial class MethodDeclarationHelper
{
    #region Phase Tracking Members

    /// <summary>
    ///     Creates the PhaseChanged event declaration for the query runnable interface implementation.
    /// </summary>
    /// <returns>Event field declaration for PhaseChanged</returns>
    public static EventFieldDeclarationSyntax CreatePhaseChangedEvent()
    {
        return SyntaxFactory.EventFieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(nameof(QueryPhaseEventHandler)))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("PhaseChanged")))))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
    }

    /// <summary>
    ///     Creates the OnPhaseChanged helper method that safely invokes the PhaseChanged event.
    /// </summary>
    /// <returns>Method declaration for OnPhaseChanged</returns>
    public static MethodDeclarationSyntax CreateOnPhaseChangedMethod()
    {
        var body = SyntaxFactory.Block(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName("PhaseChanged"),
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberBindingExpression(
                                SyntaxFactory.IdentifierName("Invoke")))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                [
                                    SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.ObjectCreationExpression(
                                                SyntaxFactory.IdentifierName(nameof(QueryPhaseEventArgs)))
                                            .WithArgumentList(
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SeparatedList(
                                                    [
                                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("queryId")),
                                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("phase"))
                                                    ]))))
                                ]))))));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                SyntaxFactory.Identifier("OnPhaseChanged"))
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("queryId"))
                            .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("phase"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(QueryPhase)))
                    ])))
            .WithBody(body);
    }

    #endregion
}
