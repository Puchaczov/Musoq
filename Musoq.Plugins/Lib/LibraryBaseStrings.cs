using System;
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
        public string Substring(string value, int index, int length)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (length < 1)
                return string.Empty;

            var valueLastIndex = value.Length - 1;
            var computedLastIndex = index + (length - 1);

            if (valueLastIndex < computedLastIndex)
                length = ((value.Length - 1) - index) + 1;

            return value.Substring(index, length);
        }

        [BindableMethod]
        public string Substring(string value, int length)
        {
            return Substring(value, 0, length);
        }

        [BindableMethod]
        public string Concat(params string[] strings)
        {
            var concatedStrings = new StringBuilder();

            foreach (var value in strings)
                concatedStrings.Append(value);

            return concatedStrings.ToString();
        }

        [BindableMethod]
        public string Concat(params object[] objects)
        {
            var concatedStrings = new StringBuilder();

            foreach (var value in objects)
                concatedStrings.Append(value);

            return concatedStrings.ToString();
        }

        [BindableMethod]
        public bool Contains(string content, string what)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(what))
                return false;

            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(content, what, CompareOptions.IgnoreCase) >= 0;
        }

        [BindableMethod]
        public int IndexOf(string value, string text)
        {
            return value.IndexOf(text, StringComparison.OrdinalIgnoreCase);
        }

        [BindableMethod]
        public string Soundex(string value)
        {
            return _soundex.For(value);
        }

        [BindableMethod]
        public string ToUpperInvariant(string value)
        {
            return value.ToUpperInvariant();
        }

        [BindableMethod]
        public string ToLowerInvariant(string value)
        {
            return value.ToLowerInvariant();
        }

        [BindableMethod]
        public string PadLeft(string value, string character, int totalWidth)
        {
            return value.PadLeft(totalWidth, character[0]);
        }

        [BindableMethod]
        public string PadRight(string value, string character, int totalWidth)
        {
            return value.PadRight(totalWidth, character[0]);
        }

        [BindableMethod]
        public string Head(string value, int length) => value.Substring(0, length);

        [BindableMethod]
        public string Tail(string value, int length) => value.Substring(value.Length - length, length);


        [BindableMethod]
        public int LevenshteinDistance(string firstValue, string secondValue)
        {
            return Fastenshtein.Levenshtein.Distance(firstValue, secondValue);
        }

        [BindableMethod]
        public string LongestCommonSubstring(string source, string pattern)
            => new string(LongestCommonSequence(source, pattern).ToArray());
    }
}
