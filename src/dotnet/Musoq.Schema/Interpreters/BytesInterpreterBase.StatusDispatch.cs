namespace Musoq.Schema.Interpreters;

public abstract partial class BytesInterpreterBase<TOut>
{
    ParseAttempt<TOut> IBytesInterpreterStatus<TOut>.ReadAt(ReadOnlySpan<byte> data, int offset)
    {
        try
        {
            return ParseAttempt<TOut>.Success(InterpretAt(data, offset));
        }
        catch (ParseException failure)
        {
            return ParseAttempt<TOut>.Failed(_partialParsedValue, ParseFailure.FromException(failure));
        }
    }
}
