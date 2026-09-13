using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class ExecutionLikeMatcherSyntaxFactory
{
    public static bool IsStateDeclaration(ExecutionLet declaration) =>
        declaration.Value is ExecutionPrepareLikeMatcher or ExecutionLikeMatcherCacheSlot;

    public static ExpressionSyntax Render(
        ExecutionExpression expression,
        Func<ExecutionExpression, ExpressionSyntax> renderChild,
        ExecutionRenderSession session) =>
        expression switch
        {
            ExecutionStringMatch stringMatch => ExecutionStringMatchSyntaxFactory.Render(
                stringMatch,
                renderChild(stringMatch.Input),
                session),
            ExecutionPrepareLikeMatcher prepareLike => InvokeOperators(
                nameof(Operators.PrepareLike),
                renderChild(prepareLike.Pattern)),
            ExecutionPreparedLikeMatch preparedLike => InvokeOperators(
                nameof(Operators.LikePrepared),
                renderChild(preparedLike.Input),
                renderChild(preparedLike.Matcher)),
            ExecutionDynamicLikeMatch dynamicLike => InvokeOperators(
                nameof(Operators.LikeDynamic),
                renderChild(dynamicLike.Input),
                renderChild(dynamicLike.Pattern),
                renderChild(dynamicLike.CacheSlot)),
            ExecutionLikeMatcherCacheSlot cacheSlot => SyntaxFactory
                .ObjectCreationExpression(CreateTypeSyntax(typeof(LikeMatcherCacheSlot)))
                .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(
                    cacheSlot.WorkerLocal
                        ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                        : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression.GetType().Name, "Unknown LIKE matcher expression.")
        };

    public static bool CanRender(
        ExecutionExpression expression,
        Func<ExecutionExpression, bool> canRenderChild,
        Func<ExecutionTypeRef, bool> canReferenceType) =>
        expression switch
        {
            ExecutionStringMatch match =>
                match.Comparison == ExecutionStringMatchComparison.LikeIgnoreCase &&
                match.Kind is (ExecutionStringMatchKind.Exact or ExecutionStringMatchKind.Prefix or
                    ExecutionStringMatchKind.Suffix or ExecutionStringMatchKind.Contains) &&
                canRenderChild(match.Input) && canReferenceType(match.ReturnType),
            ExecutionPrepareLikeMatcher prepareLike =>
                prepareLike.Comparison == ExecutionStringMatchComparison.LikeIgnoreCase &&
                prepareLike.ReturnType.RequireClrType() == typeof(PreparedLikeMatcher) &&
                canRenderChild(prepareLike.Pattern),
            ExecutionPreparedLikeMatch preparedLike =>
                preparedLike.ReturnType.RequireClrType() == typeof(bool) &&
                preparedLike.Matcher.ReturnType.RequireClrType() == typeof(PreparedLikeMatcher) &&
                canRenderChild(preparedLike.Input) &&
                canRenderChild(preparedLike.Matcher),
            ExecutionDynamicLikeMatch dynamicLike =>
                dynamicLike.Comparison == ExecutionStringMatchComparison.LikeIgnoreCase &&
                dynamicLike.ReturnType.RequireClrType() == typeof(bool) &&
                dynamicLike.CacheSlot.ReturnType.RequireClrType() == typeof(LikeMatcherCacheSlot) &&
                canRenderChild(dynamicLike.Input) &&
                canRenderChild(dynamicLike.Pattern) &&
                canRenderChild(dynamicLike.CacheSlot),
            ExecutionLikeMatcherCacheSlot cacheSlot =>
                cacheSlot.ReturnType.RequireClrType() == typeof(LikeMatcherCacheSlot),
            _ => false
        };

    private static InvocationExpressionSyntax InvokeOperators(
        string methodName,
        params ExpressionSyntax[] arguments) =>
        SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Operators)),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(arguments));
}
