using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

internal static class StructuralParameterBindingSyntaxFactory
{
    internal static InvocationExpressionSyntax Create(
        ScriptParameterDefinition definition,
        TypeSyntax valueType,
        Func<ExpressionSyntax> stateParametersRead)
    {
        var methodName = definition.HasDefaultValue
            ? nameof(ScriptParameterBinder.GetOptional)
            : nameof(ScriptParameterBinder.GetRequired);
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(ScriptParameterBinder)),
            SyntaxFactory.GenericName(methodName)
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        valueType))));

        var arguments = new List<ExpressionSyntax>
        {
            stateParametersRead(),
            ExecutionSyntaxFactory.CreateStringLiteral(definition.Name)
        };

        if (definition.HasDefaultValue)
        {
            arguments.Add(SyntaxFactory.DefaultExpression(valueType));
        }

        return SyntaxFactory.InvocationExpression(memberAccess)
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(arguments));
    }
}
