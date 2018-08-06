using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Csv
{
    public class CsvSource : RowSourceBase<string[]>
    {
        private readonly string _filePath;
        private readonly string _separator;
        private readonly bool _hasHeader;
        private readonly int _skipLines;

        public CsvSource(string filePath, string separator, bool hasHeader, int skipLines)
        {
            _filePath = filePath;
            _separator = separator;
            _hasHeader = hasHeader;
            _skipLines = skipLines;
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<string[]>>> chunkedSource)
        {
            var file = new FileInfo(_filePath);

            if (!file.Exists)
            {
                chunkedSource.Add(new List<EntityResolver<string[]>>());
                return;
            }

            var nameToIndexMap = new Dictionary<string, int>();
            var indexToMethodAccess = new Dictionary<int, Func<string[], object>>();

            using (var stream = CreateStreamFromFile(file))
            {
                using (var reader = new StreamReader(stream))
                {
                    SkipLines(reader);

                    using (var csvReader = new CsvReader(reader))
                    {
                        csvReader.Configuration.Delimiter = _separator;
                        csvReader.Read();

                        var header = csvReader.Context.Record;

                        for (var i = 0; i < header.Length; ++i)
                        {
                            nameToIndexMap.Add(_hasHeader ? CsvHelper.MakeHeaderNameValidColumnName(header[i]) : string.Format(CsvHelper.AutoColumnName, i + 1), i);

                            var i1 = i;
                            indexToMethodAccess.Add(i, row => row[i1]);
                        }
                    }
                }
            }

            using (var stream = CreateStreamFromFile(file))
            {
                using (var reader = new StreamReader(stream))
                {
                    SkipLines(reader);

                    using (var csvReader = new CsvReader(reader))
                    {
                        csvReader.Configuration.BadDataFound = context => { };
                        csvReader.Configuration.Delimiter = _separator;

                        int i = 1, j = 11;
                        var list = new List<EntityResolver<string[]>>(100);
                        var rowsToRead = 1000;
                        const int rowsToReadBase = 100;

                        if (_hasHeader)
                            csvReader.Read(); //skip header.

                        while (csvReader.Read())
                        {
                            var rawRow = csvReader.Context.Record;
                            list.Add(new EntityResolver<string[]>(rawRow, nameToIndexMap, indexToMethodAccess));

                            if (i++ < rowsToRead) continue;

                            i = 1;

                            if (j > 1)
                                j -= 1;

                            rowsToRead = rowsToReadBase * j;
                            
                            chunkedSource.Add(list);
                            list = new List<EntityResolver<string[]>>(rowsToRead);
                        }

                        chunkedSource.Add(list);
                    }
                }
            }
        }

        private void SkipLines(TextReader reader)
        {
            if (_skipLines <= 0) return;

            var skippedLines = 0;
            while (skippedLines < _skipLines)
            {
                reader.ReadLine();
                skippedLines += 1;
            }
        }

        private Stream CreateStreamFromFile(FileInfo file)
        {
            Stream stream;
            if (SizeConverter.ToMegabytes(file.Length) > Performance.FreeMemoryInMegabytes())
                stream = file.OpenRead();
            else
                stream = new MemoryStream(Encoding.UTF8.GetBytes(file.OpenText().ReadToEnd()));

            return stream;
        }
    }
}