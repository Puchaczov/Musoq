using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Disk.Console
{
    public class ConsoleSource : RowSource
    {
        private readonly string _content;

        public ConsoleSource(string content)
        {
            _content = content;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                var splitter = LazySplit(_content, Environment.NewLine);
                var enumerator = splitter.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    yield return new EntityResolver<ConsoleEntity>(new ConsoleEntity{ Content = enumerator.Current }, SchemaConsoleHelper.NameToIndexMap, SchemaConsoleHelper.IndexToMethodAccessMap);
                }
            }
        }

        private static IEnumerable<string> LazySplit(string input, string pattern)
        {
            var buffer = new StringBuilder();

            for (int i = 0; i < input.Length - pattern.Length; i++)
            {
                var match = true;

                for (int j = i, k = 0; j < i + pattern.Length - 1; j++, k++)
                {
                    if (input[j] == pattern[k]) continue;

                    match = false;
                    break;
                }

                if (match) yield return buffer.ToString();

                if(i + 1 == input.Length) yield break;

                buffer.Append(input[i]);
            }
        }
    }
    public class ConsoleEntity
    {
        public string Content { get; set; }
    }
}