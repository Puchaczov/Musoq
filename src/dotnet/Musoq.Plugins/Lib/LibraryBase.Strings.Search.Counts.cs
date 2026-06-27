using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Counts the number of occurrences of a substring within a string.
    /// </summary>
    /// <param name="value">The string to search in</param>
    /// <param name="substring">The substring to count</param>
    /// <returns>The number of occurrences, or null if either parameter is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? CountOccurrences(string? value, string? substring)
    {
        if (value == null || substring == null)
            return null;

        if (string.IsNullOrEmpty(substring))
            return 0;

        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }

    /// <summary>
    ///     Counts the number of words in the string.
    ///     Words are separated by whitespace characters.
    /// </summary>
    /// <param name="value">The string to count words in</param>
    /// <returns>The number of words</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? WordCount(string? value)
    {
        if (value == null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var count = 0;
        var inWord = false;

        foreach (var c in value)
            if (char.IsWhiteSpace(c))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                inWord = true;
                count++;
            }

        return count;
    }

    /// <summary>
    ///     Counts the number of lines in the string.
    ///     Lines are separated by newline characters.
    /// </summary>
    /// <param name="value">The string to count lines in</param>
    /// <returns>The number of lines</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? LineCount(string? value)
    {
        if (value == null)
            return null;

        if (value.Length == 0)
            return 0;

        var count = 1;

        for (var i = 0; i < value.Length; i++)
            if (value[i] == '\n')
                count++;
            else if (value[i] == '\r' && (i + 1 >= value.Length || value[i + 1] != '\n'))
                count++;

        return count;
    }

    /// <summary>
    ///     Counts the number of sentences in the string.
    ///     Sentences are delimited by period, exclamation mark, or question mark.
    /// </summary>
    /// <param name="value">The string to count sentences in</param>
    /// <returns>The number of sentences</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? SentenceCount(string? value)
    {
        if (value == null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var count = 0;
        var inSentence = false;

        foreach (var c in value)
            if (c is '.' or '!' or '?')
            {
                if (inSentence)
                {
                    count++;
                    inSentence = false;
                }
            }
            else if (!char.IsWhiteSpace(c))
            {
                inSentence = true;
            }


        if (inSentence)
            count++;

        return count;
    }
}
