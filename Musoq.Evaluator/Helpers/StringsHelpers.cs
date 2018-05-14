using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Helpers
{
    /// <summary>
    /// Found here: https://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
    /// </summary>
    public static class StringHelpers
    {
        public static string Escape(this string s)
        {
            return escapeRegex.Replace(s, EscapeMatchEval);
        }

        private static readonly Dictionary<string, string> escapeMapping = new Dictionary<string, string>()
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
            {"\0", @"\0"},
        };

        private static Regex escapeRegex = new Regex(string.Join("|", escapeMapping.Keys.ToArray()));

        private static string EscapeMatchEval(Match m)
        {
            if (escapeMapping.ContainsKey(m.Value))
            {
                return escapeMapping[m.Value];
            }
            return escapeMapping[Regex.Escape(m.Value)];
        }
    }
}
