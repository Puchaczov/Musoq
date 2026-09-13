using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution;

internal delegate bool TryRewritePatternExpression(
    ExecutionExpression expression,
    [NotNullWhen(true)] out ExecutionExpression? rewritten);

internal static class PatternExpressionFacts
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
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression.GetType().Name, "Unknown pattern expression.")
        };

    public static ExecutionExpression RewriteChildren(
        ExecutionExpression expression,
        Func<ExecutionExpression, ExecutionExpression> rewrite)
    {
        var children = GetChildren(expression);
        var rewritten = new ExecutionExpression[children.Count];
        var changed = false;
        for (var index = 0; index < children.Count; index++)
        {
            rewritten[index] = rewrite(children[index]);
            changed |= !ReferenceEquals(rewritten[index], children[index]);
        }

        return changed ? WithChildren(expression, rewritten) : expression;
    }

    public static bool TryRewriteChildren(
        ExecutionExpression expression,
        TryRewritePatternExpression rewrite,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        var children = GetChildren(expression);
        var rewrittenChildren = new ExecutionExpression[children.Count];
        var changed = false;
        for (var index = 0; index < children.Count; index++)
        {
            if (!rewrite(children[index], out var rewrittenChild))
            {
                rewritten = null;
                return false;
            }

            rewrittenChildren[index] = rewrittenChild;
            changed |= !ReferenceEquals(rewrittenChildren[index], children[index]);
        }

        rewritten = changed ? WithChildren(expression, rewrittenChildren) : expression;
        return true;
    }

    public static bool AnyChild(
        ExecutionExpression expression,
        Func<ExecutionExpression, bool> predicate)
    {
        foreach (var child in GetChildren(expression))
        {
            if (predicate(child))
                return true;
        }

        return false;
    }

    public static bool AllChildren(
        ExecutionExpression expression,
        Func<ExecutionExpression, bool> predicate)
    {
        foreach (var child in GetChildren(expression))
        {
            if (!predicate(child))
                return false;
        }

        return true;
    }

    private static ExecutionExpression WithChildren(
        ExecutionExpression expression,
        IReadOnlyList<ExecutionExpression> children) =>
        expression switch
        {
            ExecutionPatternMatch pattern => pattern with { Expression = children[0], Pattern = children[1] },
            ExecutionStringMatch stringMatch => stringMatch with { Input = children[0] },
            ExecutionPrepareLikeMatcher prepareLike => prepareLike with { Pattern = children[0] },
            ExecutionPreparedLikeMatch preparedLike => preparedLike with { Input = children[0], Matcher = children[1] },
            ExecutionDynamicLikeMatch dynamicLike => dynamicLike with
            {
                Input = children[0],
                Pattern = children[1],
                CacheSlot = children[2]
            },
            ExecutionLikeMatcherCacheSlot => expression,
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression.GetType().Name, "Unknown pattern expression.")
        };
}
