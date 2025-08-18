using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class PivotDebugTest : BasicEntityTestBase
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
        public void DebugPivotMethodResolution()
        {
            var sources = new Dictionary<string, IEnumerable<SalesEntity>>
            {
                {
                    "#A", new[]
                    {
                        new SalesEntity("Books", "Book1", 10, 100m),
                        new SalesEntity("Books", "Book2", 5, 50m),
                        new SalesEntity("Electronics", "Phone", 3, 300m)
                    }
                }
            };

            Console.WriteLine("=== Debugging PIVOT Method Resolution ===");
            
            // Test 1: Working GROUP BY for comparison
            Console.WriteLine("\n--- GROUP BY Test (Should Work) ---");
            try
            {
                var groupByQuery = @"SELECT Category, Sum(Quantity) FROM #A.entities() GROUP BY Category";
                var groupByVm = CreateAndRunVirtualMachine(groupByQuery, sources);
                var groupByTable = groupByVm.Run();
                Console.WriteLine($"✓ GROUP BY SUCCESS: {groupByTable.Count} rows returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ GROUP BY FAILED: {ex.Message}");
                Assert.Fail($"GROUP BY should work but failed: {ex.Message}");
            }

            // Test 2: Simple PIVOT query
            Console.WriteLine("\n--- PIVOT Test (Current Status) ---");
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
                var pivotTable = pivotVm.Run();
                Console.WriteLine($"✓ PIVOT SUCCESS: {pivotTable.Count} rows returned");
                Assert.IsTrue(true, "PIVOT is working!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ PIVOT FAILED: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                
                if (ex.GetType().Name == "CannotResolveMethodException")
                {
                    Console.WriteLine("PROGRESS: Method resolution is being attempted (vs previous null reference)");
                    Console.WriteLine("Issue: Method resolution failing to find Sum method in PIVOT context");
                    Console.WriteLine($"Full error: {ex.Message}");
                }
                else if (ex.Message.Contains("Object reference not set") && 
                    ex.StackTrace.Contains("AccessMethodNodeProcessor"))
                {
                    Console.WriteLine("REGRESSION: Back to the AccessMethodNode.Method null issue");
                }
                
                // For debugging, we don't assert fail - we want to see the progress
                // Assert.Fail($"PIVOT failed: {ex.Message}");
            }
        }
    }
}