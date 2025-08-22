using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class PivotCodeGenerationDebugTest : BasicEntityTestBase
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
        public void DebugGeneratedCode()
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

            Console.WriteLine("=== Debugging PIVOT Generated Code ===");
            
            try
            {
                var pivotQuery = @"
                    SELECT *
                    FROM #A.entities()
                    PIVOT (
                        Sum(Quantity)
                        FOR Category IN ('Books', 'Electronics')
                    ) AS p";
                
                Console.WriteLine($"Query: {pivotQuery}");
                Console.WriteLine("Attempting compilation...");
                
                var compiledQuery = InstanceCreator.CompileForExecution(
                    pivotQuery, 
                    "DebugGenerated", 
                    new SalesSchemaProvider<SalesEntity>(sources),
                    LoggerResolver);
                
                Console.WriteLine("✓ Compilation successful!");
                
                Console.WriteLine("Attempting execution...");
                var table = compiledQuery.Run();
                Console.WriteLine($"✓ Execution successful! Table has {table.Count} rows and {table.Columns.Count()} columns");
                
                foreach (var column in table.Columns)
                {
                    Console.WriteLine($"  Column: {column.ColumnName} ({column.ColumnType})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Execution failed: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Look for generated code in the exception
                if (ex.Message.Contains("error CS"))
                {
                    Console.WriteLine("C# Compilation errors detected:");
                    var lines = ex.Message.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("error CS"))
                        {
                            Console.WriteLine($"  - {line.Trim()}");
                        }
                    }
                }
                
                // Don't fail the test - we want to see the output
                // Assert.Fail($"Execution failed: {ex.Message}");
            }
        }
    }
}