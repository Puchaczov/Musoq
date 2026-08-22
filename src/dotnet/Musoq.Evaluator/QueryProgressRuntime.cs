using System.Collections.Generic;

namespace Musoq.Evaluator;

/// <summary>
///     Low-overhead helpers used by generated execution code at source boundaries.
/// </summary>
public static class QueryProgressRuntime
{
    public static IEnumerable<IReadOnlyList<T>> WrapChunks<T>(
        IEnumerable<IReadOnlyList<T>> chunks,
        QueryRunContext context,
        string sourceContextId)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceContextId);

        return context.CreateProgressChunks(chunks, sourceContextId);
    }
}
