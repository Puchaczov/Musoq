using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace DebugTest
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== Testing Aliased Character Access ===");
            
            // Test data - same as FirstLetterOfColumnTest2
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new BasicEntity[]
                    {
                        new BasicEntity("12@hostname.com"),
                        new BasicEntity("ma@hostname.comcom"),
                        new BasicEntity("david.jones@proseware.com"),  // This should match f.Name[0] = 'd'
                        new BasicEntity("ma@hostname.com")
                    }
                }
            };
            
            // The failing query
            string query = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
            
            Console.WriteLine($"Query: {query}");
            Console.WriteLine("Expected: 1 row with 'david.jones@proseware.com'");
            
            try
            {
                // This would require setting up the full Musoq engine
                // For now, just output the analysis
                Console.WriteLine("\nAnalysis:");
                Console.WriteLine("- Query should find rows where first character of Name is 'd'");
                Console.WriteLine("- Only 'david.jones@proseware.com' matches this criteria");
                Console.WriteLine("- Current result: 0 rows (test fails)");
                Console.WriteLine("- Issue: WHERE clause f.Name[0] = 'd' not working correctly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}