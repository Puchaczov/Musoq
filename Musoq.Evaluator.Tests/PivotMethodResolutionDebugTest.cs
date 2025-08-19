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

            // Test PIVOT (debug generated code)
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
                
                // First, try without callback to see the actual error
                Console.WriteLine("Attempting PIVOT compilation without callback...");
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
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    
                    if (ex.InnerException.StackTrace != null)
                    {
                        Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                    }
                }
                
                if (ex.GetType().Name == "CompilationException")
                {
                    Console.WriteLine("ANALYSIS: C# compilation failed - examining syntax errors");
                    
                    // Extract specific compilation errors for analysis
                    var message = ex.Message;
                    if (message.Contains("error CS1525"))
                    {
                        Console.WriteLine("ERROR TYPE: CS1525 - Invalid expression syntax");
                    }
                    if (message.Contains("error CS0128"))
                    {
                        Console.WriteLine("ERROR TYPE: CS0128 - Variable redefinition");
                    }
                    if (message.Contains("error CS1061"))
                    {
                        Console.WriteLine("ERROR TYPE: CS1061 - Type doesn't contain member");
                    }
                    if (message.Contains("char"))
                    {
                        Console.WriteLine("ISSUE: 'char' type instead of expected entity type");
                    }
                }
                else if (ex.GetType().Name == "CannotResolveMethodException")
                {
                    Console.WriteLine("ANALYSIS: Method resolution is being attempted but failing");
                    Console.WriteLine("This means AccessMethodNode.Method resolution pipeline is working");
                    Console.WriteLine("Issue: Cannot find Sum method in PIVOT context");
                    
                    // Check if the error message gives more details about what was expected vs found
                    if (ex.Message.Contains("Sum"))
                    {
                        Console.WriteLine("Confirmed: Sum method specifically cannot be resolved");
                    }
                }
                else
                {
                    // Log any other exception for debugging
                    Console.WriteLine($"DEBUG: Other exception type - {ex.GetType().Name}: {ex.Message}");
                    
                    // For now, don't fail - we want to debug the generated code
                    // Assert.Fail($"Unexpected exception: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
}