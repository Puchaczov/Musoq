using System.Text;

namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    private static string ApplyPatternGreediness(string pattern, bool greedy, bool lazy)
    {
        if (!greedy && !lazy)
            return pattern;

        var builder = new StringBuilder(pattern.Length + 8);
        var inCharacterClass = false;

        for (var i = 0; i < pattern.Length; i++)
        {
            var current = pattern[i];
            if (current == '\\')
            {
                builder.Append(current);
                if (i + 1 < pattern.Length)
                    builder.Append(pattern[++i]);
                continue;
            }

            if (current == '[')
                inCharacterClass = true;
            else if (current == ']' && inCharacterClass)
                inCharacterClass = false;

            if (!inCharacterClass && current is '*' or '+')
            {
                builder.Append(current);
                AppendQuantifierMode(builder, pattern, ref i, greedy, lazy);
                continue;
            }

            if (!inCharacterClass && current == '?' && (i == 0 || pattern[i - 1] != '('))
            {
                builder.Append(current);
                AppendQuantifierMode(builder, pattern, ref i, greedy, lazy);
                continue;
            }

            if (!inCharacterClass && current == '{' && IsQuantifier(pattern, i, out var end))
            {
                builder.Append(pattern.AsSpan(i, end - i + 1));
                i = end;
                AppendQuantifierMode(builder, pattern, ref i, greedy, lazy);
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static void AppendQuantifierMode(StringBuilder builder, string pattern, ref int index, bool greedy,
        bool lazy)
    {
        var hasLazyMarker = index + 1 < pattern.Length && pattern[index + 1] == '?';
        if (hasLazyMarker)
        {
            if (!greedy)
                builder.Append('?');
            index++;
        }
        else if (lazy)
        {
            builder.Append('?');
        }
    }

    private static bool IsQuantifier(string pattern, int start, out int end)
    {
        end = start + 1;
        var digitsBeforeComma = 0;
        while (end < pattern.Length && char.IsDigit(pattern[end]))
        {
            digitsBeforeComma++;
            end++;
        }

        if (digitsBeforeComma == 0)
            return false;

        if (end < pattern.Length && pattern[end] == ',')
        {
            end++;
            while (end < pattern.Length && char.IsDigit(pattern[end])) end++;
        }

        if (end >= pattern.Length || pattern[end] != '}')
            return false;

        return true;
    }
}
