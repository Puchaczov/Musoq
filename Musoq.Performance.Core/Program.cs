using System;
using System.Diagnostics;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema;
using Environment = Musoq.Plugins.Environment;

namespace Musoq.Performance.Core
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());

            for (int i = 0; i < 1000; i++)
            {
                CompileQuery(@"select Extension, Count(Extension) from #disk.files('C:\Users\jpuchala\Documents', 'true') group by Extension");
            }

            //ExecuteQuery(
            //    @"select
            //    AgencyName,
            //    Count(AgencyName),
            //    Sum(ToDecimal(Amount))
            //from #csv.file('C:\Users\Puchacz\Downloads\cards\res_purchase_card_(pcard)_fiscal_year_2014_3pcd-aiuu.csv', ',')
            //group by AgencyName");

            Console.WriteLine();
            Console.WriteLine("Press any key to close.");
            Console.ReadKey();
        }

        private static void ExecuteQuery(string query)
        {
            var watch = new Stopwatch();

            watch.Start();
            var vm = InstanceCreator.CompileForExecution(query, CreateCsvSchema());
            var compiledTime = watch.Elapsed;
            var table = vm.Run();
            watch.Stop();
            var executionTime = watch.Elapsed;

            Console.WriteLine($"Table {table.Name} contains {table.Count} rows.");
            Console.WriteLine($"Query compiled in {compiledTime}");
            Console.WriteLine($"Query prcessed in {executionTime - compiledTime}");
        }

        private static void CompileQuery(string query)
        {
            var watch = new Stopwatch();
            watch.Start();
            InstanceCreator.CompileForExecution(query, CreateDiskSchema());
            var compiledTime = watch.Elapsed;
            watch.Stop();

            Console.WriteLine($"Query compiled in {compiledTime}");
        }

        private static ISchemaProvider CreateCsvSchema()
        {
            return new CsvSchemaProvider();
        }

        private static ISchemaProvider CreateDiskSchema()
        {
            return new DiskSchemaProvider();
        }
    }
}