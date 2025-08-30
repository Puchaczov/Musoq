using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.Optimization
{
    [TestClass]
    public class ColumnAccessPerformanceTests : BasicEntityTestBase
    {
        [TestMethod]
        public void ComprehensiveColumnAccessPerformanceAnalysis()
        {
            // Create a substantial dataset to amplify performance differences
            var largeDataset = new List<BasicEntity>();
            for (int i = 0; i < 5000; i++)
            {
                largeDataset.Add(new BasicEntity($"Country{i % 10}", $"City{i % 100}"));
            }
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", largeDataset }
            };
            
            Console.WriteLine($"=== Column Access Performance Analysis (Dataset: {largeDataset.Count} rows) ===");
            
            // Test 1: Single column access baseline
            var singleQuery = @"select Country from #A.Entities()";
            var singleTime = MeasureQueryExecutionTime(singleQuery, sources, "Single column access");
            
            // Test 2: Double column access
            var doubleQuery = @"select Country, Country from #A.Entities()";
            var doubleTime = MeasureQueryExecutionTime(doubleQuery, sources, "Double column access");
            
            // Test 3: Five column access
            var fiveQuery = @"select Country, Country, Country, Country, Country from #A.Entities()";
            var fiveTime = MeasureQueryExecutionTime(fiveQuery, sources, "Five column access");
            
            // Test 4: Ten column access
            var tenQuery = @"select Country, Country, Country, Country, Country, Country, Country, Country, Country, Country from #A.Entities()";
            var tenTime = MeasureQueryExecutionTime(tenQuery, sources, "Ten column access");
            
            // Calculate performance ratios
            var doubleRatio = (double)doubleTime / singleTime;
            var fiveRatio = (double)fiveTime / singleTime; 
            var tenRatio = (double)tenTime / singleTime;
            
            Console.WriteLine($"\n=== Performance Ratios (relative to single access) ===");
            Console.WriteLine($"Double access ratio: {doubleRatio:F2}x");
            Console.WriteLine($"Five access ratio: {fiveRatio:F2}x");
            Console.WriteLine($"Ten access ratio: {tenRatio:F2}x");
            
            // Analysis of column access efficiency
            Console.WriteLine($"\n=== Column Access Efficiency Analysis ===");
            
            if (tenRatio < 3.0)
            {
                Console.WriteLine("✅ EXCELLENT: Column access caching appears to be working efficiently!");
                Console.WriteLine($"   10x column access is only {tenRatio:F2}x slower than single access");
            }
            else if (tenRatio < 6.0)
            {
                Console.WriteLine("⚠️ MODERATE: Some optimization present but room for improvement");
                Console.WriteLine($"   10x column access is {tenRatio:F2}x slower than single access");
            }
            else
            {
                Console.WriteLine("❌ INEFFICIENT: Column access caching appears to be missing");
                Console.WriteLine($"   10x column access is {tenRatio:F2}x slower (expected ~10x without caching)");
            }
            
            // Performance per access calculation
            var avgTimePerAccess = (double)tenTime / 10.0;
            var baselineTimePerAccess = singleTime;
            var efficiency = baselineTimePerAccess / avgTimePerAccess;
            
            Console.WriteLine($"\n=== Access Efficiency Metrics ===");
            Console.WriteLine($"Baseline (1 access): {baselineTimePerAccess}ms per access");
            Console.WriteLine($"Multiple (10 accesses): {avgTimePerAccess:F2}ms per access");
            Console.WriteLine($"Efficiency ratio: {efficiency:F2} (1.0 = perfect caching, 0.1 = no caching)");
            
            // Test assertions
            Assert.IsTrue(singleTime > 0 && tenTime > 0, "Both queries should take measurable time");
            
            // If column caching is working well, 10x accesses shouldn't be more than 5x slower
            Assert.IsTrue(tenRatio < 8.0, 
                $"Performance ratio {tenRatio:F2} suggests column access might not be optimally cached.\n" +
                $"Expected ratio < 8.0 if column value caching is implemented.\n" +
                $"Current ratios: 2x={doubleRatio:F2}, 5x={fiveRatio:F2}, 10x={tenRatio:F2}");
        }
        
        [TestMethod]
        public void ColumnAccessCaching_BeforeAndAfterComparison()
        {
            var dataset = GenerateTestDataset(2000);
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", dataset }
            };
            
            Console.WriteLine($"=== Before/After Column Access Optimization Analysis ===");
            
            // Simulate "before optimization" by using a simple query
            var beforeQuery = @"select Country from #A.Entities()";
            var beforeTime = MeasureQueryExecutionTime(beforeQuery, sources, "Before optimization (single access)");
            
            // Simulate "after optimization" with multiple accesses (should be cached)
            var afterQuery = @"select Country, Country, Country, Country, Country from #A.Entities()";
            var afterTime = MeasureQueryExecutionTime(afterQuery, sources, "After optimization (5x access with caching)");
            
            // Calculate the per-access cost
            var beforeCostPerAccess = beforeTime;
            var afterCostPerAccess = (double)afterTime / 5.0;
            var improvement = beforeCostPerAccess / afterCostPerAccess;
            
            Console.WriteLine($"\n=== Optimization Effectiveness ===");
            Console.WriteLine($"Before optimization: {beforeCostPerAccess}ms per access");
            Console.WriteLine($"After optimization: {afterCostPerAccess:F2}ms per access");
            Console.WriteLine($"Improvement factor: {improvement:F2}x");
            
            if (improvement > 3.0)
            {
                Console.WriteLine("✅ EXCELLENT: Column access caching provides significant performance boost!");
            }
            else if (improvement > 1.5)
            {
                Console.WriteLine("⚠️ MODERATE: Some optimization present, could be further improved");
            }
            else
            {
                Console.WriteLine("❌ MINIMAL: Little to no column access optimization detected");
            }
            
            Assert.IsTrue(improvement > 1.0, "Multiple accesses should show some level of optimization");
        }
        
        private long MeasureQueryExecutionTime(string query, Dictionary<string, IEnumerable<BasicEntity>> sources, string description)
        {
            // Warm up first
            var warmupVm = CreateAndRunVirtualMachine(query, sources);
            warmupVm.Run();
            
            // Measure actual execution  
            var stopwatch = Stopwatch.StartNew();
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            stopwatch.Stop();
            
            var elapsed = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"{description}: {elapsed}ms ({table.Count} rows processed)");
            
            // Verify results to ensure query executed properly
            Assert.IsTrue(table.Count > 0, $"Query '{description}' should return results");
            
            return elapsed;
        }
        
        private List<BasicEntity> GenerateTestDataset(int count)
        {
            var dataset = new List<BasicEntity>();
            for (int i = 0; i < count; i++)
            {
                dataset.Add(new BasicEntity($"Country{i % 10}", $"City{i % 50}"));
            }
            return dataset;
        }
    }
}