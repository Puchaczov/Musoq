using System;
using System.IO;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Csv
{
    public class CsvBasedTable : ISchemaTable
    {
        public CsvBasedTable(string fileName)
        {
            var file = new FileInfo(fileName);
            using (var stream = new StreamReader(file.OpenRead()))
            {
                var line = string.Empty;
                while (!stream.EndOfStream && (line = stream.ReadLine()) == string.Empty)
                {
                }

                var columns = line.Split(new[] {',', ';'}, StringSplitOptions.None);

                Columns = columns.Select((f, i) => (ISchemaColumn) new SchemaColumn(f, i, typeof(string))).ToArray();
            }
        }

        public ISchemaColumn[] Columns { get; }
    }
}