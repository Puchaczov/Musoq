namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    /// <summary>
    ///     Flattens multiple context arrays into a single array, handling null contexts by inserting a null value.
    /// </summary>
    /// <param name="contexts">The context arrays to flatten.</param>
    /// <returns>
    ///     A single flattened array containing all context objects from the input arrays, with nulls for any null context
    ///     arrays.
    /// </returns>
    public static object?[] FlattenContexts(params object?[]?[] contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        if (contexts.Length == 1)
            return contexts[0] ?? [null];

        if (contexts.Length == 2)
        {
            var a = contexts[0];
            var b = contexts[1];
            var aLen = a?.Length ?? 1;
            var bLen = b?.Length ?? 1;

            if (aLen == 1 && bLen == 1)
                return [a?[0], b?[0]];

            var result2 = new object?[aLen + bLen];
            if (a != null) Array.Copy(a, 0, result2, 0, aLen);
            else result2[0] = null;
            if (b != null) Array.Copy(b, 0, result2, aLen, bLen);
            else result2[aLen] = null;
            return result2;
        }

        var size = 0;
        foreach (var context in contexts)
            size += context?.Length ?? 1;

        var result = new object?[size];
        var offset = 0;
        foreach (var context in contexts)
        {
            if (context != null)
            {
                Array.Copy(context, 0, result, offset, context.Length);
                offset += context.Length;
            }
            else
            {
                result[offset++] = null;
            }
        }

        return result;
    }
}
