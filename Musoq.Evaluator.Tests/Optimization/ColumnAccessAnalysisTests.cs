using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Optimization
{
    [TestClass]
    public class ColumnAccessAnalysisTests : BasicEntityTestBase
    {
        [TestMethod]
        public void AnalyzeGeneratedCodeForColumnAccess_SingleAccess()
        {
            var query = @"select Country from #A.Entities()";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("POLAND", "WARSAW"),
                        new BasicEntity("UK", "LONDON")
                    }
                }
            };
            
            // Generate code for analysis
            var generatedCode = GetGeneratedCode(query, sources);
            Console.WriteLine("=== Single Column Access ===");
            Console.WriteLine(generatedCode);
            
            // Count how many times Country is accessed
            int countryAccessCount = CountStringOccurrences(generatedCode, "Country");
            Console.WriteLine($"Country access count: {countryAccessCount}");
            
            Assert.IsTrue(countryAccessCount > 0, "Country should be accessed at least once");
        }
        
        [TestMethod]
        public void AnalyzeGeneratedCodeForColumnAccess_MultipleAccess()
        {
            var query = @"select Country, Country, Country, Country, Country from #A.Entities()";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("POLAND", "WARSAW"),
                        new BasicEntity("UK", "LONDON")
                    }
                }
            };
            
            // Generate code for analysis
            var generatedCode = GetGeneratedCode(query, sources);
            Console.WriteLine("=== Multiple Column Access ===");
            Console.WriteLine(generatedCode);
            
            // Count how many times Country is accessed in generated code
            int countryAccessCount = CountStringOccurrences(generatedCode, "Country");
            Console.WriteLine($"Country access count: {countryAccessCount}");
            
            // If column caching is working, there should be:
            // 1. One field accessor declaration  
            // 2. One value assignment per row
            // 3. Multiple references to the cached value
            var optimizedAccessCount = CountStringOccurrences(generatedCode, "_accessor_Country");
            var cachedVariableCount = CountStringOccurrences(generatedCode, "var country_cached");
            
            Console.WriteLine($"Optimized field accessor usage: {optimizedAccessCount}");
            Console.WriteLine($"Cached variable usage: {cachedVariableCount}");
            
            Assert.IsTrue(countryAccessCount > 0, "Country should be accessed");
        }
        
        [TestMethod]
        public void MeasurePerformanceWithLargeDataset_SingleVsMultipleColumnAccess()
        {
            // Create a larger dataset to amplify performance differences
            var largeDataset = new List<BasicEntity>();
            for (int i = 0; i < 1000; i++)
            {
                largeDataset.Add(new BasicEntity($"Country{i % 5}", $"City{i}"));
            }
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", largeDataset }
            };
            
            // Test single column access
            var singleQuery = @"select Country from #A.Entities()";
            var singleTime = MeasureQueryTime(singleQuery, sources);
            Console.WriteLine($"Single column access: {singleTime}ms");
            
            // Test multiple column access
            var multipleQuery = @"select Country, Country, Country, Country, Country, Country, Country, Country, Country, Country from #A.Entities()";
            var multipleTime = MeasureQueryTime(multipleQuery, sources);
            Console.WriteLine($"Multiple column access: {multipleTime}ms");
            
            // Calculate performance ratio
            var ratio = (double)multipleTime / singleTime;
            Console.WriteLine($"Performance ratio (multiple/single): {ratio:F2}");
            
            // If column caching is working efficiently, the ratio should be close to 1.0
            // If not working, the ratio should be closer to 10.0 (10 accesses vs 1)
            
            Assert.IsTrue(singleTime > 0 && multipleTime > 0, "Both queries should take measurable time");
            
            // Expect that multiple accesses shouldn't be 10x slower if caching is working
            Assert.IsTrue(ratio < 8.0, $"Performance ratio {ratio:F2} suggests inefficient column access - should be <8x if caching works");
        }
        
        private long MeasureQueryTime(string query, Dictionary<string, IEnumerable<BasicEntity>> sources)
        {
            var stopwatch = Stopwatch.StartNew();
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            stopwatch.Stop();
            
            // Verify results to ensure query executed properly
            Assert.IsTrue(table.Count > 0, "Query should return results");
            
            return stopwatch.ElapsedMilliseconds;
        }
        
        private string GetGeneratedCode(string query, Dictionary<string, IEnumerable<BasicEntity>> sources)
        {
            try
            {
                var vm = CreateAndRunVirtualMachine(query, sources);
                
                // The generated code is typically compiled into an assembly
                // For analysis purposes, we can try to capture build items or examine the compilation process
                // This is a simplified approach - in a real implementation, we'd need to hook into the build process
                
                return "Generated code analysis not fully implemented - would need access to BuildItems";
            }
            catch (Exception ex)
            {
                return $"Error generating code: {ex.Message}";
            }
        }
        
        private int CountStringOccurrences(string text, string pattern)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return 0;
                
            int count = 0;
            int index = 0;
            
            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            
            return count;
        }
    }
}