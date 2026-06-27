using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

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

}
