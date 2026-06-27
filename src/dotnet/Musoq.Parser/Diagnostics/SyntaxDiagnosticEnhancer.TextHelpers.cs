namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Enriches syntax and lexer diagnostics with user-facing guidance for mistyped keywords,
///     unsupported dialect constructs, and malformed input.
/// </summary>
internal static partial class SyntaxDiagnosticEnhancer
{
    private static string? ExtractWordAt(string text, TextSpan span)
    {
        var end = Math.Min(span.Start + span.Length, text.Length);
        if (end <= span.Start)
            return null;

        // Only extract if the span sits on a word boundary (whitespace/start/end on both sides)
        if (span.Start > 0 && !char.IsWhiteSpace(text[span.Start - 1]))
            return null;

        if (end < text.Length && !char.IsWhiteSpace(text[end]))
            return null;

        var word = text[span.Start..end].Trim();
        if (string.IsNullOrWhiteSpace(word))
            return null;

        foreach (var ch in word)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_')
                return null;
        }

        return word;
    }

    private static string? GetFirstWord(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = 0;
        while (start < text.Length && char.IsWhiteSpace(text[start]))
            start++;

        if (start >= text.Length)
            return null;

        var end = start;
        while (end < text.Length && (char.IsLetter(text[end]) || text[end] == '_'))
            end++;

        return end > start ? text[start..end] : null;
    }

    private static bool ContainsWholeWord(string text, string word)
    {
        var index = text.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            var startsAtBoundary = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var endIndex = index + word.Length;
            var endsAtBoundary = endIndex >= text.Length || !char.IsLetterOrDigit(text[endIndex]);

            if (startsAtBoundary && endsAtBoundary)
                return true;

            index = text.IndexOf(word, endIndex, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string AppendSentence(string message, string sentence)
    {
        if (string.IsNullOrWhiteSpace(message))
            return sentence;

        if (message.Contains(sentence, StringComparison.Ordinal))
            return message;

        return message.EndsWith('.') ? $"{message} {sentence}" : $"{message}. {sentence}";
    }

}
