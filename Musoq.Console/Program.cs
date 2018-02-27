using System.Data;
using System.IO;
using System.Linq;
using CommandLine;
using Musoq.Service.Client;
using Musoq.Service.Client.Helpers;
using Musoq.Console.Helpers;

namespace Musoq.Console
{
    public class Program
    {
        static int Main(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<ApplicationArguments>(args);
            
            return CommandLine.Parser.Default.ParseArguments<ApplicationArguments>(args)
                .MapResult(
                    ProcessArguments,
                    _ => 1);
        }

        private static int ProcessArguments(ApplicationArguments appArgs)
        {
            var query = string.IsNullOrEmpty(appArgs.QuerySourceFile)
                ? appArgs.Query
                : File.ReadAllText(appArgs.QuerySourceFile);

            var api = new ApplicationFlowApi(string.IsNullOrEmpty(appArgs.Address) ? Configuration.Address : appArgs.Address);

            var result = api.RunQueryAsync(new QueryContext
            {
                Query = query
            }).Result;

            var dt = new DataTable(result.Name);

            dt.Columns.AddRange(result.Columns.Select(f => new DataColumn(f)).ToArray());

            foreach (var item in result.Rows)
                dt.Rows.Add(item);

            Printer printer;

            if (string.IsNullOrEmpty(appArgs.QueryScoreFile))
                printer = new ConsolePrinter(dt, result.ComputationTime);
            else
                printer = new CsvPrinter(dt, appArgs.QueryScoreFile);

            printer.Print();

            return 0;
        }
    }
}
