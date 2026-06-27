using System.Linq;
using Fastenshtein;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Computes soundex for the specified value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Soundex code</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Soundex(string? value)
    {
        if (value == null)
            return null;

        return _soundex.For(value);
    }

    /// <summary>
    ///     Matches the specified text by splitting it with separator and applying fuzzy comparison
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="word">The word</param>
    /// <param name="separator">The separator</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool HasFuzzyMatchedWord(string text, string word, string separator = " ")
    {
        ArgumentNullException.ThrowIfNull(separator);
        if (string.IsNullOrEmpty(word))
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var soundExWord = _soundex.For(word);
        var square = (int)Math.Ceiling(Math.Sqrt(word.Length));

        foreach (var tokenizedWord in text.Split(separator[0]))
            if (soundExWord == _soundex.For(tokenizedWord) || LevenshteinDistance(word, tokenizedWord) <= square)
                return true;

        return false;
    }

    /// <summary>
    ///     Matches the specified text by splitting it with separator and applying fuzzy comparison with a given distance
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="word">The word</param>
    /// <param name="distance">The distance</param>
    /// <param name="separator">The separator</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool HasWordThatHasSmallerLevenshteinDistanceThan(string text, string word, int distance,
        string separator = " ")
    {
        ArgumentNullException.ThrowIfNull(separator);
        if (string.IsNullOrEmpty(word))
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var tokenizedWord in text.Split(separator[0]))
            if (tokenizedWord == word || LevenshteinDistance(tokenizedWord, word) <= distance)
                return true;

        return false;
    }

    /// <summary>
    ///     Matches whether the specified word is present after being fuzzified within the specified text
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="word">The word</param>
    /// <param name="separator">The separator</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool HasWordThatSoundLike(string text, string word, string separator = " ")
    {
        ArgumentNullException.ThrowIfNull(separator);
        if (string.IsNullOrEmpty(word))
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var soundExWord = _soundex.For(word);

        foreach (var tokenizedWord in text.Split(separator[0]))
            if (soundExWord == _soundex.For(tokenizedWord))
                return true;

        return false;
    }

    /// <summary>
    ///     Matches whether the specified text is present in sentence after being fuzified
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="sentence">The sentence</param>
    /// <param name="separator">The separator</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool HasTextThatSoundLikeSentence(string text, string sentence, string separator = " ")
    {
        ArgumentNullException.ThrowIfNull(separator);
        if (string.IsNullOrEmpty(sentence))
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var words = sentence.Split(separator[0]);
        var tokens = text.Split(separator[0]);
        var wordsMatchTable = new bool[words.Length];

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var soundExWord = _soundex.For(word);

            foreach (var token in tokens)
                if (soundExWord == _soundex.For(token))
                {
                    wordsMatchTable[i] = true;
                    break;
                }
        }

        return wordsMatchTable.All(entry => entry);
    }

    /// <summary>
    ///     Computes the Levenshtein distance between two strings
    /// </summary>
    /// <param name="firstValue">The firstValue</param>
    /// <param name="secondValue">The secondValue</param>
    /// <returns>Levenshtein distance</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? LevenshteinDistance(string? firstValue, string? secondValue)
    {
        if (firstValue == null || secondValue == null)
            return null;

        return Levenshtein.Distance(firstValue, secondValue);
    }

    /// <summary>
    ///     Computes the longest common subsequence between two source and pattern
    /// </summary>
    /// <param name="source">The source</param>
    /// <param name="pattern">The pattern</param>
    /// <returns>Longest common subsequence</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? LongestCommonSubstring(string source, string pattern)
    {
        var sequence = LongestCommonSequence(source, pattern);

        if (sequence == null)
            return null;

        return string.Concat(sequence);
    }
}
