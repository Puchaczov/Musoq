using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ParenthesizedExpressionSyntax RenderLibraryBaseStringPredicate(
        ExecutionMethodCall methodCall, string methodName, ExecutionRenderContext context)
    {
        var value = RenderStringPredicateArgument(methodCall.Arguments[0], context);
        var pattern = RenderStringPredicateArgument(methodCall.Arguments[1], context);
        var names = ExecutionExpressionCaptureNames.Reserve(context, value, pattern);
        var valueLocal = SyntaxFactory.IdentifierName(names.Value);
        var literalPattern = methodCall.Arguments[1] is ExecutionLiteral;
        ExpressionSyntax patternLocal = literalPattern ? pattern : SyntaxFactory.IdentifierName(names.Pattern);
        var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        var hasNull = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression,
            SyntaxFactory.IsPatternExpression(valueLocal, SyntaxFactory.ConstantPattern(nullLiteral)),
            SyntaxFactory.IsPatternExpression(patternLocal, SyntaxFactory.ConstantPattern(nullLiteral)));
        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression, valueLocal, SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(patternLocal, CreateOrdinalIgnoreCaseStringComparisonExpression()));
        var result = SyntaxFactory.ConditionalExpression(hasNull,
            SyntaxFactory.CastExpression(CreateTypeSyntax(typeof(bool?)), nullLiteral), invocation);
        ExpressionSyntax body = result;
        if (!literalPattern)
            body = ExecutionValueCaptureSyntax.Capture(pattern, names.Pattern, body);
        return ExecutionValueCaptureSyntax.Capture(value, names.Value, body);
    }

    private ExpressionSyntax RenderStringPredicateArgument(ExecutionExpression argument, ExecutionRenderContext context)
    {
        var value = RenderExpression(argument, context);
        return argument is ExecutionLiteral { Value.Kind: ExecutionConstantKind.Null }
            ? SyntaxFactory.CastExpression(CreateTypeSyntax(typeof(string)), value) : value;
    }
}
