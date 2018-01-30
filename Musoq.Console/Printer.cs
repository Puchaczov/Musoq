using System;
using System.Data;
using System.IO;
using ConsoleTableExt;
using CsvHelper;

namespace Musoq.Console
{
    public abstract class Printer
    {
        protected readonly DataTable Table;

        public Printer(DataTable table)
        {
            Table = table;
        }

        public abstract void Print();
    }

    public class ConsolePrinter : Printer
    {
        private readonly TimeSpan _computationTime;

        public ConsolePrinter(DataTable table, TimeSpan computationTime) 
            : base(table)
        {
            _computationTime = computationTime;
        }

        public override void Print()
        {
            ConsoleTableBuilder
                .From(Table)
                .WithFormat(ConsoleTableBuilderFormat.Minimal)
                .ExportAndWrite();

            System.Console.WriteLine();
            System.Console.WriteLine("SUMMARY:");
            System.Console.WriteLine($"Computation time: {_computationTime}");

            System.Console.ReadKey();
        }
    }

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
                    foreach(var item in row.ItemArray)
                        csv.WriteField(item);
                    csv.NextRecord();
                }

                csv.WriteField(Table.Rows[Table.Rows.Count - 1].ItemArray);
                csv.NextRecord();
            }
        }
    }
}
