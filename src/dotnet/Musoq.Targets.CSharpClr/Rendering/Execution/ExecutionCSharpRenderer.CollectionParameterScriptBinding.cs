using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static InvocationExpressionSyntax CreateCollectionScriptParameterBindingExpression(
        ScriptParameterDefinition definition)
    {
        if (definition.HasDefaultValue)
            throw new InvalidOperationException("Collection script parameters do not support default values.");

        var elementType = definition.ParameterType.GetElementType() ??
                          throw new InvalidOperationException(
                              $"Collection script parameter '{definition.Name}' is missing an element type.");

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(ScriptParameterBinder)),
            SyntaxFactory.GenericName(nameof(ScriptParameterBinder.GetRequiredCollection))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        ScriptParameterSyntaxFactory.CreateTypeSyntax(elementType)))));

        return SyntaxFactory.InvocationExpression(memberAccess)
            .WithArgumentList(CreateArgumentList(
                CreateExecutionStateParametersRead(),
                CreateStringLiteral(definition.Name)));
    }
}
