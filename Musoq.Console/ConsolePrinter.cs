using System;
using System.Data;
using ConsoleTableExt;

namespace Musoq.Console
{
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
}