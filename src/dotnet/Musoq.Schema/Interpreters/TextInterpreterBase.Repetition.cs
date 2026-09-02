namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    /// <summary>
    ///     Guards a text repeat against an input or schema that never reaches its terminator.
    /// </summary>
    protected void EnsureRepeatIteration(string fieldName, int iteration)
    {
        const int maxIterations = 10_000;
        if (iteration < maxIterations)
            return;

        throw new ParseException(
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

        throw new ParseException(
            ParseErrorCode.MaxIterationsExceeded,
            SchemaName,
            fieldName,
            ParsePosition,
            $"Repeat field '{fieldName}' made no progress reading an element; the element type consumes zero characters.");
    }
}
