using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public abstract partial class LibraryBase
{
    private readonly Soundex _soundex = new();

    /// <summary>
    /// Gets the new identifier
    /// </summary>
    /// <returns>New identifier</returns>
    [BindableMethod]
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
    public static string? Trim(string? value)
    {
        return value?.Trim();
    }
        
    /// <summary>
    /// Removes leading whitespace from a string.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    [BindableMethod]
    public static string? TrimStart(string? value)
    {
        return value?.TrimStart();
    }
        
    /// <summary>
    /// Removes trailing whitespace from a string.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    [BindableMethod]
    public static string? TrimEnd(string? value)
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
    public string? Substring(string value, int? length)
    {
        return Substring(value, 0, length);
    }
        
    /// <summary>
    /// Concatenates the specified values
    /// </summary>
    /// <param name="strings">The strings</param>
    /// <returns>Concatenated values</returns>
    [BindableMethod]
    public string? Concat(params string[]? strings)
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
    /// Concatenates specified string fir characters
    /// </summary>
    /// <param name="firstString">The string</param>
    /// <param name="chars">The characters</param>
    /// <returns>Concatenated string</returns>
    [BindableMethod]
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
    public string? Concat(params object[]? objects)
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
    public string? Concat<T>(params T[]? objects)
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
    public bool? Contains(string? content, string? what)
    {
        if (content == null || what == null)
            return null;

        return CultureInfo.CurrentCulture.CompareInfo.IndexOf(content, what, CompareOptions.IgnoreCase) >= 0;
    }

    /// <summary>
    /// Position of the first occurrence of the specified value
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="text">The text</param>
    /// <returns>Index of specific text</returns>
    [BindableMethod]
    public int? IndexOf(string? value, string? text)
    {
        if (value == null || text == null)
            return null;

        return value.IndexOf(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Computes soundex for the specified value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Soundex code</returns>
    [BindableMethod]
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
    public string? ToUpper(string value)
        => ToUpper(value, CultureInfo.CurrentCulture);

    /// <summary>
    /// Makes the string uppercase within specified culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Uppercased string</returns>
    [BindableMethod]
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
    public string? ToLower(string value)
        => ToLower(value, CultureInfo.CurrentCulture);

    /// <summary>
    /// Makes the string lowercase within specified culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Lowercased string</returns>
    [BindableMethod]
    public string? ToLower(string value, string culture)
        => ToLower(value, CultureInfo.GetCultureInfo(culture));

    /// <summary>
    /// Makes the string lowercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Lowercased string</returns>
    [BindableMethod]
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
    /// <returns></returns>
    [BindableMethod]
    public string[] Split(string value, params string[] separators) => value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    
    /// <summary>
    /// Splits the string into an array of characters
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Array of characters</returns>
    [BindableMethod]
    public char[] ToCharArray(string value) => value.ToCharArray();

    /// <summary>
    /// Computes the longest common subsequence between two source and pattern
    /// </summary>
    /// <param name="source">The source</param>
    /// <param name="pattern">The pattern</param>
    /// <returns>Longest common subsequence</returns>
    [BindableMethod]
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
    public string? UriDecode(string? value)
    {
        if (value == null)
            return null;

        return Uri.UnescapeDataString(value);
    }
        
    /// <summary>
    /// Determines whether the string starts with the specified prefix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="prefix">The prefix</param>
    /// <returns>True if starts with; otherwise false</returns>
    [BindableMethod]
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
    public string? RegexReplace(string? value, string? pattern, string? replacement)
    {
        if (value == null || pattern == null || replacement == null)
            return null;
            
        return System.Text.RegularExpressions.Regex.Replace(value, pattern, replacement);
    }    
    
    /// <summary>
    /// Split string by Linux-style newlines (\n)
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of strings</returns>
    [BindableMethod]
    public string[]? SplitByLinuxNewLines(string? input)
    {
        if (input is null)
            return null;
        
        if (string.IsNullOrEmpty(input))
            return [];
            
        return input.Split(['\n'], StringSplitOptions.None);
    }

    /// <summary>
    /// Split string by Windows-style newlines (\r\n)
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of strings</returns>
    [BindableMethod]
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
    public string[]? SplitByNewLines(string? input)
    {
        if (input is null)
            return null;
        
        if (string.IsNullOrEmpty(input))
            return [];
        
        var normalizedInput = input.Replace("\r\n", "\n");
        
        return normalizedInput.Split(['\n'], StringSplitOptions.None);
    }
}