using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Musoq.Schema.SeparatedValues
{
    public class SeparatedValuesLibrary : LibraryBase
    {
        private readonly IDictionary<string, IDictionary<string, string>> _fileNameToClusteredWordsMapDictionary =
       new Dictionary<string, IDictionary<string, string>>();

        [BindableMethod]
        public string ClusteredByContainsKey(string dictionaryFilename, string value)
        {
            if (!_fileNameToClusteredWordsMapDictionary.ContainsKey(dictionaryFilename))
            {
                _fileNameToClusteredWordsMapDictionary.Add(dictionaryFilename, new Dictionary<string, string>());

                using var stream = File.OpenRead(dictionaryFilename);
                using var reader = new StreamReader(stream);
                var map = _fileNameToClusteredWordsMapDictionary[dictionaryFilename];
                var currentKey = string.Empty;

                while (!reader.EndOfStream)
                {
                    var line = reader
                        .ReadLine()
                        ?.ToLowerInvariant()
                        .Trim();

                    if (line == System.Environment.NewLine || line == string.Empty)
                        continue;

                    if (line.EndsWith(":"))
                        currentKey = line.Substring(0, line.Length - 1);
                    else
                        map.Add(line, currentKey);
                }
            }

            value = value.ToLowerInvariant();

            var dict = _fileNameToClusteredWordsMapDictionary[dictionaryFilename];
            var newValue = dict.FirstOrDefault(f => value.Contains(f.Key)).Value;

            return newValue ?? "other";
        }
    }
}