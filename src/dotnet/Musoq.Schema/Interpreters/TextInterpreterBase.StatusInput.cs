namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    /// <summary>Checks a text cursor without throwing an expected input error.</summary>
    protected ParseFailure? CheckTextPositionStatus(ReadOnlySpan<char> text, string? fieldName = null)
    {
        if ((uint)ParsePosition <= (uint)text.Length)
            return null;
        var reason = ParsePosition < 0 ? "negative" : "past the end of the input";
        return ParseFailure.ForText(ParseErrorCode.InvalidPosition, SchemaName, fieldName, ParsePosition,
            $"Parse position {ParsePosition} is {reason}; valid range is 0 through {text.Length}");
    }

    /// <summary>Checks a fixed character read without throwing an expected input error.</summary>
    protected ParseFailure? PrepareCharsStatus(ReadOnlySpan<char> text, int count, string? fieldName = null)
    {
        if (count < 0)
            return ParseFailure.ForText(ParseErrorCode.InvalidSize, SchemaName, fieldName, ParsePosition,
                $"Negative character count: {count}");
        if (CheckTextPositionStatus(text, fieldName) is { } position)
            return position;
        return count > text.Length - ParsePosition
            ? ParseFailure.ForText(ParseErrorCode.InsufficientData, SchemaName, fieldName, ParsePosition,
                $"Attempted to read {count} characters at parse position {ParsePosition}, but only {Math.Max(0, text.Length - ParsePosition)} characters available")
            : null;
    }

    private ParseAttempt<T> FailText<T>(ParseErrorCode code, string? field, string details) =>
        ParseAttempt<T>.Failed(default, ParseFailure.ForText(code, SchemaName, field, ParsePosition, details));
}
