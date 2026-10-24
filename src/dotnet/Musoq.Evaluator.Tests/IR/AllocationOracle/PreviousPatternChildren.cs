using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static class PreviousPatternChildren
{
    public static IReadOnlyList<ExecutionExpression> GetChildren(ExecutionExpression expression) =>
        expression switch
        {
            ExecutionPatternMatch pattern => [pattern.Expression, pattern.Pattern],
            ExecutionStringMatch stringMatch => [stringMatch.Input],
            ExecutionPrepareLikeMatcher prepareLike => [prepareLike.Pattern],
            ExecutionPreparedLikeMatch preparedLike => [preparedLike.Input, preparedLike.Matcher],
            ExecutionDynamicLikeMatch dynamicLike => [dynamicLike.Input, dynamicLike.Pattern, dynamicLike.CacheSlot],
            ExecutionLikeMatcherCacheSlot => [],
            ExecutionPrepareRLikeMatcher prepareRLike => [prepareRLike.Pattern],
            ExecutionPreparedRLikeMatch preparedRLike => [preparedRLike.Input, preparedRLike.Matcher],
            ExecutionDynamicRLikeMatch dynamicRLike => [dynamicRLike.Input, dynamicRLike.Pattern, dynamicRLike.CacheSlot],
            ExecutionRLikeMatcherCacheSlot => [],
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression.GetType().Name, "Unknown pattern expression.")
        };

}
