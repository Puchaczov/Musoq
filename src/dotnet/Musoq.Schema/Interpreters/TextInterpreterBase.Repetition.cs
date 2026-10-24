namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    /// <summary>
    ///     Guards a text repeat against an input or schema that never reaches its terminator.
    /// </summary>
    protected void EnsureRepeatIteration(string fieldName, int iteration) =>
        EnsureRepeatIteration(fieldName, iteration, 10_000);

    /// <summary>Guards a repeat using a positive maximum supplied by the compiled interpreter.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    protected void EnsureRepeatIteration(string fieldName, int iteration, int maxIterations)
    {
        if (iteration >= maxIterations)
            ThrowRepeatIterationLimit(fieldName, maxIterations);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void ThrowRepeatIterationLimit(string fieldName, int maxIterations)
    {
        throw ParseException.ForText(
            ParseErrorCode.MaxIterationsExceeded,
            SchemaName,
            fieldName,
            ParsePosition,
            $"Repeat field '{fieldName}' exceeded the maximum of {maxIterations} iterations.");
    }

    /// <summary>
    ///     Guards a text repeat against an element schema that consumes no characters.
    /// </summary>
    protected void EnsureRepeatMadeProgress(string fieldName, int startPosition)
    {
        if (ParsePosition > startPosition)
            return;

        throw ParseException.ForText(
            ParseErrorCode.MaxIterationsExceeded,
            SchemaName,
            fieldName,
            ParsePosition,
            $"Repeat field '{fieldName}' made no progress reading an element; the element type consumes zero characters.");
    }
}
