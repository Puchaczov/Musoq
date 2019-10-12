using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.SeparatedValues
{
    public class SeparatedValuesSource : RowSourceBase<object[]>
    {
        private class SeparatedValueFile
        {
            public string FilePath { get; set; }

            public string Separator { get; set; }

            public bool HasHeader { get; set; }

            public int SkipLines { get; set; }
        }

        private readonly SeparatedValueFile[] _files;
        private readonly RuntimeContext _context;
        private readonly IReadOnlyDictionary<string, Type> _types;

        public SeparatedValuesSource(string filePath, string separator, bool hasHeader, int skipLines, RuntimeContext context)
            : this(context)
        {
            _files = new[] {
                new SeparatedValueFile()
                {
                    FilePath = filePath,
                    HasHeader = hasHeader,
                    Separator = separator,
                    SkipLines = skipLines
                }
            };
        }

        public SeparatedValuesSource(IReadOnlyTable table, string separator, RuntimeContext context)
            : this(context)
        {
            _files = new SeparatedValueFile[table.Count];

            for (int i = 0; i < table.Count; ++i)
            {
                var row = table.Rows[i];
                _files[i] = new SeparatedValueFile()
                {
                    FilePath = (string)row[0],
                    Separator = separator,
                    HasHeader = (bool)row[1],
                    SkipLines = (int)row[2]
                };
            }
        }

        private SeparatedValuesSource(RuntimeContext context)
        {
            _context = context;
            _types = _context.AllColumns.ToDictionary(col => col.ColumnName, col => col.ColumnType.GetUnderlyingNullable());
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<DataSources.IObjectResolver>> chunkedSource)
        {
            foreach (var csvFile in _files)
            {
                ProcessFile(csvFile, chunkedSource);
            }
        }

        private void ProcessFile(SeparatedValueFile csvFile, BlockingCollection<IReadOnlyList<DataSources.IObjectResolver>> chunkedSource)
        {
            var file = new FileInfo(csvFile.FilePath);

            if (!file.Exists)
            {
                chunkedSource.Add(new List<EntityResolver<object[]>>());
                return;
            }

            var nameToIndexMap = new Dictionary<string, int>();
            var indexToMethodAccess = new Dictionary<int, Func<object[], object>>();
            var indexToNameMap = new Dictionary<int, string>();
            var endWorkToken = _context.EndWorkToken;

            using (var stream = CreateStreamFromFile(file))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    SkipLines(reader, csvFile);

                    using (var csvReader = new CsvReader(reader))
                    {
                        csvReader.Configuration.Delimiter = csvFile.Separator;
                        csvReader.Read();

                        var header = csvReader.Context.Record;

                        for (var i = 0; i < header.Length; ++i)
                        {
                            var headerName = csvFile.HasHeader ? SeparatedValuesHelper.MakeHeaderNameValidColumnName(header[i]) : string.Format(SeparatedValuesHelper.AutoColumnName, i + 1);
                            nameToIndexMap.Add(headerName, i);
                            indexToNameMap.Add(i, headerName);
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
                            list.Add(new EntityResolver<object[]>(ParseRecords(rawRow, indexToNameMap), nameToIndexMap, indexToMethodAccess));

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

        private object[] ParseRecords(string[] rawRow, IReadOnlyDictionary<int, string> indexToNameMap)
        {
            var parsedRecords = new object[rawRow.Length];

            for (int i = 0; i < rawRow.Length; ++i)
            {
                var headerName = indexToNameMap[i];
                if (_types.ContainsKey(headerName))
                {
                    var colValue = rawRow[i];
                    switch (Type.GetTypeCode(_types[headerName]))
                    {
                        case TypeCode.Boolean:
                            if (bool.TryParse(colValue, out var boolValue))
                                parsedRecords[i] = boolValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.Byte:
                            if (byte.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue))
                                parsedRecords[i] = byteValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.Char:
                            if (char.TryParse(colValue, out var charValue))
                                parsedRecords[i] = charValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.DateTime:
                            if (DateTime.TryParse(colValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
                                parsedRecords[i] = dateTimeValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.DBNull:
                            throw new NotSupportedException($"Type {TypeCode.DBNull} is not supported.");
                        case TypeCode.Decimal:
                            if (decimal.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                                parsedRecords[i] = decimalValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.Double:
                            if (double.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
                                parsedRecords[i] = doubleValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.Empty:
                            throw new NotSupportedException($"Type {TypeCode.Empty} is not supported.");
                        case TypeCode.Int16:
                            if (short.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue))
                                parsedRecords[i] = shortValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.Int32:
                            if (int.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
                                parsedRecords[i] = intValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.Int64:
                            if (long.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
                                parsedRecords[i] = longValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.Object:
                            parsedRecords[i] = colValue;
                            break;
                        case TypeCode.SByte:
                            if (sbyte.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var sbyteValue))
                                parsedRecords[i] = sbyteValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.Single:
                            if (float.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                                parsedRecords[i] = floatValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.String:
                            if (string.IsNullOrEmpty(colValue))
                                parsedRecords[i] = null;
                            else
                                parsedRecords[i] = colValue;
                            break;
                        case TypeCode.UInt16:
                            if (ushort.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ushortValue))
                                parsedRecords[i] = ushortValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.UInt32:
                            if (uint.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var uintValue))
                                parsedRecords[i] = uintValue;
                            else
                                parsedRecords[i] = null;
                            break;
                        case TypeCode.UInt64:
                            if (ulong.TryParse(colValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ulongValue))
                                parsedRecords[i] = ulongValue;
                            else
                                parsedRecords[i] = null;
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

        private void SkipLines(TextReader reader, SeparatedValueFile csvFile)
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