using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal abstract partial class ExecutionIrRewriter
{
    protected virtual ExecutionSourceBinding RewriteSourceBinding(ExecutionSourceBinding binding)
    {
        var arguments = RewriteExpressionList(binding.Arguments);
        return ReferenceEquals(arguments, binding.Arguments) ? binding : binding with { Arguments = arguments };
    }

    protected virtual ExecutionRowValue RewriteRowValue(ExecutionRowValue value)
    {
        var expression = RewriteExpression(value.Value);
        return ReferenceEquals(expression, value.Value) ? value : value with { Value = expression };
    }

    protected virtual ExecutionCaseWhenBranch RewriteCaseWhenBranch(ExecutionCaseWhenBranch branch)
    {
        var condition = RewriteExpression(branch.Condition);
        var result = RewriteExpression(branch.Result);
        return ReferenceEquals(condition, branch.Condition) && ReferenceEquals(result, branch.Result)
            ? branch
            : branch with { Condition = condition, Result = result };
    }

    protected virtual ExecutionContextLayout? RewriteContextLayout(ExecutionContextLayout? layout)
    {
        if (layout == null)
            return null;

        var segments = RewriteContextSegments(layout.Segments);
        return ReferenceEquals(segments, layout.Segments) ? layout : layout with { Segments = segments };
    }

    protected virtual ExecutionContextSegment RewriteContextSegment(ExecutionContextSegment segment)
    {
        var value = RewriteExpression(segment.Value);
        return ReferenceEquals(value, segment.Value) ? segment : segment with { Value = value };
    }

    protected virtual ExecutionWindowOrderKey RewriteWindowOrderKey(ExecutionWindowOrderKey key)
    {
        var expression = RewriteExpression(key.Expression);
        return ReferenceEquals(expression, key.Expression) ? key : key with { Expression = expression };
    }

    protected virtual ExecutionAsOfEqualityKey RewriteAsOfEqualityKey(ExecutionAsOfEqualityKey key)
    {
        var left = RewriteExpression(key.Left);
        var right = RewriteExpression(key.Right);
        return ReferenceEquals(left, key.Left) && ReferenceEquals(right, key.Right)
            ? key
            : key with { Left = left, Right = right };
    }

    protected virtual ExecutionAsOfTieBreak? RewriteAsOfTieBreak(ExecutionAsOfTieBreak? tieBreak)
    {
        if (tieBreak == null)
            return null;

        var key = RewriteExpression(tieBreak.Key);
        return ReferenceEquals(key, tieBreak.Key) ? tieBreak : tieBreak with { Key = key };
    }

    protected virtual ExecutionCapacityHint RewriteCapacityHint(ExecutionCapacityHint capacityHint)
    {
        return capacityHint switch
        {
            ExecutionRowsCapacityHintCandidate candidate => RewriteRowsCapacityHintCandidate(candidate),
            _ => capacityHint
        };
    }

    protected virtual ExecutionCapacityHint RewriteRowsCapacityHintCandidate(ExecutionRowsCapacityHintCandidate candidate)
    {
        var rows = RewriteExpression(candidate.Rows);
        return ReferenceEquals(rows, candidate.Rows)
            ? candidate
            : candidate with { Rows = rows };
    }

    protected virtual ExecutionParallelTask RewriteParallelTask(ExecutionParallelTask task)
    {
        var body = RewriteBlock(task.Body);
        return ReferenceEquals(body, task.Body) ? task : task with { Body = body };
    }

    protected virtual ExecutionParallelMerge RewriteParallelMerge(ExecutionParallelMerge merge)
    {
        var body = RewriteBlock(merge.Body);
        return ReferenceEquals(body, merge.Body) ? merge : merge with { Body = body };
    }

    protected ExecutionExpression? RewriteOptionalExpression(ExecutionExpression? expression)
    {
        return expression == null ? null : RewriteExpression(expression);
    }

    protected ExecutionBlock? RewriteOptionalBlock(ExecutionBlock? block)
    {
        return block == null ? null : RewriteBlock(block);
    }

    protected ExecutionCapacityHint? RewriteOptionalCapacityHint(ExecutionCapacityHint? capacityHint)
    {
        return capacityHint == null ? null : RewriteCapacityHint(capacityHint);
    }

    protected T RewriteCapacityHintOwner<T>(
        T owner,
        ExecutionCapacityHint? capacityHint,
        Func<ExecutionCapacityHint?, T> replace)
        where T : class
    {
        var rewritten = RewriteOptionalCapacityHint(capacityHint);
        return ReferenceEquals(rewritten, capacityHint) ? owner : replace(rewritten);
    }

    protected T RewriteRequiredCapacityHintOwner<T>(
        T owner,
        ExecutionCapacityHint capacityHint,
        Func<ExecutionCapacityHint, T> replace)
        where T : class
    {
        var rewritten = RewriteCapacityHint(capacityHint);
        return ReferenceEquals(rewritten, capacityHint) ? owner : replace(rewritten);
    }

    protected T RewriteBlockOwner<T>(
        T owner,
        ExecutionBlock body,
        Func<ExecutionBlock, T> replace)
        where T : class
    {
        var rewritten = RewriteBlock(body);
        return ReferenceEquals(rewritten, body) ? owner : replace(rewritten);
    }

    protected T RewriteExpressionOwner<T>(
        T owner,
        ExecutionExpression expression,
        Func<ExecutionExpression, T> replace)
        where T : class
    {
        var rewritten = RewriteExpression(expression);
        return ReferenceEquals(rewritten, expression) ? owner : replace(rewritten);
    }

    protected T RewriteExpressionAndBlockOwner<T>(
        T owner,
        ExecutionExpression expression,
        ExecutionBlock body,
        Func<ExecutionExpression, ExecutionBlock, T> replace)
        where T : class
    {
        var rewrittenExpression = RewriteExpression(expression);
        var rewrittenBody = RewriteBlock(body);
        return ReferenceEquals(rewrittenExpression, expression) && ReferenceEquals(rewrittenBody, body)
            ? owner
            : replace(rewrittenExpression, rewrittenBody);
    }

    protected T RewriteKeyBlockAndOptionalBlockOwner<T>(
        T owner,
        ExecutionExpression key,
        ExecutionBlock body,
        ExecutionBlock? noMatchBody,
        Func<ExecutionExpression, ExecutionBlock, ExecutionBlock?, T> replace)
        where T : class
    {
        var rewrittenKey = RewriteExpression(key);
        var rewrittenBody = RewriteBlock(body);
        var rewrittenNoMatchBody = RewriteOptionalBlock(noMatchBody);
        return ReferenceEquals(rewrittenKey, key) &&
               ReferenceEquals(rewrittenBody, body) &&
               ReferenceEquals(rewrittenNoMatchBody, noMatchBody)
            ? owner
            : replace(rewrittenKey, rewrittenBody, rewrittenNoMatchBody);
    }

    protected T RewriteRowValuesOwner<T>(
        T owner,
        IReadOnlyList<ExecutionRowValue> values,
        Func<IReadOnlyList<ExecutionRowValue>, T> replace)
        where T : class
    {
        var rewritten = RewriteRowValues(values);
        return ReferenceEquals(rewritten, values) ? owner : replace(rewritten);
    }

    protected T RewriteRowValuesAndContextsOwner<T>(
        T owner,
        IReadOnlyList<ExecutionRowValue> values,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout,
        Func<IReadOnlyList<ExecutionRowValue>, IReadOnlyList<ExecutionExpression>, ExecutionContextLayout?, T> replace)
        where T : class
    {
        var rewrittenValues = RewriteRowValues(values);
        var rewrittenContexts = RewriteExpressionList(contexts);
        var rewrittenContextLayout = RewriteContextLayout(contextLayout);
        return ReferenceEquals(rewrittenValues, values) &&
               ReferenceEquals(rewrittenContexts, contexts) &&
               ReferenceEquals(rewrittenContextLayout, contextLayout)
            ? owner
            : replace(rewrittenValues, rewrittenContexts, rewrittenContextLayout);
    }

    protected IReadOnlyList<ExecutionExpression> RewriteExpressionList(IReadOnlyList<ExecutionExpression> expressions)
    {
        return RewriteList(expressions, RewriteExpression);
    }

    protected IReadOnlyList<ExecutionRowValue> RewriteRowValues(IReadOnlyList<ExecutionRowValue> values)
    {
        return RewriteList(values, RewriteRowValue);
    }

    protected IReadOnlyList<IReadOnlyList<ExecutionRowValue>> RewriteRowValueRows(
        IReadOnlyList<IReadOnlyList<ExecutionRowValue>> values)
    {
        return RewriteList(values, RewriteRowValues);
    }

    protected IReadOnlyList<ExecutionContextSegment> RewriteContextSegments(
        IReadOnlyList<ExecutionContextSegment> segments)
    {
        return RewriteList(segments, RewriteContextSegment);
    }

    protected IReadOnlyList<ExecutionWindowOrderKey> RewriteWindowOrderKeys(
        IReadOnlyList<ExecutionWindowOrderKey> keys)
    {
        return RewriteList(keys, RewriteWindowOrderKey);
    }

    protected IReadOnlyList<ExecutionAsOfEqualityKey> RewriteAsOfEqualityKeys(
        IReadOnlyList<ExecutionAsOfEqualityKey> keys)
    {
        return RewriteList(keys, RewriteAsOfEqualityKey);
    }

    protected IReadOnlyList<T> RewriteList<T>(IReadOnlyList<T> items, Func<T, T> rewrite)
        where T : class
    {
        T[]? rewritten = null;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var current = rewrite(item);
            if (ReferenceEquals(current, item) && rewritten == null)
                continue;

            rewritten ??= CopyPrefix(items, index);
            rewritten[index] = current;
        }

        return rewritten ?? items;
    }

    private static T[] CopyPrefix<T>(IReadOnlyList<T> items, int length)
    {
        var copy = new T[items.Count];
        for (var index = 0; index < length; index++)
            copy[index] = items[index];

        return copy;
    }
}
