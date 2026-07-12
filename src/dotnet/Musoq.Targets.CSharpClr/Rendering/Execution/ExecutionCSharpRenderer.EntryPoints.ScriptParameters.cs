using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> CreateScriptParameterBindingStatements()
    {
        foreach (var definition in _scriptParameterDefinitions)
        {
            yield return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                GetScriptParameterLocalName(definition.Name),
                CreateScriptParameterBindingExpression(definition));
        }

        yield return CreateScriptParameterUnknownValidationStatement();
    }

    private InvocationExpressionSyntax CreateScriptParameterBindingExpression(ScriptParameterDefinition definition)
    {
        if (definition.ParameterType.IsArray)
            return CreateCollectionScriptParameterBindingExpression(definition);

        var methodName = definition.HasDefaultValue
            ? nameof(ScriptParameterBinder.GetOptional)
            : nameof(ScriptParameterBinder.GetRequired);

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(ScriptParameterBinder)),
            SyntaxFactory.GenericName(methodName)
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        ScriptParameterSyntaxFactory.CreateTypeSyntax(definition.ParameterType)))));

        var arguments = new List<ExpressionSyntax>
        {
            CreateExecutionStateParametersRead(),
            CreateStringLiteral(definition.Name)
        };

        if (definition.HasDefaultValue)
            arguments.Add(ScriptParameterSyntaxFactory.CreateDefaultArgumentExpression(
                definition.ParameterType,
                definition.DefaultValue));

        return SyntaxFactory.InvocationExpression(memberAccess)
            .WithArgumentList(CreateArgumentList(arguments));
    }

    private ExpressionStatementSyntax CreateScriptParameterUnknownValidationStatement()
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(ScriptParameterBinder)),
            SyntaxFactory.IdentifierName(nameof(ScriptParameterBinder.ValidateNoUnknownParameters)));

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(memberAccess)
                .WithArgumentList(CreateArgumentList(
                    CreateExecutionStateParametersRead(),
                    CreateScriptParameterDeclaredNamesExpression())));
    }

    private ExpressionSyntax CreateScriptParameterDeclaredNamesExpression()
    {
        if (_scriptParameterDefinitions.Count == 0)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Array)),
                    SyntaxFactory.GenericName(nameof(Array.Empty))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))))));
        }

        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(_scriptParameterDefinitions.Select<ScriptParameterDefinition, ExpressionSyntax>(static definition =>
                    CreateStringLiteral(definition.Name)))));
    }

}
