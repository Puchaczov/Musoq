using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.Visitors.Helpers;

public static partial class MethodDeclarationHelper
{
    internal static ParameterListSyntax CreateTypedRunContextParameterList()
    {
        return SyntaxFactory.ParameterList(
            SyntaxFactory.SeparatedList(
            [
                CreateProviderParameter(),
                CreateSourceRuntimeSettingsBySourceContextIdParameter(),
                CreateSourceExecutionPlansParameter(),
                CreateLoggerParameter(),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("queryContext"))
                    .WithType(SyntaxFactory.IdentifierName(nameof(QueryRunContext)))
            ]));
    }

    internal static MethodDeclarationSyntax CreateTypedRunOptionsMethodWithBody(Type outputType, BlockSyntax body)
    {
        ArgumentNullException.ThrowIfNull(outputType);
        ArgumentNullException.ThrowIfNull(body);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName(nameof(IEnumerable<object>))
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            LegacyCodeGenerationSyntaxFactory.CreateTypeSyntax(outputType)))),
                SyntaxFactory.Identifier(nameof(ITypedRunnable<object>.Run)))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("options"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(TypedQueryRunOptions))))))
            .WithBody(body);
    }

    internal static MethodDeclarationSyntax CreateTypedRunTokenShim(Type outputType)
    {
        ArgumentNullException.ThrowIfNull(outputType);

        var body = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(nameof(ITypedRunnable<object>.Run)))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName(nameof(TypedQueryRunOptions)))
                        .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(nameof(IParameterizedRunnable.Parameters))),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(nameof(IQueryRunnable.PhaseChanged))),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(nameof(IQueryRunnable.DataSourceProgress)))
                        ])))))))));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName(nameof(IEnumerable<object>))
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            LegacyCodeGenerationSyntaxFactory.CreateTypeSyntax(outputType)))),
                SyntaxFactory.Identifier(nameof(ITypedRunnable<object>.Run)))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("token"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(CancellationToken))))))
            .WithBody(body);
    }
}
