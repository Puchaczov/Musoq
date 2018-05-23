using System.Data;
using System.IO;
using CsvHelper;

namespace Musoq.Console
{
    public class CsvPrinter : Printer
    {
        private readonly string _path;

        public CsvPrinter(DataTable table, string path)
            : base(table)
        {
            _path = path;
        }

        public override void Print()
        {
            var file = new FileInfo(_path);

            if (!Directory.Exists(file.DirectoryName)) return;

            using (var stream = new StreamWriter(File.OpenWrite(_path)))
            {
                var csv = new CsvWriter(stream);

                for (var index = 0; index < Table.Columns.Count - 1; index++)
                {
                    var field = Table.Columns[index];
                    csv.WriteField(field.ColumnName, true);
                }

                csv.WriteField(Table.Columns[Table.Columns.Count - 1].ColumnName, true);
                csv.NextRecord();

                for (var index = 0; index < Table.Rows.Count - 1; index++)
                {
                    var row = Table.Rows[index];
                    foreach (var item in row.ItemArray)
                        csv.WriteField(item);
                    csv.NextRecord();
                }

                csv.WriteField(Table.Rows[Table.Rows.Count - 1].ItemArray);
                csv.NextRecord();
            }
        }
    }
}