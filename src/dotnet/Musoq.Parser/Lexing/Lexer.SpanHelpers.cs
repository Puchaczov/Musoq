using System.Runtime.CompilerServices;

namespace Musoq.Parser.Lexing;

public sealed partial class Lexer
{
    private enum PhraseBoundary
    {
        WhitespaceOrEnd,
        WordBoundary,
        WhitespaceRightParenOrEnd
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryMatchWord(string word, PhraseBoundary boundary, out int end)
    {
        var start = Position;
        if (!MatchesAt(start, word))
        {
            end = start;
            return false;
        }

        end = start + word.Length;
        if (!MatchesBoundary(end, boundary))
            return false;

        Position = end;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryMatchTwoWords(string first, string second, PhraseBoundary boundary, out int end)
    {
        var firstEnd = Position;
        end = firstEnd;
        if (!MatchesAt(firstEnd, first))
        {
            return false;
        }

        firstEnd += first.Length;
        if (firstEnd >= Input.Length || !FastCharacterClassifier.IsWhitespace(Input[firstEnd]))
        {
            end = firstEnd;
            return false;
        }

        while (firstEnd < Input.Length && FastCharacterClassifier.IsWhitespace(Input[firstEnd]))
            firstEnd++;

        if (!MatchesAt(firstEnd, second))
        {
            end = firstEnd;
            return false;
        }

        end = firstEnd + second.Length;
        if (!MatchesBoundary(end, boundary))
            return false;

        Position = end;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryMatchThreeWords(
        string first,
        string second,
        string third,
        PhraseBoundary boundary,
        out int end)
    {
        var firstEnd = Position;
        end = firstEnd;
        if (!MatchesAt(firstEnd, first))
        {
            return false;
        }

        firstEnd += first.Length;
        if (!TryReadSeparatedWord(firstEnd, second, out var secondEnd) ||
            !TryReadSeparatedWord(secondEnd, third, out end))
            return false;

        if (!MatchesBoundary(end, boundary))
            return false;

        Position = end;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryMatchFourWords(
        string first,
        string second,
        string third,
        string fourth,
        PhraseBoundary boundary,
        out int end)
    {
        var firstEnd = Position;
        end = firstEnd;
        if (!MatchesAt(firstEnd, first))
        {
            return false;
        }

        firstEnd += first.Length;
        if (!TryReadSeparatedWord(firstEnd, second, out var secondEnd) ||
            !TryReadSeparatedWord(secondEnd, third, out var thirdEnd) ||
            !TryReadSeparatedWord(thirdEnd, fourth, out end))
            return false;

        if (!MatchesBoundary(end, boundary))
            return false;

        Position = end;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryReadSeparatedWord(int start, string word, out int end)
    {
        end = start;
        if (start >= Input.Length || !FastCharacterClassifier.IsWhitespace(Input[start]))
            return false;

        while (end < Input.Length && FastCharacterClassifier.IsWhitespace(Input[end]))
            end++;

        if (!MatchesAt(end, word))
            return false;

        end += word.Length;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MatchesAt(int start, string value)
    {
        return start >= 0 && start + value.Length <= Input.Length &&
               Input.AsSpan(start, value.Length).Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MatchesBoundary(int end, PhraseBoundary boundary)
    {
        return boundary switch
        {
            PhraseBoundary.WhitespaceOrEnd => end == Input.Length ||
                                              FastCharacterClassifier.IsWhitespace(Input[end]),
            PhraseBoundary.WordBoundary => end == Input.Length ||
                                           !FastCharacterClassifier.IsIdentifierContinue(Input[end]),
            PhraseBoundary.WhitespaceRightParenOrEnd => end == Input.Length ||
                                                         Input[end] == ')' ||
                                                         FastCharacterClassifier.IsWhitespace(Input[end]),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHashSourceCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value is '_' or '*' or '?';
    }
}
