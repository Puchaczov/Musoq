using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
namespace Musoq.Targets.CSharpClr;
public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> CreateScriptParameterBindingStatements(ExecutionPlan plan)
    {
        if (_scriptParameterDefinitions.Any(static definition => definition.Contract.IsStructured))
            yield return SyntaxFactory.ParseStatement("var __musoqTypedParameterCaptureState = new StructuralParameterCaptureState(token);");
        for (var parameterIndex = 0; parameterIndex < _scriptParameterDefinitions.Count; parameterIndex++)
        {
            var definition = _scriptParameterDefinitions[parameterIndex];
            yield return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                GetScriptParameterLocalName(definition.Name),
                CreateScriptParameterBindingExpression(definition, parameterIndex));
        }
        yield return CreateScriptParameterUnknownValidationStatement();
        foreach (var name in plan.NonNullMembershipParameters)
            yield return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseExpression($"{nameof(ScriptParameterBinder)}.{nameof(ScriptParameterBinder.ValidateMembershipCollection)}"))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.IdentifierName(GetScriptParameterLocalName(name)),
                    CreateStringLiteral(name))));
    }
    private ExpressionSyntax CreateScriptParameterBindingExpression(
        ScriptParameterDefinition definition,
        int parameterIndex)
    {
        if (definition.Contract.IsStructured)
        {
            if (StructuralCarrierSyntaxFactory.TryCreateParameterStorage(
                    _scriptVariableDefinitions,
                    definition,
                    parameterIndex,
                    out var storageType))
            {
                var name = CreateStringLiteral(definition.Name).ToFullString();
                var state = ExecutionStateVariableName;
                var index = _scriptVariableDefinitions.Count + parameterIndex;
                return SyntaxFactory.ParseExpression($"__musoqBindTypedJson_{index}({state}, {name}, __musoqTypedParameterCaptureState)");
            }
            throw new InvalidOperationException(
                $"Structured parameter '{definition.Name}' does not have generated storage metadata.");
        }
        if (definition.ParameterType.IsArray)
            return CreateCollectionScriptParameterBindingExpression(definition);

        var methodName = definition.HasDefaultValue
            ? nameof(ScriptParameterBinder.GetOptional)
            : nameof(ScriptParameterBinder.GetRequired);

        var underlying = Nullable.GetUnderlyingType(definition.ParameterType);
        if (underlying is not null)
            methodName = definition.HasDefaultValue ? nameof(ScriptParameterBinder.GetOptionalNullable) : nameof(ScriptParameterBinder.GetRequiredNullable);

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(ScriptParameterBinder)),
            SyntaxFactory.GenericName(methodName)
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        ScriptParameterSyntaxFactory.CreateTypeSyntax(underlying ?? definition.ParameterType)))));

        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName(ExecutionStateVariableName),
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
                    SyntaxFactory.IdentifierName(ExecutionStateVariableName),
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
