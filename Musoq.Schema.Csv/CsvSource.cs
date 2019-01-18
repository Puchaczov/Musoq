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
        private class CsvFile
        {
            public string FilePath { get; set; }
            public string Separator { get; set; }
            public bool HasHeader { get; set; }
            public int SkipLines { get; set; }
        }

        private readonly CsvFile[] _files;
        private readonly InterCommunicator _communicator;

        public CsvSource(string filePath, string separator, bool hasHeader, int skipLines, InterCommunicator communicator)
        {
            _files = new CsvFile[] {
                new CsvFile()
                {
                    FilePath = filePath,
                    HasHeader = hasHeader,
                    Separator = separator,
                    SkipLines = skipLines
                }
            };
            _communicator = communicator;
        }

        public CsvSource(IReadOnlyTable table, InterCommunicator communicator)
        {
            _files = new CsvFile[table.Count];

            for(int i = 0; i < table.Count; ++i)
            {
                var row = table.Rows[i];
                _files[i] = new CsvFile()
                {
                    FilePath = (string)row[0],
                    Separator = (string)row[1],
                    HasHeader = (bool)row[2],
                    SkipLines = (int)row[3]
                };
            }

            _communicator = communicator;
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<string[]>>> chunkedSource)
        {
            foreach(var csvFile in _files)
            {
                ProcessFile(csvFile, chunkedSource);
            }
        }

        private void ProcessFile(CsvFile csvFile, BlockingCollection<IReadOnlyList<EntityResolver<string[]>>> chunkedSource)
        {
            var file = new FileInfo(csvFile.FilePath);

            if (!file.Exists)
            {
                chunkedSource.Add(new List<EntityResolver<string[]>>());
                return;
            }

            var nameToIndexMap = new Dictionary<string, int>();
            var indexToMethodAccess = new Dictionary<int, Func<string[], object>>();
            var endWorkToken = _communicator.EndWorkToken;

            using (var stream = CreateStreamFromFile(file))
            {
                using (var reader = new StreamReader(stream))
                {
                    SkipLines(reader, csvFile);

                    using (var csvReader = new CsvReader(reader))
                    {
                        csvReader.Configuration.Delimiter = csvFile.Separator;
                        csvReader.Read();

                        var header = csvReader.Context.Record;

                        for (var i = 0; i < header.Length; ++i)
                        {
                            nameToIndexMap.Add(csvFile.HasHeader ? CsvHelper.MakeHeaderNameValidColumnName(header[i]) : string.Format(CsvHelper.AutoColumnName, i + 1), i);

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
                    SkipLines(reader, csvFile);

                    using (var csvReader = new CsvReader(reader))
                    {
                        csvReader.Configuration.BadDataFound = context => { };
                        csvReader.Configuration.Delimiter = csvFile.Separator;

                        int i = 1, j = 11;
                        var list = new List<EntityResolver<string[]>>(100);
                        var rowsToRead = 1000;
                        const int rowsToReadBase = 100;

                        if (csvFile.HasHeader)
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

                            chunkedSource.Add(list, endWorkToken);
                            list = new List<EntityResolver<string[]>>(rowsToRead);
                        }

                        chunkedSource.Add(list, endWorkToken);
                    }
                }
            }
        }

        private void SkipLines(TextReader reader, CsvFile csvFile)
        {
            if (csvFile.SkipLines <= 0) return;

            var skippedLines = 0;
            while (skippedLines < csvFile.SkipLines)
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