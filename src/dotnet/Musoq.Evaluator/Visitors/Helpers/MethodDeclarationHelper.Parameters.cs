using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Helpers;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.Helpers;

public static partial class MethodDeclarationHelper
{
    #region Private Helper Methods

    private static ParameterSyntax CreateProviderParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            SyntaxFactory.IdentifierName(nameof(ISchemaProvider))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("provider"), null);
    }

    private static ParameterSyntax CreateSourceRuntimeSettingsBySourceContextIdParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            CreateSourceRuntimeSettingsBySourceContextIdType()
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("sourceRuntimeSettingsBySourceContextId"), null);
    }

    private static ParameterSyntax CreateSourceExecutionPlansParameter()
    {
        return SyntaxFactory.Parameter(
                SyntaxFactory.Identifier("sourceExecutionPlans"))
            .WithType(CreateSourceExecutionPlansType());
    }

    private static ParameterSyntax CreateLoggerParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            SyntaxFactory.IdentifierName(nameof(ILogger))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("logger"), null);
    }

    private static ParameterSyntax CreateTokenParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            SyntaxFactory.IdentifierName(nameof(CancellationToken))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("token"), null);
    }

    private static GenericNameSyntax CreateSourceRuntimeSettingsBySourceContextIdType()
    {
        return SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("IReadOnlyDictionary"))
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.GenericName(
                                    SyntaxFactory.Identifier("IReadOnlyDictionary"))
                                .WithTypeArgumentList(
                                    SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SeparatedList<TypeSyntax>(
                                            new SyntaxNodeOrToken[]
                                            {
                                                SyntaxFactory.PredefinedType(
                                                    SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                SyntaxFactory.PredefinedType(
                                                    SyntaxFactory.Token(SyntaxKind.StringKeyword))
                                            })))
                        })));
    }

    private static GenericNameSyntax CreateSourceExecutionPlansType()
    {
        return SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("IReadOnlyDictionary"))
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.IdentifierName("SourceExecutionPlan")
                        })));
    }

    #endregion
}
