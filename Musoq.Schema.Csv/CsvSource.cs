using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using CsvHelper;
using Musoq.Schema.DataSources;
using IObjectResolver = Musoq.Schema.DataSources.IObjectResolver;

namespace Musoq.Schema.Csv
{
    public class CsvSource : RowSource
    {
        private readonly string _filePath;
        private readonly string _separator;

        public CsvSource(string filePath, string separator)
        {
            _filePath = filePath;
            _separator = separator;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                var file = new FileInfo(_filePath);

                var nameToIndexMap = new Dictionary<string, int>();
                var indexToMethodAccess = new Dictionary<int, Func<string[], object>>();

                Stream stream;
                if (SizeConverter.ToMegabytes(file.Length) > Performance.FreeMemoryInMegabytes())
                    stream = file.OpenRead();
                else
                    stream = new MemoryStream(Encoding.UTF8.GetBytes(file.OpenText().ReadToEnd()));

                var readedRows = new BlockingCollection<List<EntityResolver<string[]>>>();
                var tokenSource = new CancellationTokenSource();

                var thread = new Thread(() =>
                {
                    try
                    {
                        using (var reader = new CsvReader(new StreamReader(stream)))
                        {
                            reader.Configuration.BadDataFound = context => { };
                            reader.Configuration.Delimiter = _separator;

                            int i = 1, j = 11;
                            var list = new List<EntityResolver<string[]>>(100);
                            var rowsToRead = 1000;
                            const int rowsToReadBase = 100;

                            reader.Read(); //skip header.

                            while (reader.Read())
                            {
                                var rawRow = reader.Context.Record;
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
                    catch (Exception exc)
                    {
                        if (Debugger.IsAttached)
                            Debug.WriteLine(exc);
                    }
                    finally
                    {
                        tokenSource.Cancel();
                    }
                });

                thread.Start();

                using (var reader = new CsvReader(file.OpenText()))
                {
                    reader.Configuration.Delimiter = _separator;
                    reader.Read();

                    var header = reader.Context.Record;

                    for (var i = 0; i < header.Length; ++i)
                    {
                        nameToIndexMap.Add(header[i], i);

                        var i1 = i;
                        indexToMethodAccess.Add(i, row => row[i1]);
                    }
                }

                return new ChunkedFile(readedRows, tokenSource.Token);
            }
        }
    }
}