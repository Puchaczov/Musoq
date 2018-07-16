using System;
using System.IO;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Csv
{
    public class CsvBasedTable : ISchemaTable
    {
        public CsvBasedTable(string fileName, string separator, bool hasHeader, int skipLines)
        {
            var file = new FileInfo(fileName);
            using (var stream = new StreamReader(file.OpenRead()))
            {
                var line = string.Empty;

                var currentLine = 0;
                while (!stream.EndOfStream && ((line = stream.ReadLine()) == string.Empty || currentLine < skipLines))
                {
                    currentLine += 1;
                }

                var columns = line.Split(new[] {separator}, StringSplitOptions.None);

                if (hasHeader)
                    Columns = columns
                        .Select((header, i) => (ISchemaColumn) new SchemaColumn(CsvHelper.MakeHeaderNameValidColumnName(header), i, typeof(string)))
                        .ToArray();
                else
                    Columns = columns
                        .Select((f, i) => (ISchemaColumn) new SchemaColumn(string.Format(CsvHelper.AutoColumnName, i + 1), i, typeof(string)))
                        .ToArray();
            }
        }

        public ISchemaColumn[] Columns { get; }
    }
}