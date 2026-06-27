using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderCachedMethodCall(ExecutionMethodCall methodCall)
    {
        if (methodCall.Target == null)
            throw new NotSupportedException($"Cached method {methodCall.Method.Name} requires a reusable target.");
        if (methodCall.Arguments.Count != 1)
            throw new NotSupportedException($"Cached method {methodCall.Method.Name} requires exactly one argument.");

        const string cacheTargetParameter = "__cacheTarget";
        const string cacheKeyParameter = "__cacheKey";

        var cacheType = methodCall.Arguments[0].ReturnType;
        var helperName = SyntaxFactory.GenericName(nameof(EvaluationHelper.GetOrAddCachedMethod))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
            [
                CreateTypeSyntax(methodCall.Target.Type),
                    CreateTypeSyntax(cacheType),
                    CreateTypeSyntax(methodCall.ReturnType)
            ])));

        var methodName = CreateMethodNameSyntax(methodCall.Method);
        var factory = SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(cacheTargetParameter),
                            methodName))
                    .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(cacheKeyParameter))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(cacheTargetParameter)),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(cacheKeyParameter))
            ])));

        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    helperName))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(methodCall.Cache!.Name),
                SyntaxFactory.IdentifierName(methodCall.Target.Name),
                RenderExpression(methodCall.Arguments[0]),
                factory));

        return CastIfNeeded(invocation, methodCall.ReturnType);
    }
}
