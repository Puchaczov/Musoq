using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class PivotQueryInfoDebugTest : BasicEntityTestBase
    {
        protected CompiledQuery CreateAndRunVirtualMachine<T>(
            string script,
            IDictionary<string, IEnumerable<T>> sources)
            where T : SalesEntity
        {
            return InstanceCreator.CompileForExecution(
                script, 
                Guid.NewGuid().ToString(), 
                new SalesSchemaProvider<T>(sources),
                LoggerResolver);
        }

        [TestMethod]
        public void DebugQueryInfoContents()
        {
            var sources = new Dictionary<string, IEnumerable<SalesEntity>>
            {
                {
                    "#A", new[]
                    {
                        new SalesEntity("Books", "Book1", 10, 100m),
                        new SalesEntity("Electronics", "Phone", 3, 300m)
                    }
                }
            };

            Console.WriteLine("=== Debugging QueryInfo Contents ===");
            
            // Test 1: Working GROUP BY for comparison
            Console.WriteLine("\n--- GROUP BY QueryInfo (Should Work) ---");
            try
            {
                var groupByQuery = @"SELECT Category, Sum(Quantity) FROM #A.entities() GROUP BY Category";
                var groupByVm = CreateAndRunVirtualMachine(groupByQuery, sources);
                
                // Access the QueriesInformation property through reflection
                var queriesInfo = groupByVm.GetType().GetProperty("QueriesInformation")?.GetValue(groupByVm);
                if (queriesInfo is System.Collections.IDictionary dict)
                {
                    Console.WriteLine($"GROUP BY QueriesInformation has {dict.Count} entries:");
                    foreach (var key in dict.Keys)
                    {
                        Console.WriteLine($"  Key: {key}");
                    }
                }
                
                var groupByTable = groupByVm.Run();
                Console.WriteLine($"✓ GROUP BY SUCCESS: {groupByTable.Count} rows returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ GROUP BY FAILED: {ex.Message}");
            }

            // Test 2: PIVOT query
            Console.WriteLine("\n--- PIVOT QueryInfo (Current Issue) ---");
            try
            {
                var pivotQuery = @"
                    SELECT *
                    FROM #A.entities()
                    PIVOT (
                        Sum(Quantity)
                        FOR Category IN ('Books', 'Electronics')
                    ) AS p";
                var pivotVm = CreateAndRunVirtualMachine(pivotQuery, sources);
                
                // Access the QueriesInformation property through reflection
                var queriesInfo = pivotVm.GetType().GetProperty("QueriesInformation")?.GetValue(pivotVm);
                if (queriesInfo is System.Collections.IDictionary dict)
                {
                    Console.WriteLine($"PIVOT QueriesInformation has {dict.Count} entries:");
                    foreach (var key in dict.Keys)
                    {
                        Console.WriteLine($"  Key: {key}");
                    }
                }
                
                Console.WriteLine("Attempting PIVOT execution...");
                var pivotTable = pivotVm.Run();
                Console.WriteLine($"✓ PIVOT SUCCESS: {pivotTable.Count} rows returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ PIVOT FAILED: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            }
        }
    }
}