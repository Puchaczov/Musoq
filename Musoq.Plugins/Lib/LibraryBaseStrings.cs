using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public abstract partial class LibraryBase
    {
        private readonly Soundex _soundex = new Soundex();

        [BindableMethod]
        public string NewId()
        {
            return Guid.NewGuid().ToString();
        }

        [BindableMethod]
        public string Substring(string value, int? index, int? length)
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

            return value.Substring(index.Value, length.Value);
        }

        [BindableMethod]
        public string Substring(string value, int? length)
        {
            return Substring(value, 0, length);
        }

        [BindableMethod]
        public string Concat(params string[] strings)
        {
            if (strings == null)
                return null;

            var concatedStrings = new StringBuilder();

            foreach (var value in strings)
                concatedStrings.Append(value);

            return concatedStrings.ToString();
        }

        [BindableMethod]
        public string Concat(params char[] characters)
        {
            if (characters == null)
                return null;

            var concatedChars = new StringBuilder();

            foreach (var value in characters)
                concatedChars.Append(value);

            return concatedChars.ToString();
        }

        [BindableMethod]
        public string Concat(string firstString, params char[] chars)
        {
            if (firstString == null || chars == null)
                return null;

            var concatedStrings = new StringBuilder();

            concatedStrings.Append(firstString);

            foreach (var value in chars)
                concatedStrings.Append(value);

            return concatedStrings.ToString();
        }

        public string Concat(char? firstChar, params string[] strings)
        {
            if (firstChar == null || strings == null)
                return null;

            var concatedStrings = new StringBuilder();

            concatedStrings.Append(firstChar);

            foreach (var value in strings)
                concatedStrings.Append(value);

            return concatedStrings.ToString();
        }

        [BindableMethod]
        public string Concat(params object[] objects)
        {
            if (objects == null)
                return null;

            var concatedStrings = new StringBuilder();

            foreach (var value in objects)
                concatedStrings.Append(value);

            return concatedStrings.ToString();
        }

        [BindableMethod]
        public string Concat<T>(params T[] objects)
        {
            if (objects == null)
                return null;

            var concatedStrings = new StringBuilder();

            foreach (var value in objects)
                concatedStrings.Append(value.ToString());

            return concatedStrings.ToString();
        }

        [BindableMethod]
        public bool? Contains(string content, string what)
        {
            if (content == null || what == null)
                return null;

            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(content, what, CompareOptions.IgnoreCase) >= 0;
        }

        [BindableMethod]
        public int? IndexOf(string value, string text)
        {
            if (value == null || text == null)
                return null;

            return value.IndexOf(text, StringComparison.OrdinalIgnoreCase);
        }

        [BindableMethod]
        public string Soundex(string value)
        {
            if (value == null)
                return null;

            return _soundex.For(value);
        }

        [BindableMethod]
        public bool HasFuzzyMatchedWord(string text, string word, string separator = " ")
        {
            if (word == null || word == string.Empty)
                return false;

            if (text == null || text == string.Empty)
                return false;

            var soundexedWord = _soundex.For(word);
            var square = (int) Math.Ceiling(Math.Sqrt(word.Length));

            foreach (var tokenizedWord in text.Split(separator[0]))
            {
                if (soundexedWord == _soundex.For(tokenizedWord) || LevenshteinDistance(word, tokenizedWord) <= square)
                    return true;
            }

            return false;
        }

        [BindableMethod]
        public bool HasWordThatHasSmallerLevenshteinDistanceThan(string text, string word, int distance, string separator = " ")
        {
            if (word == null || word == string.Empty)
                return false;

            if (text == null || text == string.Empty)
                return false;

            foreach (var tokenizedWord in text.Split(separator[0]))
            {
                if (tokenizedWord == word || LevenshteinDistance(tokenizedWord, word) <= distance)
                    return true;
            }

            return false;
        }

        [BindableMethod]
        public bool HasWordThatSoundLike(string text, string word, string separator = " ")
        {
            if (word == null || word == string.Empty)
                return false;

            if (text == null || text == string.Empty)
                return false;

            var soundexedWord = _soundex.For(word);

            foreach (var tokenizedWord in text.Split(separator[0]))
            {
                if (soundexedWord == _soundex.For(tokenizedWord))
                    return true;
            }

            return false;
        }

        [BindableMethod]
        public bool HasTextThatSoundLikeSentence(string text, string sentence, string separator = " ")
        {
            if (sentence == null || sentence == string.Empty)
                return false;

            if (text == null || text == string.Empty)
                return false;

            var words = sentence.Split(separator[0]);
            var tokens = text.Split(separator[0]);
            var wordsMatchTable = new bool[words.Length];

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                var soundexedWord = _soundex.For(word);

                foreach (var token in tokens)
                {
                    if (soundexedWord == _soundex.For(token))
                    {
                        wordsMatchTable[i] = true;
                        break;
                    }
                }
            }

            return wordsMatchTable.All(entry => entry);
        }

        [BindableMethod]
        public string ToUpperInvariant(string value)
        {
            if (value == null)
                return null;

            return value.ToUpperInvariant();
        }

        [BindableMethod]
        public string ToLowerInvariant(string value)
        {
            if (value == null)
                return null;

            return value.ToLowerInvariant();
        }

        [BindableMethod]
        public string PadLeft(string value, string character, int? totalWidth)
        {
            if (value == null || character == null)
                return null;

            if (totalWidth == null)
                return null;

            return value.PadLeft(totalWidth.Value, character[0]);
        }

        [BindableMethod]
        public string PadRight(string value, string character, int? totalWidth)
        {
            if (value == null || character == null)
                return null;

            if (totalWidth == null)
                return null;

            return value.PadRight(totalWidth.Value, character[0]);
        }

        [BindableMethod]
        public string Head(string value, int? length = 10)
        {
            if (value == null)
                return null;

            return value.Substring(0, length.Value);
        }

        [BindableMethod]
        public string Tail(string value, int? length = 10)
        {
            if (value == null)
                return null;

            if (length == null)
                return null;

            return value.Substring(value.Length - length.Value, length.Value);
        }


        [BindableMethod]
        public int? LevenshteinDistance(string firstValue, string secondValue)
        {
            if (firstValue == null || secondValue == null)
                return null;

            return Fastenshtein.Levenshtein.Distance(firstValue, secondValue);
        }

        [BindableMethod]
        public char? GetCharacterOf(string value, int index)
        {
            if (value.Length <= index || index < 0)
                return null;

            return value[index];
        }

        [BindableMethod]
        public string Reverse(string value)
        {
            if (value == null)
                return null;

            if (value == string.Empty)
                return value;

            if (value.Length == 1)
                return value;

            return string.Concat(value.Reverse());
        }

        [BindableMethod]
        public string[] Split(string value, params string[] separators) => value.Split(separators, StringSplitOptions.None);

        [BindableMethod]
        public string LongestCommonSubstring(string source, string pattern)
            => new string(LongestCommonSequence(source, pattern).ToArray());

        [BindableMethod]
        public string Replicate(string value, int integer)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < integer; ++i)
                builder.Append(value);

            return builder.ToString();
        }

        [BindableMethod]
        public string Translate(string value, string characters, string translations)
        {
            if (value == null)
                return null;

            if (characters == null || translations == null)
                return null;

            if (characters.Length != translations.Length)
                return null;

            var builder = new StringBuilder();

            for(int i = 0; i < value.Length; ++i)
            {
                var index = characters.IndexOf(value[i]);

                if (index == -1)
                    builder.Append(value[i]);
                else
                    builder.Append(translations[index]);
            }

            return builder.ToString();
        }

        [BindableMethod]
        public string Replace(string text, string lookFor, string changeTo)
        {
            if (text == null)
                return null;

            if (string.IsNullOrEmpty(lookFor))
                return text;

            if (changeTo == null)
                return text;

            return text.Replace(lookFor, changeTo);
        }

        [BindableMethod]
        public string CapitalizeFirstLetterOfWords(string value)
        {
            if (value == null)
                return null;

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
        }

        [BindableMethod]
        public string GetNthWord(string text, int wordIndex, string separator)
        {
            if (text == null || separator == null)
                return null;

            var splitted = text.Split(separator[0]);

            if (wordIndex >= splitted.Length)
                return null;

            return splitted[wordIndex];
        }

        [BindableMethod]
        public string GetFirstWord(string text, string separator)
        {
            return GetNthWord(text, 1, separator);
        }


        [BindableMethod]
        public string GetSecondWord(string text, string separator)
        {
            return GetNthWord(text, 2, separator);
        }


        [BindableMethod]
        public string GetThirdWord(string text, string separator)
        {
            return GetNthWord(text, 3, separator);
        }

        [BindableMethod]
        public string GetLastWord(string text, string separator)
        {
            if (text == null || separator == null)
                return null;

            var splitted = text.Split(separator[0]);

            return splitted[splitted.Length - 1];
        }
    }
}
