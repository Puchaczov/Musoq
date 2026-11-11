namespace Musoq.Schema.Interpreters;

/// <summary>Typed status dispatch with a compatibility fallback for established interpreters.</summary>
public static class InterpretationStatus
{
    /// <summary>Interprets bytes without requiring an external interpreter to implement the status protocol.</summary>
    public static ParseAttempt<T> ReadAt<T>(IBytesInterpreter<T> interpreter, ReadOnlySpan<byte> data, int offset)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        try
        {
            if (interpreter is IBytesInterpreterStatus<T> status)
                return status.ReadAt(data, offset);
            return ParseAttempt<T>.Success(interpreter.InterpretAt(data, offset));
        }
        catch (ParseException failure) { return ParseAttempt<T>.Failed(default, ParseFailure.FromException(failure)); }
    }

    /// <summary>Parses text without requiring an external interpreter to implement the status protocol.</summary>
    public static ParseAttempt<T> ReadAt<T>(ITextInterpreter<T> interpreter, ReadOnlySpan<char> data, int offset)
    {
        var input = new TextParseInput(data);
        return ReadAt(interpreter, ref input, offset);
    }

    internal static ParseAttempt<T> ReadAt<T>(ITextInterpreter<T> interpreter, ref TextParseInput input, int offset)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        try
        {
            if (interpreter is ITextInterpreterStatus<T> status)
                return status.ReadAt(ref input, offset);
            return ParseAttempt<T>.Success(interpreter.ParseAt(input.Text, offset));
        }
        catch (ParseException failure) { return ParseAttempt<T>.Failed(default, ParseFailure.FromException(failure)); }
    }

    /// <summary>Returns a binary result or the established tolerant default value.</summary>
    public static T? TryInterpret<T>(IBytesInterpreter<T> interpreter, ReadOnlySpan<byte> data)
    {
        var result = ReadAt(interpreter, data, 0);
        return result.Succeeded ? result.Value : default;
    }

    /// <summary>Parses immutable text while retaining it for any capture-producing readers.</summary>
    public static T ParseText<T>(ITextInterpreter<T> interpreter, string? data)
    {
        var input = new TextParseInput(data, suppressFailureDetails: false);
        return ReadAt(interpreter, ref input, 0).GetValueOrThrow();
    }

    /// <summary>Returns a text result or the established tolerant default value.</summary>
    public static T? TryParse<T>(ITextInterpreter<T> interpreter, string? data)
    {
        var input = new TextParseInput(data, suppressFailureDetails: true);
        var result = ReadAt(interpreter, ref input, 0);
        return result.Succeeded ? result.Value : default;
    }
}
