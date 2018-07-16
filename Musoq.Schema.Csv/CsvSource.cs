using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Musoq.Schema.DataSources;
using IObjectResolver = Musoq.Schema.DataSources.IObjectResolver;

namespace Musoq.Schema.Csv
{
    public class CsvSource : RowSource
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

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                var tokenSource = new CancellationTokenSource();
                var file = new FileInfo(_filePath);

                if(!file.Exists)
                    return new ChunkedFile(new BlockingCollection<List<EntityResolver<string[]>>>(), tokenSource.Token);

                var nameToIndexMap = new Dictionary<string, int>();
                var indexToMethodAccess = new Dictionary<int, Func<string[], object>>();

                var readedRows = new BlockingCollection<List<EntityResolver<string[]>>>();

                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        using(var stream = CreateStreamFromFile(file))
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
                                        await csvReader.ReadAsync(); //skip header.

                                    while (await csvReader.ReadAsync())
                                    {
                                        var rawRow = csvReader.Context.Record;
                                        list.Add(new EntityResolver<string[]>(rawRow, nameToIndexMap, indexToMethodAccess));

                                        if (i++ < rowsToRead) continue;

                                        i = 1;

                                        if (j > 1)
                                            j -= 1;

                                        rowsToRead = rowsToReadBase * j;

                                        readedRows.Add(list);
                                        list = new List<EntityResolver<string[]>>(rowsToRead);
                                    }

                                    readedRows.Add(list);
                                }
                            }
                        }
                    }
                    finally
                    {
                        tokenSource.Cancel();
                    }
                });

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

                return new ChunkedFile(readedRows, tokenSource.Token);
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