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

        public CsvSource(string filePath)
        {
            _filePath = filePath;
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
                {
                    stream = file.OpenRead();
                }
                else
                {
                    stream = new MemoryStream(Encoding.UTF8.GetBytes(file.OpenText().ReadToEnd()));
                }

                var readedRows = new BlockingCollection<List<EntityResolver<string[]>>>();
                var tokenSource = new CancellationTokenSource();

                Task.Factory.StartNew(() =>
                {
                    using (var reader = new CsvReader(new StreamReader(stream)))
                    {
                        reader.Configuration.BadDataFound = context => { };

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
                    }

                    tokenSource.Cancel();
                });

                using (var reader = new CsvReader(file.OpenText()))
                {
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