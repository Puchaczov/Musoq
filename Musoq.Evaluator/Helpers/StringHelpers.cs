using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Helpers
{
    /// <summary>
    ///     Found here:
    ///     https://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal?utm_medium=organic
    ///     &utm_source=google_rich_qa&utm_campaign=google_rich_qa
    /// </summary>
    public static class StringHelpers
    {
        private static readonly Dictionary<string, string> EscapeMapping = new Dictionary<string, string>
        {
            {"\"", @"\\\"""},
            {"\\\\", @"\\"},
            {"\a", @"\a"},
            {"\b", @"\b"},
            {"\f", @"\f"},
            {"\n", @"\n"},
            {"\r", @"\r"},
            {"\t", @"\t"},
            {"\v", @"\v"},
            {"\0", @"\0"}
        };

        private static readonly Regex EscapeRegex = new Regex(string.Join("|", EscapeMapping.Keys.ToArray()));

        public static string Escape(this string s)
        {
            return EscapeRegex.Replace(s, EscapeMatchEval);
        }

        public static string CreateAliasIfEmpty(string alias, IReadOnlyList<string> usedAliases)
        {
            if (!string.IsNullOrEmpty(alias))
                return alias;

            var aliasCandidate = RandomString(6);

            while (usedAliases.Contains(aliasCandidate) || char.IsDigit(aliasCandidate[0]))
                aliasCandidate = RandomString(6);

            return aliasCandidate;
        }

        private static string EscapeMatchEval(Match m)
        {
            if (EscapeMapping.ContainsKey(m.Value)) return EscapeMapping[m.Value];
            return EscapeMapping[Regex.Escape(m.Value)];
        }

        /// <summary>
        ///     Code found here:
        ///     https://stackoverflow.com/questions/730268/unique-random-string-generation?utm_medium=organic&amp;
        ///     utm_source=google_rich_qa&amp;utm_campaign=google_rich_qa
        /// </summary>
        /// <param name="length">Length of alias</param>
        /// <param name="allowedChars">Allowed characters.</param>
        /// <returns>Randomly generated alias.</returns>
        private static string RandomString(int length, string allowedChars = "abcdefghijklmnopqrstuvwxyz0123456789")
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be less than zero.");
            if (string.IsNullOrEmpty(allowedChars)) throw new ArgumentException("allowedChars may not be empty.");

            const int byteSize = 0x100;
            var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
            if (byteSize < allowedCharSet.Length)
                throw new ArgumentException(
                    $"allowedChars may contain no more than {byteSize} characters.");

            var rng = new Random();
            var result = new StringBuilder();
            var buf = new byte[128];
            while (result.Length < length)
            {
                rng.NextBytes(buf);
                for (var i = 0; i < buf.Length && result.Length < length; ++i)
                {
                    // Divide the byte into allowedCharSet-sized groups. If the
                    // random value falls into the last group and the last group is
                    // too small to choose from the entire allowedCharSet, ignore
                    // the value in order to avoid biasing the result.
                    var outOfRangeStart = byteSize - byteSize % allowedCharSet.Length;
                    if (outOfRangeStart <= buf[i]) continue;
                    result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                }
            }

            return result.ToString();
        }
    }
}