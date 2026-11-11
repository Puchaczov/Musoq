namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    private static readonly char[] TextWhitespaceCharacters = [' ', '\t', '\r', '\n'];

    private static bool IsTextWhitespace(char value)
    {
        return value is ' ' or '\t' or '\r' or '\n';
    }

    protected ParseAttempt<string> ReadWhitespaceStatus(
        ReadOnlySpan<char> text,
        string quantifier = "+",
        bool trim = false,
        bool ltrim = false,
        bool rtrim = false,
        bool lower = false,
        bool upper = false,
        string? fieldName = null
    )
    {
        if (CheckTextPositionStatus(text, fieldName) is { } failure)
            return ParseAttempt<string>.Failed(default, failure);
        var start = ParsePosition;
        var maximum = quantifier == "?" ? 1 : int.MaxValue;

        if (quantifier is not ("+" or "*" or "?"))
            return FailText<string>(
                ParseErrorCode.GeneralError,
                fieldName,
                $"Unknown whitespace quantifier '{quantifier}'"
            );

        while (ParsePosition < text.Length && ParsePosition - start < maximum && IsTextWhitespace(text[ParsePosition]))
            ParsePosition++;

        if (quantifier == "+" && ParsePosition == start)
            return FailText<string>(ParseErrorCode.ExpectedWhitespace, fieldName, "Expected whitespace");

        var value = text.Slice(start, ParsePosition - start).ToString();
        return ParseAttempt<string>.Success(ApplyModifiers(value, ltrim || trim, rtrim || trim, lower, upper));
    }
}
