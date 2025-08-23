using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PivotSyntaxDebugTest : BasicEntityTestBase
{
    [TestMethod]
    public void Debug_PivotSyntaxGeneration()
    {
        var query = @"SELECT *
            FROM #A.entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics')
            ) AS p";
            
        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity("Books", "Book1", 10, 100m),
                    new SalesEntity("Electronics", "Phone", 5, 300m)
                ]
            }
        };

        try
        {
            var vm = InstanceCreator.CompileForExecution(
                query, 
                Guid.NewGuid().ToString(), 
                new SalesSchemaProvider<SalesEntity>(sources),
                LoggerResolver);
            var table = vm.Run();
            
            Assert.IsNotNull(table, "Table should not be null");
            Console.WriteLine($"Test passed - got {table.Count} rows");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }
}