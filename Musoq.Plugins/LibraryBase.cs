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

namespace Musoq.Plugins
{
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public abstract class LibraryBase
    {
        [AggregationGetMethod]
        public string AggregateValue([InjectGroup] Group group, string name)
        {
            var list = group.GetOrCreateValue<List<string>>(name);

            var builder = new StringBuilder();
            for (int i = 0, j = list.Count - 1; i < j; i++)
            {
                builder.Append(list[i]);
                builder.Append(',');
            }

            builder.Append(list[list.Count - 1]);

            return builder.ToString();
        }

        [AggregationSetMethod]
        public void SetAggregateValue([InjectGroup] Group group, string name, string value)
        {
            AggregateAdd(group, name, value);
        }

        [AggregationSetMethod]
        public void SetAggregateValue([InjectGroup] Group group, string name, decimal value)
        {
            AggregateAdd(group, name, value.ToString(CultureInfo.InvariantCulture));
        }

        [AggregationSetMethod]
        public void SetAggregateValue([InjectGroup] Group group, string name, long value)
        {
            AggregateAdd(group, name, value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AggregateAdd<TType>(Group group, string name, TType value)
        {
            var values = group.GetOrCreateValue(name, new List<TType>());
            values.Add(value);
        }

        [AggregationGetMethod]
        public int Count([InjectGroup] Group group, string name)
        {
            return group.GetValue<int>(name);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, string value)
        {
            var values = group.GetOrCreateValue<int>(name);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, decimal value)
        {
            var values = group.GetOrCreateValue(name, 1);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, DateTimeOffset value)
        {
            var values = group.GetOrCreateValue(name, 1);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, long value)
        {
            var values = group.GetOrCreateValue(name, 1);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, int value)
        {
            var values = group.GetOrCreateValue(name, 1);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, short value)
        {
            var values = group.GetOrCreateValue(name, 1);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, bool value)
        {
            var values = group.GetOrCreateValue(name, 1);
            group.SetValue(name, values + 1);
        }

        [AggregationGetMethod]
        public int ParentCount([InjectGroup] Group group, string name)
        {
            var parentGroup = group.GetRawValue<Group>(name);
            return parentGroup.Count;
        }

        [AggregationSetMethod]
        public void SetParentCount([InjectGroup] Group group, string name, long number)
        {
            var parent = GetParentGroup(group, number);
            group.GetOrCreateValueWithConverter<Group, int>(name, parent, o => ((Group)o).Count);
        }

        [AggregationGetMethod]
        public decimal Sum([InjectGroup] Group group, string name)
        {
            return group.GetValue<decimal>(name);
        }
        
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, decimal number)
        {
            var value = group.GetOrCreateValue<decimal>(name);
            group.SetValue(name, value + number);
        }

        [AggregationGetMethod]
        public decimal SumIncome([InjectGroup] Group group, string name)
        {
            return group.GetValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetSumIncome([InjectGroup] Group group, string name, decimal number)
        {
            var value = group.GetOrCreateValue<decimal>(name);
            
            if(number >= 0)
                group.SetValue(name, value + number);
        }

        [AggregationGetMethod]
        public decimal SumOutcome([InjectGroup] Group group, string name)
        {
            return group.GetValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetSumOutcome([InjectGroup] Group group, string name, decimal number)
        {
            var value = group.GetOrCreateValue<decimal>(name);

            if(number < 0)
                group.SetValue(name, value + number);
        }

        [AggregationGetMethod]
        public decimal StringAsNumericSum([InjectGroup] Group group, string name)
        {
            return group.GetValue<decimal>(name);
        }
        
        [AggregationSetMethod]
        public void SetStringAsNumericSum([InjectGroup] Group group, string name, string number)
        {
            var value = group.GetOrCreateValue<decimal>(name);
            group.SetValue(name, value + ToDecimal(number));
        }

        [AggregationGetMethod]
        public decimal Max([InjectGroup] Group group, string name)
        {
            return group.GetValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetMax([InjectGroup] Group group, string name, decimal value)
        {
            var storedValue = group.GetOrCreateValue<decimal>(name);

            if (storedValue < value)
                group.SetValue(name, value);
        }

        [AggregationGetMethod]
        public decimal Min([InjectGroup] Group group, string name)
        {
            return group.GetValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetMin([InjectGroup] Group group, string name, decimal value)
        {
            var storedValue = group.GetOrCreateValue<decimal>(name);

            if (storedValue > value)
                group.SetValue(name, value);
        }

        [AggregationGetMethod]
        public decimal Avg([InjectGroup] Group group, string name)
        {
            return Sum(group, name) / group.Count;
        }

        [AggregationSetMethod]
        public void SetAvg([InjectGroup] Group group, string name, decimal value)
        {
            SetSum(group, name, value);
        }

        [AggregationGetMethod]
        public decimal Dominant([InjectGroup] Group group, string name)
        {
            var dict = group.GetValue<SortedDictionary<decimal, Occurence>>(name);

            return dict.First().Key;
        }

        [AggregationSetMethod]
        public void SetDominant([InjectGroup] Group group, string name, decimal value)
        {
            var dict = group.GetOrCreateValue(name, new SortedDictionary<decimal, Occurence>());

            if (!dict.TryGetValue(value, out var occur))
            {
                occur = new Occurence();
                dict.Add(value, occur);
            }

            occur.Increment();
        }

        [AggregationGetMethod]
        public decimal SumIncome([InjectGroup] Group group, string name, long number)
        {
            var parent = GetParentGroup(group, number);
            var value = parent.GetRawValue<decimal>(name);
            return value;
        }

        [AggregationSetMethod]
        public void SetSumIncome([InjectGroup] Group group, string name, long number, decimal value)
        {
            var parent = GetParentGroup(group, number);
            SetSumIncome(parent, name, value);
            group.GetOrCreateValueWithConverter<Group, decimal>(name, parent,
                o => ((Group) o).GetRawValue<decimal>(name));
        }

        [AggregationGetMethod]
        public decimal SumOutcome([InjectGroup] Group group, string name, long number)
        {
            var parent = GetParentGroup(group, number);
            var value = parent.GetRawValue<decimal>(name);
            return value;
        }

        [AggregationSetMethod]
        public void SetSumOutcome([InjectGroup] Group group, string name, long number, decimal value)
        {
            var parent = GetParentGroup(group, number);
            SetSumOutcome(parent, name, value);
            group.GetOrCreateValueWithConverter<Group, decimal>(name, parent,
                o => ((Group)o).GetRawValue<decimal>(name));
        }

        [BindableMethod]
        public decimal PercentOf(decimal value, decimal max)
        {
            return value * 100 / max;
        }

        [BindableMethod]
        public long ExtractFromDate(string date, string partOfDate)
        {
            var value = DateTime.Parse(date);
            switch (partOfDate)
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
        public string ClusterIfLesserThan(decimal value, decimal edgeValue)
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

        private readonly IDictionary<string, IDictionary<string, string>> _fileNameToClusteredWordsMapDictionary =
            new Dictionary<string, IDictionary<string, string>>();

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

                        if(line == Environment.NewLine || line == string.Empty)
                            continue;

                        if (line.EndsWith(":"))
                        {
                            currentKey = line.Substring(0, line.Length - 1);
                        }
                        else
                        {
                            map.Add(line, currentKey);
                        }
                    }
                }
            }

            value = value.ToLowerInvariant();

            var dict = _fileNameToClusteredWordsMapDictionary[dictionaryFilename];
            var newValue = dict.FirstOrDefault(f => value.Contains(f.Key)).Value;

            return newValue ?? "other";
        }

        [BindableMethod]
        public string Concat(string first, string second)
        {
            return first + second;
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
        public string Coalesce(string exp1, string exp2, string exp3)
        {
            if (!string.IsNullOrEmpty(exp1))
                return exp1;
            if (!string.IsNullOrEmpty(exp2))
                return exp2;
            if (!string.IsNullOrEmpty(exp3))
                return exp3;

            return string.Empty;
        }

        [BindableMethod]
        public long Coalesce(long exp1, long exp2, long exp3)
        {
            return Coalesce<long>(exp1, exp2, exp3);
        }

        [BindableMethod]
        public int Coalesce(int exp1, int exp2, int exp3)
        {
            return Coalesce<int>(exp1, exp2, exp3);
        }

        [BindableMethod]
        public long Coalesce(short exp1, short exp2, short exp3)
        {
            return Coalesce<short>(exp1, exp2, exp3);
        }

        private static T Coalesce<T>(T exp1, T exp2, T exp3)
        {
            if (!exp1.Equals(default(T)))
                return exp1;
            if (!exp2.Equals(default(T)))
                return exp2;
            if (!exp3.Equals(default(T)))
                return exp3;

            return default(T);
        }

        [BindableMethod]
        public string Reduce(string value)
        {
            var spaces = 0;
            var text = new StringBuilder();

            foreach (var character in value)
            {
                if (character == ' ')
                {
                    spaces += 1;
                }
                else
                {
                    if (spaces > 0)
                        spaces = 0;
                }

                if (character == ',')
                    continue;

                if (spaces > 1)
                    continue;

                text.Append(character);
            }

            var i = text.Length - 1;
            while (text[i] == ' ')
            {
                text.Remove(i, 1);
                i -= 1;
            }

            return text.ToString();
        }

        [BindableMethod]
        public decimal Round(decimal value, int precision)
        {
            return Math.Round(value, precision);
        }

        [BindableMethod]
        public decimal ToDecimal(string value)
        {
            return Convert.ToDecimal(value.Replace(" ", string.Empty));
        }

        [BindableMethod]
        public decimal ToDecimal(long value)
        {
            return value;
        }

        [BindableMethod]
        public decimal ToDecimal(decimal value)
        {
            return value;
        }

        [BindableMethod]
        public string ToString(DateTimeOffset date)
        {
            return date.ToString();
        }

        [BindableMethod]
        public string ToString(DateTime date)
        {
            return date.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public TimeSpan DateDiff(DateTimeOffset first, DateTimeOffset second)
        {
            return second - first;
        }

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
        public string Sha512(string content)
        {
            return CalculateHash<SHA512Managed>(content);
        }

        [BindableMethod]
        public string Sha256(string content)
        {
            return CalculateHash<SHA256Managed>(content);
        }

        [BindableMethod]
        public string Md5(string content)
        {
            return CalculateHash<MD5CryptoServiceProvider>(content);
        }

        [BindableMethod]
        public decimal Abs(decimal value)
        {
            return Math.Abs(value);
        }

        [BindableMethod]
        public long Abs(long value)
        {
            return Math.Abs(value);
        }

        [BindableMethod]
        public int Abs(int value)
        {
            return Math.Abs(value);
        }

        [BindableMethod]
        public short Abs(short value)
        {
            return Math.Abs(value);
        }

        [BindableMethod]
        public string ToString(object obj)
        {
            return obj.ToString();
        }

        [BindableMethod]
        public bool Like(string content, string searchFor)
        {
            return 
                new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\")
                              .Replace(searchFor, ch => @"\" + ch).Replace('_', '.')
                              .Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(content);
        }

        private static string CalculateHash<THashProvider>(string content)
            where THashProvider : HashAlgorithm, new()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var provider = new THashProvider();
                var hash = provider.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static Group GetParentGroup(Group group, long number)
        {
            var i = 0;
            var parent = group;

            while (parent.Parent != null && i < number)
            {
                parent = parent.Parent;
                i += 1;
            }

            return parent;
        }

        private class Occurence
        {
            public int Value { get; private set; }

            public void Increment()
            {
                Value += 1;
            }
        }
    }
}