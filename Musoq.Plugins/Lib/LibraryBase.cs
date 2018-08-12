using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Helpers;

namespace Musoq.Plugins
{
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public abstract partial class LibraryBase : UserMethodsLibrary
    {
        private readonly IDictionary<string, IDictionary<string, string>> _fileNameToClusteredWordsMapDictionary =
            new Dictionary<string, IDictionary<string, string>>();

        [BindableMethod]
        public int RowNumber([InjectQueryStats] QueryStats info)
        {
            return info.RowNumber;
        }

        [BindableMethod]
        public decimal? PercentOf(decimal? value, decimal? max)
        {
            if (!value.HasValue)
                return null;

            if (!max.HasValue)
                return null;

            return value * 100 / max;
        }

        [BindableMethod]
        public int ExtractFromDate(string date, string partOfDate)
        {
            var value = DateTime.Parse(date);
            switch (partOfDate.ToLowerInvariant())
            {
                case "month":
                    return value.Month;
                case "year":
                    return value.Year;
                case "day":
                    return value.Day;
            }

            throw new NotSupportedException();
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

        [BindableMethod]
        public T Coalesce<T>(params T[] array)
        {
            foreach (var obj in array)
            {
                if (obj.Equals(default(T)))
                    return obj;
            }

            return default(T);
        }

        [BindableMethod]
        public decimal? Round(decimal? value, int precision)
        {
            if (!value.HasValue)
                return null;

            return Math.Round(value.Value, precision);
        }

        [BindableMethod]
        public decimal ToDecimal(string value)
        {
            return Convert.ToDecimal(value, CultureInfo.CurrentCulture);
        }

        [BindableMethod]
        public decimal ToDecimal(string value, string culture)
        {
            return Convert.ToDecimal(value, new CultureInfo(culture));
        }

        [BindableMethod]
        public decimal? ToDecimal(long? value)
        {
            return value;
        }

        [BindableMethod]
        public decimal? ToDecimal(decimal? value)
        {
            return value;
        }

        [BindableMethod]
        public long? ToLong(string value)
        {
            return long.Parse(value);
        }

        [BindableMethod]
        public string ToString(DateTimeOffset? date)
        {
            if (!date.HasValue)
                return null;

            return date.ToString();
        }

        [BindableMethod]
        public string ToString(DateTime? date)
        {
            return date?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(decimal? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(long? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public TimeSpan? DateDiff(DateTimeOffset? first, DateTimeOffset? second)
        {
            return second - first;
        }

        [BindableMethod]
        public string Substr(string value, long index, long length)
        {
            if (value.Length < index + length + 1)
                length = value.Length - (index + 1);

            return value.Substring((int) index, (int) length);
        }

        [BindableMethod]
        public string Substr(string value, long length)
        {
            return Substr(value, 0, length);
        }

        [BindableMethod]
        public string Sha512(string content)
        {
            return HashHelper.ComputeHash<SHA512Managed>(content);
        }

        [BindableMethod]
        public string Sha256(string content)
        {
            if (content == null)
                return null;

            return HashHelper.ComputeHash<SHA256Managed>(content);
        }

        [BindableMethod]
        public string Md5(string content)
        {
            if (content == null)
                return null;

            return HashHelper.ComputeHash<MD5CryptoServiceProvider>(content);
        }

        [BindableMethod]
        public decimal? Abs(decimal? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        [BindableMethod]
        public long? Abs(long? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        [BindableMethod]
        public int? Abs(int? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        [BindableMethod]
        public string ToString(object obj)
        {
            return obj?.ToString();
        }
    }
}