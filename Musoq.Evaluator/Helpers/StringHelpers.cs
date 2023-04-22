using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Helpers
{
    public static class StringHelpers
    {
        private static readonly Dictionary<string, string> EscapeMapping = new()
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

        private static readonly Regex EscapeRegex = new(string.Join("|", EscapeMapping.Keys.ToArray()));

        /// <summary>
        ///     Found here:
        ///     https://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal?utm_medium=organic
        ///     &utm_source=google_rich_qa&utm_campaign=google_rich_qa
        /// </summary>
        public static string Escape(this string s)
        {
            return EscapeRegex.Replace(s, EscapeMatchEval);
        }

        private static string EscapeMatchEval(Match m)
        {
            return EscapeMapping.TryGetValue(m.Value, out var eval) ? eval : EscapeMapping[Regex.Escape(m.Value)];
        }

        private static readonly object NamespaceIdentifierGuard = new();
        private static long _namespaceUniqueId = 0;

        public static long GenerateNamespaceIdentifier()
        {
            long value;

            lock (NamespaceIdentifierGuard)
            {
                value = _namespaceUniqueId++;
            }

            return value;
        }
    }
}