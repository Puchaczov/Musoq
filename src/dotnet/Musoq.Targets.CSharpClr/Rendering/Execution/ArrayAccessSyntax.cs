using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

internal sealed class ArrayAccessSyntax(ExecutionCSharpRenderer renderer, ExecutionRenderContext context)
{
    internal ExpressionSyntax Render(ExecutionArrayAccess arrayAccess)
    {
        if (arrayAccess.Array is ExecutionScriptParameterRead or ExecutionScriptVariableRead)
            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(SafeArrayAccess)),
                        SyntaxFactory.GenericName(nameof(SafeArrayAccess.GetReadOnlyListElement))
                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(CreateTypeSyntax(arrayAccess.ElementType))))))
                .WithArgumentList(CreateArgumentList(
                    renderer.RenderExpressionForTypedSink(arrayAccess.Array, context),
                    renderer.RenderExpressionForTypedSink(arrayAccess.Index, context)));

        var arrayType = arrayAccess.Array.ReturnType.RequireClrType();
        if (!arrayType.IsArray || arrayType.GetArrayRank() != 1 || arrayType.GetElementType() != arrayAccess.ElementType.RequireClrType())
            throw new InvalidOperationException("Indexed execution requires an exact concrete array contract.");
        var valueType = arrayAccess.InterpretationShape?.TypeName is { } typeName
            ? SyntaxFactory.ParseTypeName(typeName) : CreateTypeSyntax(arrayAccess.ElementType);
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(SafeArrayAccess)),
                    SyntaxFactory.GenericName(nameof(SafeArrayAccess.GetArrayElement)).WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(valueType)))))
            .WithArgumentList(CreateArgumentList(renderer.RenderExpressionForTypedSink(arrayAccess.Array, context),
                renderer.RenderExpressionForTypedSink(arrayAccess.Index, context)));
        return SyntaxFactory.CastExpression(valueType, invocation);
    }
}
