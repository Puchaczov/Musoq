namespace Musoq.Evaluator.Tables;

public static class ContextMaterializer
{
    public static object?[]? Read(Row? row)
    {
        return row?.Contexts;
    }

    public static object?[]? Merge(object?[]? leftContexts, object?[]? rightContexts)
    {
        if (leftContexts == null)
            return rightContexts;

        if (rightContexts == null)
            return leftContexts;

        var result = new object?[leftContexts.Length + rightContexts.Length];
        Array.Copy(leftContexts, 0, result, 0, leftContexts.Length);
        Array.Copy(rightContexts, 0, result, leftContexts.Length, rightContexts.Length);
        return result;
    }

    public static object?[] MergePreservingNullSegments(object?[]? leftContexts, object?[]? rightContexts)
    {
        if (leftContexts == null)
            return Prepend(null, rightContexts);

        if (rightContexts == null)
            return Append(leftContexts, null);

        return Merge(leftContexts, rightContexts)
               ?? throw new InvalidOperationException("Both context segments were expected to be materialized.");
    }

    public static object?[] Append(object?[]? contexts, object? context)
    {
        if (contexts == null)
            return [context];

        var result = new object?[contexts.Length + 1];
        Array.Copy(contexts, 0, result, 0, contexts.Length);
        result[contexts.Length] = context;
        return result;
    }

    public static object?[] AppendPreservingNullSegment(object?[]? contexts, object? context)
    {
        return contexts == null
            ? [null, context]
            : Append(contexts, context);
    }

    public static object?[] Prepend(object? context, object?[]? contexts)
    {
        if (contexts == null)
            return [context];

        var result = new object?[contexts.Length + 1];
        result[0] = context;
        Array.Copy(contexts, 0, result, 1, contexts.Length);
        return result;
    }

    public static object?[] PrependPreservingNullSegment(object? context, object?[]? contexts)
    {
        return contexts == null
            ? [context, null]
            : Prepend(context, contexts);
    }
}
