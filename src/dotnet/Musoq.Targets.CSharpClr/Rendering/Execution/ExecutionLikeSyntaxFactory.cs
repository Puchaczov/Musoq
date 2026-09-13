using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Targets.CSharpClr;

internal static class ExecutionLikeSyntaxFactory
{
    internal static bool CanRender(ExecutionPatternMatch patternMatch) =>
        patternMatch.Kind is PatternKind.Like or PatternKind.RLike;

    internal static InvocationExpressionSyntax Render(
        ExecutionPatternMatch patternMatch,
        Func<ExecutionExpression, ExpressionSyntax> renderExpression)
    {
        ArgumentNullException.ThrowIfNull(renderExpression);

        var methodName = patternMatch.Kind switch
        {
            PatternKind.Like => nameof(Operators.Like),
            PatternKind.RLike => nameof(Operators.RLike),
            _ => throw UnsupportedShape.Of($"Pattern kind {patternMatch.Kind}")
        };

        var target = SyntaxFactory.ObjectCreationExpression(ExecutionSyntaxFactory.CreateTypeSyntax(typeof(Operators)))
            .WithArgumentList(SyntaxFactory.ArgumentList());
        var arguments = ExecutionSyntaxFactory.CreateArgumentList(
            renderExpression(patternMatch.Expression),
            renderExpression(patternMatch.Pattern));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    target,
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(arguments);
    }
}
