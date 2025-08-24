using System;
using System.Collections.Generic;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace DebugPivot
{
    class Program
    {
        static void Main()
        {
            var query = @"SELECT p.Product, p.Books, p.Electronics 
                         FROM #A.entities() 
                         PIVOT (
                             Sum(Quantity)
                             FOR Category IN ('Books', 'Electronics')
                         ) AS p";
            
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

            Console.WriteLine("=== GENERATED C# CODE ===");
            
            var compiledQuery = InstanceCreator.CompileForExecution(
                query, 
                Guid.NewGuid().ToString(), 
                new SalesSchemaProvider<SalesEntity>(sources),
                null);
                
            Console.WriteLine("=== COMPILATION SUCCESSFUL ===");
            
            try
            {
                var result = compiledQuery.Run();
                Console.WriteLine($"Result count: {result.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Runtime error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}