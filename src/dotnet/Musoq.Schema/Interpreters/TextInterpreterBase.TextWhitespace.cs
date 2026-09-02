namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    private static readonly char[] TextWhitespaceCharacters = [' ', '\t', '\r', '\n'];

    private static bool IsTextWhitespace(char value)
    {
        return value is ' ' or '\t' or '\r' or '\n';
    }

    protected string ReadWhitespace(ReadOnlySpan<char> text, string quantifier = "+", bool trim = false,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false, string? fieldName = null)
    {
        EnsureValidParsePosition(text, fieldName);
        var start = ParsePosition;
        var maximum = quantifier == "?" ? 1 : int.MaxValue;

        if (quantifier is not ("+" or "*" or "?"))
            throw new ParseException(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Unknown whitespace quantifier '{quantifier}'");

        while (ParsePosition < text.Length && ParsePosition - start < maximum &&
               IsTextWhitespace(text[ParsePosition]))
            ParsePosition++;

        if (quantifier == "+" && ParsePosition == start)
            throw new ParseException(
                ParseErrorCode.ExpectedWhitespace,
                SchemaName,
                fieldName,
                ParsePosition,
                "Expected whitespace");

        var value = text.Slice(start, ParsePosition - start).ToString();
        return ApplyModifiers(value, ltrim || trim, rtrim || trim, lower, upper);
    }
}
