using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        private readonly IDictionary<string, IDictionary<string, string>> _fileNameToClusteredWordsMapDictionary =
            new Dictionary<string, IDictionary<string, string>>();

        [BindableMethod]
        public string Substr(string value, long index, long length)
        {
            if (value.Length < index + length + 1)
                length = value.Length - (index + 1);

            return value.Substring((int)index, (int)length);
        }

        [BindableMethod]
        public string Substr(string value, long length)
        {
            return Substr(value, 0, length);
        }

        [BindableMethod]
        public string ClusterIfLesserThan(decimal? value, decimal? edgeValue)
        {
            if (value >= 0)
                return "none";

            return value >= edgeValue ? "small" : "big";
        }

        [BindableMethod]
        public string CopyUntilStopword(string text, string stopword)
        {
            var index = text.IndexOf(stopword, StringComparison.Ordinal);

            return index == -1 ? text : text.Substring(0, index);
        }

        [BindableMethod]
        public string ClusteredByContainsKey(string dictionaryFilename, string value)
        {
            if (!_fileNameToClusteredWordsMapDictionary.ContainsKey(dictionaryFilename))
            {
                _fileNameToClusteredWordsMapDictionary.Add(dictionaryFilename, new Dictionary<string, string>());

                using (var stream = File.OpenRead(dictionaryFilename))
                {
                    var reader = new StreamReader(stream);
                    var map = _fileNameToClusteredWordsMapDictionary[dictionaryFilename];
                    var currentKey = string.Empty;

                    while (!reader.EndOfStream)
                    {
                        var line = reader
                            .ReadLine()
                            .ToLowerInvariant()
                            .Trim();

                        if (line == System.Environment.NewLine || line == string.Empty)
                            continue;

                        if (line.EndsWith(":"))
                            currentKey = line.Substring(0, line.Length - 1);
                        else
                            map.Add(line, currentKey);
                    }
                }
            }

            value = value.ToLowerInvariant();

            var dict = _fileNameToClusteredWordsMapDictionary[dictionaryFilename];
            var newValue = dict.FirstOrDefault(f => value.Contains(f.Key)).Value;

            return newValue ?? "other";
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
        public bool Contains(string content, string what)
        {
            return content.Contains(what);
        }

        [BindableMethod]
        public string TrimWhen(string value, string pattern)
        {
            var match = Regex.Match(value, pattern);

            return match.Success ? value.Substring(0, match.Index).TrimEnd() : value;
        }

        [BindableMethod]
        public int IndexOf(string value, string text)
        {
            return value.IndexOf(text, StringComparison.Ordinal);
        }
    }
}
