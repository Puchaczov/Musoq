using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Csv
{
    public class CsvSource : RowSourceBase<object[]>
    {
        private class CsvFile
        {
            public string FilePath { get; set; }
            public string Separator { get; set; }
            public bool HasHeader { get; set; }
            public int SkipLines { get; set; }
        }

        private readonly CsvFile[] _files;
        private readonly RuntimeContext _context;
        private readonly IReadOnlyDictionary<int, Type> _types;

        public CsvSource(string filePath, string separator, bool hasHeader, int skipLines, RuntimeContext context)
            : this(context)
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
        }

        public CsvSource(IReadOnlyTable table, RuntimeContext context)
            : this(context)
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
        }

        private CsvSource(RuntimeContext context)
        {
            _context = context;
            _types = _context.AllColumns.ToDictionary(col => col.ColumnIndex, col => col.ColumnType);
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<object[]>>> chunkedSource)
        {
            foreach(var csvFile in _files)
            {
                ProcessFile(csvFile, chunkedSource);
            }
        }

        private void ProcessFile(CsvFile csvFile, BlockingCollection<IReadOnlyList<EntityResolver<object[]>>> chunkedSource)
        {
            var file = new FileInfo(csvFile.FilePath);

            if (!file.Exists)
            {
                chunkedSource.Add(new List<EntityResolver<object[]>>());
                return;
            }

            var nameToIndexMap = new Dictionary<string, int>();
            var indexToMethodAccess = new Dictionary<int, Func<object[], object>>();
            var endWorkToken = _context.EndWorkToken;

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
                        var list = new List<EntityResolver<object[]>>(100);
                        var rowsToRead = 1000;
                        const int rowsToReadBase = 100;

                        if (csvFile.HasHeader)
                            csvReader.Read(); //skip header.

                        while (csvReader.Read())
                        {
                            var rawRow = csvReader.Context.Record;
                            list.Add(new EntityResolver<object[]>(ParseRecords(rawRow), nameToIndexMap, indexToMethodAccess));

                            if (i++ < rowsToRead) continue;

                            i = 1;

                            if (j > 1)
                                j -= 1;

                            rowsToRead = rowsToReadBase * j;

                            chunkedSource.Add(list, endWorkToken);
                            list = new List<EntityResolver<object[]>>(rowsToRead);
                        }

                        chunkedSource.Add(list, endWorkToken);
                    }
                }
            }
        }

        private object[] ParseRecords(string[] rawRow)
        {
            var parsedRecords = new object[rawRow.Length];

            for (int i = 0; i < rawRow.Length; ++i)
            {
                if (_types.ContainsKey(i))
                {
                    switch (Type.GetTypeCode(_types[i]))
                    {
                        case TypeCode.Boolean:
                            parsedRecords[i] = bool.Parse(rawRow[i]);
                            break;
                        case TypeCode.Byte:
                            parsedRecords[i] = byte.Parse(rawRow[i]);
                            break;
                        case TypeCode.Char:
                            parsedRecords[i] = char.Parse(rawRow[i]);
                            break;
                        case TypeCode.DateTime:
                            parsedRecords[i] = DateTime.Parse(rawRow[i], CultureInfo.InvariantCulture);
                            break;
                        case TypeCode.DBNull:
                            throw new NotSupportedException($"Type {TypeCode.DBNull} is not supported.");
                        case TypeCode.Decimal:
                            parsedRecords[i] = decimal.Parse(rawRow[i]);
                            break;
                        case TypeCode.Double:
                            parsedRecords[i] = double.Parse(rawRow[i]);
                            break;
                        case TypeCode.Empty:
                            throw new NotSupportedException($"Type {TypeCode.Empty} is not supported.");
                        case TypeCode.Int16:
                            parsedRecords[i] = short.Parse(rawRow[i]);
                            break;
                        case TypeCode.Int32:
                            parsedRecords[i] = int.Parse(rawRow[i]);
                            break;
                        case TypeCode.Int64:
                            parsedRecords[i] = long.Parse(rawRow[i]);
                            break;
                        case TypeCode.Object:
                            parsedRecords[i] = rawRow[i];
                            break;
                        case TypeCode.SByte:
                            parsedRecords[i] = sbyte.Parse(rawRow[i]);
                            break;
                        case TypeCode.Single:
                            parsedRecords[i] = decimal.Parse(rawRow[i]);
                            break;
                        case TypeCode.String:
                            parsedRecords[i] = rawRow[i];
                            break;
                        case TypeCode.UInt16:
                            parsedRecords[i] = ushort.Parse(rawRow[i]);
                            break;
                        case TypeCode.UInt32:
                            parsedRecords[i] = uint.Parse(rawRow[i]);
                            break;
                        case TypeCode.UInt64:
                            parsedRecords[i] = ulong.Parse(rawRow[i]);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    parsedRecords[i] = rawRow[i];
                }
            }

            return parsedRecords;
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