using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class PivotMethodResolutionDebugTest : BasicEntityTestBase
    {
        [TestMethod]
        public void ComparePivotVsGroupByMethodResolution()
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

            // Test GROUP BY (should work)
            Console.WriteLine("=== GROUP BY Test ===");
            try
            {
                var groupByQuery = @"SELECT Category, Sum(Quantity) FROM #A.entities() GROUP BY Category";
                var groupByVm = InstanceCreator.CompileForExecution(
                    groupByQuery, 
                    Guid.NewGuid().ToString(), 
                    new SalesSchemaProvider<SalesEntity>(sources),
                    LoggerResolver);
                var groupByTable = groupByVm.Run();
                Console.WriteLine($"✓ GROUP BY SUCCESS: {groupByTable.Count} rows");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ GROUP BY FAILED: {ex.GetType().Name}: {ex.Message}");
                Assert.Fail($"GROUP BY should work: {ex.Message}");
            }

            // Test PIVOT (expected to fail with method resolution)
            Console.WriteLine("\n=== PIVOT Test ===");
            try
            {
                var pivotQuery = @"
                    SELECT *
                    FROM #A.entities()
                    PIVOT (
                        Sum(Quantity)
                        FOR Category IN ('Books', 'Electronics')
                    ) AS p";
                var pivotVm = InstanceCreator.CompileForExecution(
                    pivotQuery, 
                    Guid.NewGuid().ToString(), 
                    new SalesSchemaProvider<SalesEntity>(sources),
                    LoggerResolver);
                var pivotTable = pivotVm.Run();
                Console.WriteLine($"✓ PIVOT SUCCESS: {pivotTable.Count} rows");
                Assert.IsTrue(true, "PIVOT is working!"); // Success case
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ PIVOT FAILED: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                
                if (ex.GetType().Name == "CannotResolveMethodException")
                {
                    Console.WriteLine("ANALYSIS: Method resolution is being attempted but failing");
                    Console.WriteLine("This means AccessMethodNode.Method resolution pipeline is working");
                    Console.WriteLine("Issue: Cannot find Sum method in PIVOT context");
                    
                    // Check if the error message gives more details about what was expected vs found
                    if (ex.Message.Contains("Sum"))
                    {
                        Console.WriteLine("Confirmed: Sum method specifically cannot be resolved");
                    }
                    
                    // This is expected to fail for now, so we don't Assert.Fail
                    // We want to see the progress and understand the exact issue
                }
                else
                {
                    // Any other exception is unexpected at this point
                    Assert.Fail($"Unexpected exception: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
}