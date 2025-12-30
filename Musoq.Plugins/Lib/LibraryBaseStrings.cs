using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private readonly Soundex _soundex = new();
    
    private static readonly char[] Separator = ['\n'];
    
    private static readonly ConcurrentDictionary<string, Regex> StringRegexCache = new();

    /// <summary>
    /// Gets the new identifier
    /// </summary>
    /// <returns>New identifier</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    [NonDeterministic]
    public string NewId()
    {
        return Guid.NewGuid().ToString();
    }
        
    /// <summary>
    /// Removes leading and trailing whitespace from a string.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Trim(string? value)
    {
        return value?.Trim();
    }
        
    /// <summary>
    /// Removes leading whitespace from a string.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? TrimStart(string? value)
    {
        return value?.TrimStart();
    }
        
    /// <summary>
    /// Removes trailing whitespace from a string.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? TrimEnd(string? value)
    {
        return value?.TrimEnd();
    }

    /// <summary>
    /// Gets the substring from the string.
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="index">The index</param>
    /// <param name="length">The length</param>
    /// <returns>Substring of a string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Substring(string? value, int? index, int? length)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (length < 1)
            return string.Empty;

        if (index == null || length == null)
            return null;

        var valueLastIndex = value.Length - 1;
        var computedLastIndex = index + (length - 1);

        if (valueLastIndex < computedLastIndex)
            length = ((value.Length - 1) - index) + 1;
            
        return length is null ? null : value.Substring(index.Value, length.Value);
    }

    /// <summary>
    /// Gets the substring from the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="length">The length</param>
    /// <returns>Substring of a string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Substring(string? value, int? length)
    {
        return Substring(value, 0, length);
    }
        
    /// <summary>
    /// Concatenates the specified values
    /// </summary>
    /// <param name="strings">The strings</param>
    /// <returns>Concatenated values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat(params string?[]? strings)
    {
        if (strings == null)
            return null;

        var concatenatedStrings = new StringBuilder();

        foreach (var value in strings)
            concatenatedStrings.Append(value);

        return concatenatedStrings.ToString();
    }

    /// <summary>
    /// Concatenates the specified characters
    /// </summary>
    /// <param name="characters">The characters</param>
    /// <returns>Concatenated characters</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat(params char[]? characters)
    {
        if (characters == null)
            return null;

        var concatenatedChars = new StringBuilder();

        foreach (var value in characters)
            concatenatedChars.Append(value);

        return concatenatedChars.ToString();
    }

    /// <summary>
    /// Concatenates specified string first characters
    /// </summary>
    /// <param name="firstString">The string</param>
    /// <param name="chars">The characters</param>
    /// <returns>Concatenated string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat(string? firstString, params char[]? chars)
    {
        if (firstString == null || chars == null)
            return null;

        var concatenatedStrings = new StringBuilder();

        concatenatedStrings.Append(firstString);

        foreach (var value in chars)
            concatenatedStrings.Append(value);

        return concatenatedStrings.ToString();
    }

    /// <summary>
    /// Concatenate specific character with strings
    /// </summary>
    /// <param name="firstChar">The character</param>
    /// <param name="strings">The strings</param>
    /// <returns>Concatenated string</returns>
    public string? Concat(char? firstChar, params string[]? strings)
    {
        if (firstChar == null || strings == null)
            return null;

        var concatenatedStrings = new StringBuilder();

        concatenatedStrings.Append(firstChar);

        foreach (var value in strings)
            concatenatedStrings.Append(value);

        return concatenatedStrings.ToString();
    }

    /// <summary>
    /// Concatenates the specified strings
    /// </summary>
    /// <param name="objects">The objects</param>
    /// <returns>Concatenated string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat(params object?[]? objects)
    {
        if (objects == null)
            return null;

        var concatenatedStrings = new StringBuilder();

        foreach (var value in objects)
            concatenatedStrings.Append(value);

        return concatenatedStrings.ToString();
    }

    /// <summary>
    /// Concatenates the specified strings
    /// </summary>
    /// <param name="objects">The objects</param>
    /// <returns>Concatenated string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat<T>(params T?[]? objects)
    {
        if (objects == null)
            return null;

        var concatenatedStrings = new StringBuilder();

        foreach (var value in objects)
            concatenatedStrings.Append(value);

        return concatenatedStrings.ToString();
    }

    /// <summary>
    /// Determine whether the string contains the specified value
    /// </summary>
    /// <param name="content">The content</param>
    /// <param name="what">The what</param>
    /// <returns>True if contains; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? Contains(string? content, string? what)
    {
        if (content == null || what == null)
            return null;

        return content.Contains(what, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Position of the first occurrence of the specified value
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="text">The text</param>
    /// <returns>Index of specific text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? IndexOf(string? value, string? text)
    {
        if (value == null || text == null)
            return null;

        return value.IndexOf(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Position of the nth occurrence of the specified value
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="text">The text</param>
    /// <param name="index">The index</param>
    /// <returns>Index of specific text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? NthIndexOf(string? value, string? text, int index)
    {
        if (value == null || text == null || index < 0)
            return null;
        
        var searchText = text;
        if (string.IsNullOrEmpty(searchText))
            return null;
    
        var count = 0;
        var position = -1;

        do
        {
            position = value.IndexOf(searchText, position + 1, StringComparison.Ordinal);
        
            if (position == -1)
                return null;
            
            if (count == index)
                return position;
            
            count++;
        } while (true);
    }
    
    /// <summary>
    /// Position of the last occurrence of the specified pattern
    /// </summary>
    /// <param name="value">The content to search in</param>
    /// <param name="text">The pattern to find</param>
    /// <returns>Index of the last occurrence of the pattern</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? LastIndexOf(string? value, string? text)
    {
        if (value == null || text == null || text.Length == 0)
            return null;
        
        var position = value.LastIndexOf(text, StringComparison.Ordinal);
        return position == -1 ? null : position;
    }

    /// <summary>
    /// Computes soundex for the specified value
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
    /// Matches the specified text by splitting it with separator and applying fuzzy comparison 
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="word">The word</param>
    /// <param name="separator">The separator</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool HasFuzzyMatchedWord(string text, string word, string separator = " ")
    {
        if (string.IsNullOrEmpty(word))
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var soundExWord = _soundex.For(word);
        var square = (int) Math.Ceiling(Math.Sqrt(word.Length));

        foreach (var tokenizedWord in text.Split(separator[0]))
        {
            if (soundExWord == _soundex.For(tokenizedWord) || LevenshteinDistance(word, tokenizedWord) <= square)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Matches the specified text by splitting it with separator and applying fuzzy comparison with a given distance
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="word">The word</param>
    /// <param name="distance">The distance</param>
    /// <param name="separator">The separator</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool HasWordThatHasSmallerLevenshteinDistanceThan(string text, string word, int distance, string separator = " ")
    {
        if (string.IsNullOrEmpty(word))
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var tokenizedWord in text.Split(separator[0]))
        {
            if (tokenizedWord == word || LevenshteinDistance(tokenizedWord, word) <= distance)
                return true;
        }

        return false;
    }
        
    /// <summary>
    /// Matches whether the specified word is present after being fuzzified within the specified text
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="word">The word</param>
    /// <param name="separator">The separator</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool HasWordThatSoundLike(string text, string word, string separator = " ")
    {
        if (string.IsNullOrEmpty(word))
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return false;
            
        var soundExWord = _soundex.For(word);

        foreach (var tokenizedWord in text.Split(separator[0]))
        {
            if (soundExWord == _soundex.For(tokenizedWord))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Matches whether the specified text is present in sentence after being fuzified
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="sentence">The sentence</param>
    /// <param name="separator">The separator</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool HasTextThatSoundLikeSentence(string text, string sentence, string separator = " ")
    {
        if (string.IsNullOrEmpty(sentence))
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var words = sentence.Split(separator[0]);
        var tokens = text.Split(separator[0]);
        var wordsMatchTable = new bool[words.Length];

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            var soundExWord = _soundex.For(word);

            foreach (var token in tokens)
            {
                if (soundExWord == _soundex.For(token))
                {
                    wordsMatchTable[i] = true;
                    break;
                }
            }
        }

        return wordsMatchTable.All(entry => entry);
    }

    /// <summary>
    /// Makes the string uppercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Uppercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToUpper(string value)
        => ToUpper(value, CultureInfo.CurrentCulture);

    /// <summary>
    /// Makes the string uppercase within specified culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Uppercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToUpper(string value, string culture)
    {
        return ToUpper(value, CultureInfo.GetCultureInfo(culture));
    }
        
    /// <summary>
    /// Makes the string uppercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Uppercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToUpperInvariant(string value)
        => ToUpper(value, CultureInfo.InvariantCulture);

    /// <summary>
    /// Makes the string uppercase within specified culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Uppercased string</returns>
    private string? ToUpper(string? value, CultureInfo? culture)
    {
        if (value == null)
            return null;

        if (culture == null)
            return null;

        return value.ToUpper(culture);
    }

    /// <summary>
    /// Makes the string lowercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Lowercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToLower(string value)
        => ToLower(value, CultureInfo.CurrentCulture);

    /// <summary>
    /// Makes the string lowercase within specified culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Lowercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToLower(string value, string culture)
        => ToLower(value, CultureInfo.GetCultureInfo(culture));

    /// <summary>
    /// Makes the string lowercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Lowercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToLowerInvariant(string value)
        => ToLower(value, CultureInfo.InvariantCulture);

    /// <summary>
    /// Makes the string lowercase within specified culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Lowercased string</returns>
    private string? ToLower(string? value, CultureInfo? culture)
    {
        if (value == null)
            return null;

        if (culture == null)
            return null;

        return value.ToLower(culture);
    }

    /// <summary>
    /// Returns a new string that right-aligns the characters in this instance by padding them on the left with a specified Unicode character, for a specified total lengt
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="character">The character</param>
    /// <param name="totalWidth">The total width</param>
    /// <returns>Left aligned value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? PadLeft(string? value, string? character, int? totalWidth)
    {
        if (value == null || character == null)
            return null;

        if (totalWidth == null)
            return null;

        return value.PadLeft(totalWidth.Value, character[0]);
    }

    /// <summary>
    /// Returns a new string that left-aligns the characters in this instance by padding them on the right with a specified Unicode character, for a specified total length
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="character">The character</param>
    /// <param name="totalWidth">The total width</param>
    /// <returns>Right aligned value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? PadRight(string? value, string? character, int? totalWidth)
    {
        if (value == null || character == null)
            return null;

        if (totalWidth == null)
            return null;

        return value.PadRight(totalWidth.Value, character[0]);
    }

    /// <summary>
    /// Gets the first N characters of the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="length">The length</param>
    /// <returns>First characters of string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Head(string? value, int? length = 10)
    {
        if (value == null)
            return null;

        if (length == null)
            return null;

        return value.Substring(0, length.Value);
    }
        
    /// <summary>
    /// Gets the last N characters of the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="length">The length</param>
    /// <returns>Last characters of string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Tail(string? value, int? length = 10)
    {
        if (value == null)
            return null;

        if (length == null)
            return null;

        return value.Substring(value.Length - length.Value, length.Value);
    }
        
    /// <summary>
    /// Computes the Levenshtein distance between two strings
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

        return Fastenshtein.Levenshtein.Distance(firstValue, secondValue);
    }

    /// <summary>
    /// Gets the character at specified index
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="index">the index</param>
    /// <returns>Character based on index</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public char? GetCharacterOf(string value, int index)
    {
        if (value.Length <= index || index < 0)
            return null;

        return value[index];
    }

    /// <summary>
    /// Reverses the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Reversed string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Reverse(string? value)
    {
        if (value == null)
            return null;

        if (value == string.Empty)
            return value;

        if (value.Length == 1)
            return value;

        return string.Concat(value.Reverse());
    }

    /// <summary>
    /// Splits the string into an array of substrings based on the specified separators
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="separators">The separators</param>
    /// <returns>Separated values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[] Split(string? value, params string[] separators)
    {
        if (value == null)
            return [];
            
        return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    }
    
    /// <summary>
    /// Splits the string into an array of characters
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Array of characters</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public char[] ToCharArray(string? value)
    {
        if (value == null)
            return [];
            
        return value.ToCharArray();
    }

    /// <summary>
    /// Computes the longest common subsequence between two source and pattern
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

    /// <summary>
    /// Clones the value n times
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="integer">The integer</param>
    /// <returns>Cloned value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string Replicate(string value, int integer)
    {
        var builder = new StringBuilder();

        for (int i = 0; i < integer; ++i)
            builder.Append(value);

        return builder.ToString();
    }

    /// <summary>
    /// Returns the string from the first argument after the characters specified in the second argument are translated into the characters specified in the third argument.
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="characters">The characters</param>
    /// <param name="translations">The translations</param>
    /// <returns>Translated value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Translate(string? value, string? characters, string? translations)
    {
        if (value == null)
            return null;

        if (characters == null || translations == null)
            return null;

        if (characters.Length != translations.Length)
            return null;

        var builder = new StringBuilder();

        foreach (var character in value)
        {
            var index = characters.IndexOf(character);

            builder.Append(index == -1 ? character : translations[index]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Replaces the first occurrence of a specified string in this instance with another specified string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="lookFor">The lookFor</param>
    /// <param name="changeTo">The changeTo</param>
    /// <returns>Changed value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Replace(string? text, string lookFor, string? changeTo)
    {
        if (text == null)
            return null;

        if (string.IsNullOrEmpty(lookFor))
            return text;

        if (changeTo == null)
            return text;

        return text.Replace(lookFor, changeTo);
    }

    /// <summary>
    /// Capitalizes the first letter of the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Capitalized text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToTitleCase(string? value)
    {
        if (value == null)
            return null;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
    }

    /// <summary>
    /// Gets the nth word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="wordIndex">The wordIndex</param>
    /// <param name="separator">The separator</param>
    /// <returns>Nth word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetNthWord(string? text, int wordIndex, string? separator)
    {
        if (text == null || separator == null)
            return null;

        var split = text.Split(separator[0]);

        if (wordIndex >= split.Length)
            return null;

        return split[wordIndex];
    }

    /// <summary>
    /// Gets the first word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="separator">The separator</param>
    /// <returns>First word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetFirstWord(string text, string separator)
    {
        return GetNthWord(text, 1, separator);
    }

    /// <summary>
    /// Gets the second word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="separator">The separator</param>
    /// <returns>Second word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetSecondWord(string text, string separator)
    {
        return GetNthWord(text, 2, separator);
    }

    /// <summary>
    /// Gets the third word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="separator">The separator</param>
    /// <returns>Third word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetThirdWord(string text, string separator)
    {
        return GetNthWord(text, 3, separator);
    }

    /// <summary>
    /// Gets last word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="separator">The separator</param>
    /// <returns>Last word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetLastWord(string? text, string? separator)
    {
        if (text == null || separator == null)
            return null;

        var split = text.Split(separator[0]);

        return split[^1];
    }
        
    /// <summary>
    /// Determines whether the string is null or empty
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>True if null or empty; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool IsNullOrEmpty(string? value)
    {
        return string.IsNullOrEmpty(value);
    }
        
    /// <summary>
    /// Determines whether the string is null or whitespace
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>True if null or whitespace; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool IsNullOrWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
        
    /// <summary>
    /// Encodes the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Url encoded value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? UrlEncode(string? value)
    {
        if (value == null)
            return null;
            
        return HttpUtility.UrlEncode(value);
    }
        
    /// <summary>
    /// Decodes the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Url decoded value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? UrlDecode(string? value)
    {
        if (value == null)
            return null;

        return HttpUtility.UrlDecode(value);
    }
        
    /// <summary>
    /// Encodes the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Uri encoded value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? UriEncode(string? value)
    {
        if (value == null)
            return null;

        return Uri.EscapeDataString(value);
    }
        
    /// <summary>
    /// Decodes the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Uri decoded value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? UriDecode(string? value)
    {
        if (value == null)
            return null;

        return Uri.UnescapeDataString(value);
    }

    /// <summary>
    /// Encodes the value for safe use in HTML content.
    /// Converts special characters like &lt;, &gt;, &amp;, ", ' to their HTML entity equivalents.
    /// </summary>
    /// <param name="value">The value to encode</param>
    /// <returns>HTML encoded value, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? HtmlEncode(string? value)
    {
        if (value == null)
            return null;

        return HttpUtility.HtmlEncode(value);
    }

    /// <summary>
    /// Decodes HTML entities in the value back to their original characters.
    /// Converts entities like &amp;lt;, &amp;gt;, &amp;amp;, &amp;quot; back to &lt;, &gt;, &amp;, ".
    /// </summary>
    /// <param name="value">The HTML encoded value to decode</param>
    /// <returns>Decoded value, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? HtmlDecode(string? value)
    {
        if (value == null)
            return null;

        return HttpUtility.HtmlDecode(value);
    }
        
    /// <summary>
    /// Determines whether the string starts with the specified prefix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="prefix">The prefix</param>
    /// <returns>True if starts with; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? StartsWith(string? value, string? prefix)
    {
        if (value == null || prefix == null)
            return null;

        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the string starts with the specified prefix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="prefix">The prefix</param>
    /// <param name="comparison">The comparison</param>
    /// <returns>True if starts with; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? StartsWith(string? value, string? prefix, string comparison)
    {
        if (value == null || prefix == null)
            return null;

        return value.StartsWith(prefix, Enum.Parse<StringComparison>(comparison));
    }
        
    /// <summary>
    /// Determines whether the string ends with the specified suffix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="suffix">The suffix</param>
    /// <returns>True if ends with; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? EndsWith(string? value, string? suffix)
    {
        if (value == null || suffix == null)
            return null;

        return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }
        
    /// <summary>
    /// Determines whether the string ends with the specified suffix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="suffix">The suffix</param>
    /// <param name="comparison">The comparison</param>
    /// <returns>True if ends with; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? EndsWith(string? value, string? suffix, string comparison)
    {
        if (value == null || suffix == null)
            return null;

        return value.EndsWith(suffix, Enum.Parse<StringComparison>(comparison));
    }
        
    /// <summary>
    /// Replace the specified value part that matches the pattern with the replacement
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="pattern">The pattern</param>
    /// <param name="replacement">The replacement</param>
    /// <returns>Replaced value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RegexReplace(string? value, string? pattern, string? replacement)
    {
        if (value == null || pattern == null || replacement == null)
            return null;
        
        var compiledRegex = StringRegexCache.GetOrAdd(pattern, p => 
            new Regex(p, RegexOptions.Compiled));
            
        return compiledRegex.Replace(value, replacement);
    }    
    
    /// <summary>
    /// Returns all matching strings based on regular expression pattern
    /// </summary>
    /// <param name="regex">The regular expression pattern</param>
    /// <param name="content">The content to search in</param>
    /// <returns>Array of matching strings, or null if either parameter is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? RegexMatches(string? regex, string? content)
    {
        if (regex == null || content == null)
            return null;
        
        var compiledRegex = StringRegexCache.GetOrAdd(regex, p => 
            new Regex(p, RegexOptions.Compiled));
            
        var matches = compiledRegex.Matches(content);
        return matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToArray();
    }
    
    /// <summary>
    /// Split string by Linux-style newlines (\n)
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of strings</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? SplitByLinuxNewLines(string? input)
    {
        if (input is null)
            return null;
        
        if (string.IsNullOrEmpty(input))
            return [];
            
        // ReSharper disable once UseCollectionExpression
        // ReSharper disable once RedundantExplicitArrayCreation
        return input.Split(Separator, StringSplitOptions.None);
    }

    /// <summary>
    /// Split string by Windows-style newlines (\r\n)
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of strings</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? SplitByWindowsNewLines(string? input)
    {
        if (input is null)
            return null;
        
        if (string.IsNullOrEmpty(input))
            return [];
            
        return input.Split(["\r\n"], StringSplitOptions.None);
    }

    /// <summary>
    /// Smart split that handles both Windows (\r\n) and Linux (\n) newlines
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of strings</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? SplitByNewLines(string? input)
    {
        if (input is null)
            return null;
        
        if (string.IsNullOrEmpty(input))
            return [];
        
        var normalizedInput = input.Replace("\r\n", "\n");
        
        return normalizedInput.Split(Separator, StringSplitOptions.None);
    }
    
    /// <summary>
    /// Joins the specified values with the separator
    /// </summary>
    /// <param name="separator">The separator</param>
    /// <param name="values">The values</param>
    /// <returns>Joined values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? StringsJoin(string? separator, params string?[]? values)
    {
        if (separator is null)
            return null;
        
        if (values is null)
            return null;
        
        return string.Join(separator, values.Where(str => str != null));
    }
    
    /// <summary>
    /// Joins the specified values with the separator
    /// </summary>
    /// <param name="separator">The separator</param>
    /// <param name="values">The values</param>
    /// <returns>Joined values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? StringsJoin(string? separator, IEnumerable<string?>? values)
    {
        if (separator is null)
            return null;
        
        if (values is null)
            return null;
        
        return string.Join(separator, values.Where(str => str != null));
    }

    /// <summary>
    /// Extracts text between the first occurrence of the start delimiter and the first occurrence of the end delimiter after it.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="startDelimiter">The starting delimiter (character or substring)</param>
    /// <param name="endDelimiter">The ending delimiter (character or substring)</param>
    /// <returns>The extracted text between delimiters, or null if delimiters are not found</returns>
    /// <example>
    /// ExtractBetween("Hello [World] Test", "[", "]") returns "World"
    /// ExtractBetween("&lt;tag&gt;content&lt;/tag&gt;", "&lt;tag&gt;", "&lt;/tag&gt;") returns "content"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ExtractBetween(string? value, string? startDelimiter, string? endDelimiter)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(startDelimiter) || string.IsNullOrEmpty(endDelimiter))
            return null;

        var startIndex = value.IndexOf(startDelimiter, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        var contentStart = startIndex + startDelimiter.Length;
        var endIndex = value.IndexOf(endDelimiter, contentStart, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return value.Substring(contentStart, endIndex - contentStart);
    }

    /// <summary>
    /// Extracts all occurrences of text between the start and end delimiters.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="startDelimiter">The starting delimiter (character or substring)</param>
    /// <param name="endDelimiter">The ending delimiter (character or substring)</param>
    /// <returns>An array of all extracted texts between delimiters</returns>
    /// <example>
    /// ExtractBetweenAll("a]b] test", "[", "]") returns ["a", "b"]
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[] ExtractBetweenAll(string? value, string? startDelimiter, string? endDelimiter)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(startDelimiter) || string.IsNullOrEmpty(endDelimiter))
            return [];

        var results = new List<string>();
        var currentIndex = 0;

        while (currentIndex < value.Length)
        {
            var startIndex = value.IndexOf(startDelimiter, currentIndex, StringComparison.Ordinal);
            if (startIndex == -1)
                break;

            var contentStart = startIndex + startDelimiter.Length;
            var endIndex = value.IndexOf(endDelimiter, contentStart, StringComparison.Ordinal);
            if (endIndex == -1)
                break;

            results.Add(value.Substring(contentStart, endIndex - contentStart));
            currentIndex = endIndex + endDelimiter.Length;
        }

        return results.ToArray();
    }

    /// <summary>
    /// Extracts text between the first occurrence of the start delimiter and the first occurrence of the end delimiter,
    /// including the delimiters themselves in the result.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="startDelimiter">The starting delimiter (character or substring)</param>
    /// <param name="endDelimiter">The ending delimiter (character or substring)</param>
    /// <returns>The extracted text including delimiters, or null if delimiters are not found</returns>
    /// <example>
    /// ExtractBetweenIncluding("Hello [World] Test", "[", "]") returns "[World]"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ExtractBetweenIncluding(string? value, string? startDelimiter, string? endDelimiter)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(startDelimiter) || string.IsNullOrEmpty(endDelimiter))
            return null;

        var startIndex = value.IndexOf(startDelimiter, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        var endIndex = value.IndexOf(endDelimiter, startIndex + startDelimiter.Length, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return value.Substring(startIndex, endIndex - startIndex + endDelimiter.Length);
    }

    /// <summary>
    /// Extracts text from the first occurrence of the start delimiter to the end of the string.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="startDelimiter">The starting delimiter (character or substring)</param>
    /// <param name="includeDelimiter">Whether to include the delimiter in the result</param>
    /// <returns>The extracted text from the delimiter to the end, or null if delimiter is not found</returns>
    /// <example>
    /// ExtractAfter("Hello World Test", "World", false) returns " Test"
    /// ExtractAfter("Hello World Test", "World", true) returns "World Test"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ExtractAfter(string? value, string? startDelimiter, bool includeDelimiter = false)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(startDelimiter))
            return null;

        var startIndex = value.IndexOf(startDelimiter, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        return includeDelimiter 
            ? value.Substring(startIndex) 
            : value.Substring(startIndex + startDelimiter.Length);
    }

    /// <summary>
    /// Extracts text from the beginning of the string up to the first occurrence of the end delimiter.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="endDelimiter">The ending delimiter (character or substring)</param>
    /// <param name="includeDelimiter">Whether to include the delimiter in the result</param>
    /// <returns>The extracted text from the beginning to the delimiter, or null if delimiter is not found</returns>
    /// <example>
    /// ExtractBefore("Hello World Test", "World", false) returns "Hello "
    /// ExtractBefore("Hello World Test", "World", true) returns "Hello World"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ExtractBefore(string? value, string? endDelimiter, bool includeDelimiter = false)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(endDelimiter))
            return null;

        var endIndex = value.IndexOf(endDelimiter, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return includeDelimiter 
            ? value.Substring(0, endIndex + endDelimiter.Length) 
            : value.Substring(0, endIndex);
    }

    /// <summary>
    /// Determines whether the string contains only numeric characters (0-9).
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string contains only digits; otherwise false. Returns null if input is null, false if empty.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? IsNumeric(string? value)
    {
        if (value == null)
            return null;

        if (value.Length == 0)
            return false;

        foreach (var c in value)
        {
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the string contains only alphabetic characters (a-z, A-Z).
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string contains only letters; otherwise false. Returns null if input is null, false if empty.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? IsAlpha(string? value)
    {
        if (value == null)
            return null;

        if (value.Length == 0)
            return false;

        foreach (var c in value)
        {
            if (!char.IsLetter(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the string contains only alphanumeric characters (a-z, A-Z, 0-9).
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string contains only letters and digits; otherwise false. Returns null if input is null, false if empty.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? IsAlphaNumeric(string? value)
    {
        if (value == null)
            return null;

        if (value.Length == 0)
            return false;

        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Counts the number of occurrences of a substring within a string.
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
    /// Removes all whitespace characters from the string.
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <returns>The string without any whitespace, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RemoveWhitespace(string? value)
    {
        if (value == null)
            return null;

        var result = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            if (!char.IsWhiteSpace(c))
                result.Append(c);
        }

        return result.ToString();
    }

    /// <summary>
    /// Truncates the string to the specified maximum length, optionally adding an ellipsis.
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">The maximum length of the result</param>
    /// <param name="ellipsis">The ellipsis to append when truncated (default is "...")</param>
    /// <returns>The truncated string, or the original string if shorter than maxLength</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Truncate(string? value, int maxLength, string ellipsis = "...")
    {
        if (value == null)
            return null;

        if (maxLength < 0)
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        var ellipsisLength = ellipsis?.Length ?? 0;
        
        if (maxLength <= ellipsisLength)
            return value.Substring(0, maxLength);

        return value.Substring(0, maxLength - ellipsisLength) + ellipsis;
    }

    /// <summary>
    /// Capitalizes the first character of the string.
    /// </summary>
    /// <param name="value">The string to capitalize</param>
    /// <returns>The string with the first character in uppercase</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Capitalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length == 1)
            return value.ToUpperInvariant();

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// Returns the string repeated the specified number of times with a separator.
    /// </summary>
    /// <param name="value">The string to repeat</param>
    /// <param name="count">The number of times to repeat</param>
    /// <param name="separator">The separator between repetitions</param>
    /// <returns>The repeated string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Repeat(string? value, int count, string separator = "")
    {
        if (value == null)
            return null;

        if (count <= 0)
            return string.Empty;

        if (count == 1)
            return value;

        var result = new StringBuilder();
        
        for (var i = 0; i < count; i++)
        {
            if (i > 0 && separator != null)
                result.Append(separator);
            result.Append(value);
        }

        return result.ToString();
    }

    /// <summary>
    /// Wraps the string with the specified prefix and suffix.
    /// </summary>
    /// <param name="value">The string to wrap</param>
    /// <param name="prefix">The prefix to add</param>
    /// <param name="suffix">The suffix to add</param>
    /// <returns>The wrapped string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Wrap(string? value, string? prefix, string? suffix)
    {
        if (value == null)
            return null;

        return (prefix ?? string.Empty) + value + (suffix ?? string.Empty);
    }

    /// <summary>
    /// Removes the specified prefix from the string if it starts with it.
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <param name="prefix">The prefix to remove</param>
    /// <returns>The string without the prefix, or the original string if it doesn't start with the prefix</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RemovePrefix(string? value, string? prefix)
    {
        if (value == null)
            return null;

        if (string.IsNullOrEmpty(prefix))
            return value;

        return value.StartsWith(prefix, StringComparison.Ordinal) 
            ? value.Substring(prefix.Length) 
            : value;
    }

    /// <summary>
    /// Removes the specified suffix from the string if it ends with it.
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <param name="suffix">The suffix to remove</param>
    /// <returns>The string without the suffix, or the original string if it doesn't end with the suffix</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RemoveSuffix(string? value, string? suffix)
    {
        if (value == null)
            return null;

        if (string.IsNullOrEmpty(suffix))
            return value;

        return value.EndsWith(suffix, StringComparison.Ordinal) 
            ? value.Substring(0, value.Length - suffix.Length) 
            : value;
    }

    /// <summary>
    /// Converts a string to snake_case format.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in snake_case format</returns>
    /// <example>
    /// ToSnakeCase("HelloWorld") returns "hello_world"
    /// ToSnakeCase("XMLParser") returns "xml_parser"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToSnakeCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();
        
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            
            if (char.IsUpper(c))
            {
                if (i > 0 && !char.IsUpper(value[i - 1]))
                    result.Append('_');
                else if (i > 0 && i < value.Length - 1 && char.IsUpper(value[i - 1]) && !char.IsUpper(value[i + 1]))
                    result.Append('_');
                    
                result.Append(char.ToLowerInvariant(c));
            }
            else if (c is ' ' or '-')
            {
                result.Append('_');
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a string to kebab-case format.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in kebab-case format</returns>
    /// <example>
    /// ToKebabCase("HelloWorld") returns "hello-world"
    /// ToKebabCase("XMLParser") returns "xml-parser"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToKebabCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();
        
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            
            if (char.IsUpper(c))
            {
                if (i > 0 && !char.IsUpper(value[i - 1]))
                    result.Append('-');
                else if (i > 0 && i < value.Length - 1 && char.IsUpper(value[i - 1]) && !char.IsUpper(value[i + 1]))
                    result.Append('-');
                    
                result.Append(char.ToLowerInvariant(c));
            }
            else if (c is ' ' or '_')
            {
                result.Append('-');
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a string to camelCase format.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in camelCase format</returns>
    /// <example>
    /// ToCamelCase("hello_world") returns "helloWorld"
    /// ToCamelCase("Hello World") returns "helloWorld"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToCamelCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();
        var capitalizeNext = false;
        var isFirst = true;
        
        foreach (var c in value)
        {
            if (c is '_' or '-' or ' ')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                result.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else if (isFirst)
            {
                result.Append(char.ToLowerInvariant(c));
                isFirst = false;
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a string to PascalCase format.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in PascalCase format</returns>
    /// <example>
    /// ToPascalCase("hello_world") returns "HelloWorld"
    /// ToPascalCase("hello world") returns "HelloWorld"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToPascalCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();
        var capitalizeNext = true;
        
        foreach (var c in value)
        {
            if (c is '_' or '-' or ' ')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                result.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Counts the number of words in the string.
    /// Words are separated by whitespace characters.
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
        {
            if (char.IsWhiteSpace(c))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                inWord = true;
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Counts the number of lines in the string.
    /// Lines are separated by newline characters.
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
        {
            if (value[i] == '\n')
                count++;
            else if (value[i] == '\r' && (i + 1 >= value.Length || value[i + 1] != '\n'))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Counts the number of sentences in the string.
    /// Sentences are delimited by period, exclamation mark, or question mark.
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
        {
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
        }

        // Count incomplete sentence at end (no terminator)
        if (inSentence)
            count++;

        return count;
    }

    /// <summary>
    /// Extracts the first match of a regex capture group from the string.
    /// </summary>
    /// <param name="value">The string to search in</param>
    /// <param name="pattern">The regex pattern with capture groups</param>
    /// <param name="groupIndex">The capture group index (0 = whole match, 1+ = capture groups)</param>
    /// <returns>The matched group text, or null if no match</returns>
    /// <example>
    /// RegexExtract("Hello 123 World", @"(\d+)", 1) returns "123"
    /// RegexExtract("test@example.com", @"(\w+)@(\w+)\.(\w+)", 2) returns "example"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RegexExtract(string? value, string? pattern, int groupIndex = 0)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
            return null;

        try
        {
            var regex = StringRegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
            var match = regex.Match(value);
            
            if (!match.Success)
                return null;

            if (groupIndex < 0 || groupIndex >= match.Groups.Count)
                return null;

            return match.Groups[groupIndex].Value;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts all matches of a regex capture group from the string.
    /// </summary>
    /// <param name="value">The string to search in</param>
    /// <param name="pattern">The regex pattern with capture groups</param>
    /// <param name="groupIndex">The capture group index (0 = whole match, 1+ = capture groups)</param>
    /// <returns>Array of matched group texts</returns>
    /// <example>
    /// RegexExtractAll("a1b2c3", @"(\d)", 1) returns ["1", "2", "3"]
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[] RegexExtractAll(string? value, string? pattern, int groupIndex = 0)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
            return [];

        try
        {
            var regex = StringRegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
            var matches = regex.Matches(value);
            var results = new List<string>();

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (groupIndex >= 0 && groupIndex < match.Groups.Count)
                    results.Add(match.Groups[groupIndex].Value);
            }

            return results.ToArray();
        }
        catch (ArgumentException)
        {
            return [];
        }
    }

    /// <summary>
    /// Checks if the string matches the specified regex pattern.
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <param name="pattern">The regex pattern</param>
    /// <returns>True if the string matches the pattern; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? IsMatch(string? value, string? pattern)
    {
        if (value == null || pattern == null)
            return null;

        try
        {
            var regex = StringRegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
            return regex.IsMatch(value);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a string to Unicode escape sequences (e.g., "Hi" -> "\u0048\u0069").
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The Unicode-escaped string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToUnicodeEscape(string? value)
    {
        if (value == null)
            return null;

        var sb = new StringBuilder();
        foreach (var c in value)
        {
            sb.Append($"\\u{(int)c:X4}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts Unicode escape sequences back to regular text (e.g., "\u0048\u0069" -> "Hi").
    /// </summary>
    /// <param name="value">The Unicode-escaped string</param>
    /// <returns>The decoded string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? FromUnicodeEscape(string? value)
    {
        if (value == null)
            return null;

        try
        {
            return UnicodeEscapeRegex().Replace(value, m =>
                ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        }
        catch
        {
            return value;
        }
    }

    /// <summary>
    /// Applies ROT13 cipher to a string (rotates letters by 13 positions).
    /// </summary>
    /// <param name="value">The string to transform</param>
    /// <returns>The ROT13-transformed string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Rot13(string? value)
    {
        if (value == null)
            return null;

        var result = new char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c is >= 'a' and <= 'z')
                result[i] = (char)('a' + (c - 'a' + 13) % 26);
            else if (c is >= 'A' and <= 'Z')
                result[i] = (char)('A' + (c - 'A' + 13) % 26);
            else
                result[i] = c;
        }
        return new string(result);
    }

    /// <summary>
    /// Applies ROT47 cipher to a string (rotates printable ASCII by 47 positions).
    /// </summary>
    /// <param name="value">The string to transform</param>
    /// <returns>The ROT47-transformed string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Rot47(string? value)
    {
        if (value == null)
            return null;

        var result = new char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c is >= '!' and <= '~')
                result[i] = (char)('!' + (c - '!' + 47) % 94);
            else
                result[i] = c;
        }
        return new string(result);
    }

    private static readonly Dictionary<char, string> MorseCodeMap = new()
    {
        {'A', ".-"}, {'B', "-..."}, {'C', "-.-."}, {'D', "-.."}, {'E', "."},
        {'F', "..-."}, {'G', "--."}, {'H', "...."}, {'I', ".."}, {'J', ".---"},
        {'K', "-.-"}, {'L', ".-.."}, {'M', "--"}, {'N', "-."}, {'O', "---"},
        {'P', ".--."}, {'Q', "--.-"}, {'R', ".-."}, {'S', "..."}, {'T', "-"},
        {'U', "..-"}, {'V', "...-"}, {'W', ".--"}, {'X', "-..-"}, {'Y', "-.--"},
        {'Z', "--.."}, {'0', "-----"}, {'1', ".----"}, {'2', "..---"}, {'3', "...--"},
        {'4', "....-"}, {'5', "....."}, {'6', "-...."}, {'7', "--..."}, {'8', "---.."},
        {'9', "----."}, {' ', "/"}
    };

    private static readonly Dictionary<string, char> ReverseMorseCodeMap = 
        MorseCodeMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    /// <summary>
    /// Converts text to Morse code.
    /// </summary>
    /// <param name="value">The text to convert</param>
    /// <returns>The Morse code representation</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToMorse(string? value)
    {
        if (value == null)
            return null;

        var result = new List<string>();
        foreach (var c in value.ToUpperInvariant())
        {
            if (MorseCodeMap.TryGetValue(c, out var morse))
                result.Add(morse);
        }
        return string.Join(" ", result);
    }

    /// <summary>
    /// Converts Morse code to text.
    /// </summary>
    /// <param name="value">The Morse code to convert (space-separated, / for word breaks)</param>
    /// <returns>The decoded text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? FromMorse(string? value)
    {
        if (value == null)
            return null;

        var sb = new StringBuilder();
        var codes = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var code in codes)
        {
            if (ReverseMorseCodeMap.TryGetValue(code, out var c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts a string to its binary representation (space-separated bytes).
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The binary string representation</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToBinaryString(string? value)
    {
        if (value == null)
            return null;

        var bytes = Encoding.UTF8.GetBytes(value);
        return string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
    }

    /// <summary>
    /// Converts a binary string (space-separated bytes) back to text.
    /// </summary>
    /// <param name="value">The binary string to convert</param>
    /// <returns>The decoded text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? FromBinaryString(string? value)
    {
        if (value == null)
            return null;

        try
        {
            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var bytes = parts.Select(p => Convert.ToByte(p, 2)).ToArray();
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reverses a string.
    /// </summary>
    /// <param name="value">The string to reverse</param>
    /// <returns>The reversed string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ReverseString(string? value)
    {
        if (value == null)
            return null;

        var charArray = value.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    /// <summary>
    /// Splits a string and returns the element at the specified index.
    /// </summary>
    /// <param name="value">The string to split</param>
    /// <param name="delimiter">The delimiter</param>
    /// <param name="index">The zero-based index of the element to return</param>
    /// <returns>The element at the specified index, or null if index is out of range</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? SplitAndTake(string? value, string? delimiter, int index)
    {
        if (value == null || delimiter == null)
            return null;

        var parts = value.Split(delimiter);
        if (index >= 0 && index < parts.Length)
            return parts[index];

        return null;
    }

    /// <summary>
    /// Pads a string on the left to the specified total length.
    /// </summary>
    /// <param name="value">The string to pad</param>
    /// <param name="totalWidth">The total desired width</param>
    /// <param name="paddingChar">The character to pad with (default: space)</param>
    /// <returns>The padded string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? PadLeft(string? value, int totalWidth, char paddingChar = ' ')
    {
        if (value == null)
            return null;

        return value.PadLeft(totalWidth, paddingChar);
    }

    /// <summary>
    /// Pads a string on the right to the specified total length.
    /// </summary>
    /// <param name="value">The string to pad</param>
    /// <param name="totalWidth">The total desired width</param>
    /// <param name="paddingChar">The character to pad with (default: space)</param>
    /// <returns>The padded string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? PadRight(string? value, int totalWidth, char paddingChar = ' ')
    {
        if (value == null)
            return null;

        return value.PadRight(totalWidth, paddingChar);
    }

    /// <summary>
    /// Removes diacritical marks (accents) from a string.
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <returns>The string with diacritics removed</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RemoveDiacritics(string? value)
    {
        if (value == null)
            return null;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
    
    [GeneratedRegex(@"\\u([0-9A-Fa-f]{4})")]
    private static partial Regex UnicodeEscapeRegex();
}