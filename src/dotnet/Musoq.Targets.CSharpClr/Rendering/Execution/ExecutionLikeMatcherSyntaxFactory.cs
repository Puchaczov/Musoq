using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class ExecutionLikeMatcherSyntaxFactory
{
    public static bool IsStateDeclaration(ExecutionLet declaration) =>
        declaration.Value is ExecutionPrepareLikeMatcher or ExecutionLikeMatcherCacheSlot or
            ExecutionPrepareRLikeMatcher or ExecutionRLikeMatcherCacheSlot;

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
            ExecutionPrepareRLikeMatcher prepareRLike => InvokeOperators(
                nameof(Operators.PrepareRLike),
                renderChild(prepareRLike.Pattern)),
            ExecutionPreparedRLikeMatch preparedRLike => InvokeOperators(
                nameof(Operators.RLikePrepared),
                renderChild(preparedRLike.Input),
                renderChild(preparedRLike.Matcher)),
            ExecutionDynamicRLikeMatch dynamicRLike => InvokeOperators(
                nameof(Operators.RLikeDynamic),
                renderChild(dynamicRLike.Input),
                renderChild(dynamicRLike.Pattern),
                renderChild(dynamicRLike.CacheSlot)),
            ExecutionRLikeMatcherCacheSlot cacheSlot => SyntaxFactory
                .ObjectCreationExpression(CreateTypeSyntax(typeof(RLikeMatcherCacheSlot)))
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
                Enum.IsDefined(match.Comparison) &&
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
            ExecutionPrepareRLikeMatcher prepareRLike =>
                prepareRLike.ReturnType.RequireClrType() == typeof(PreparedRLikeMatcher) &&
                canRenderChild(prepareRLike.Pattern),
            ExecutionPreparedRLikeMatch preparedRLike =>
                preparedRLike.ReturnType.RequireClrType() == typeof(bool) &&
                preparedRLike.Matcher.ReturnType.RequireClrType() == typeof(PreparedRLikeMatcher) &&
                canRenderChild(preparedRLike.Input) &&
                canRenderChild(preparedRLike.Matcher),
            ExecutionDynamicRLikeMatch dynamicRLike =>
                dynamicRLike.ReturnType.RequireClrType() == typeof(bool) &&
                dynamicRLike.CacheSlot.ReturnType.RequireClrType() == typeof(RLikeMatcherCacheSlot) &&
                canRenderChild(dynamicRLike.Input) &&
                canRenderChild(dynamicRLike.Pattern) &&
                canRenderChild(dynamicRLike.CacheSlot),
            ExecutionRLikeMatcherCacheSlot cacheSlot =>
                cacheSlot.ReturnType.RequireClrType() == typeof(RLikeMatcherCacheSlot),
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
