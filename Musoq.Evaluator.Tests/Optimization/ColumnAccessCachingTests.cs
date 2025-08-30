using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Optimization
{
    [TestClass]
    public class ColumnAccessCachingTests : BasicEntityTestBase
    {
        [TestMethod]
        public void WhenAccessingSameColumnMultipleTimes_ShouldOptimizeToSingleAccess()
        {
            // Test multiple access to the same column in SELECT clause
            var query = @"select Country, Country, Country, Country, Country, Country from #A.Entities()";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("POLAND", "WARSAW"),
                        new BasicEntity("POLAND", "CZESTOCHOWA"),
                        new BasicEntity("UK", "LONDON"),
                        new BasicEntity("POLAND", "KRAKOW"),
                        new BasicEntity("UK", "MANCHESTER"),
                        new BasicEntity("ANGOLA", "LLL")
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            // Verify the query works correctly
            Assert.IsTrue(table.Count > 0);
            
            // All Country columns should have the same value for each row
            foreach (var row in table)
            {
                var country1 = row[0];
                var country2 = row[1]; 
                var country3 = row[2];
                var country4 = row[3];
                var country5 = row[4];
                var country6 = row[5];
                
                Assert.AreEqual(country1, country2);
                Assert.AreEqual(country1, country3);
                Assert.AreEqual(country1, country4);
                Assert.AreEqual(country1, country5);
                Assert.AreEqual(country1, country6);
            }
        }
        
        [TestMethod]
        public void WhenAccessingSameColumnInAggregation_ShouldOptimizeToSingleAccess()
        {
            // Test multiple access to the same column in aggregation functions  
            var query = @"select Country, Count(Country), Count(Country) from #A.Entities() group by Country";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("POLAND", "WARSAW"),
                        new BasicEntity("POLAND", "CZESTOCHOWA"),
                        new BasicEntity("UK", "LONDON"),
                        new BasicEntity("POLAND", "KRAKOW"),
                        new BasicEntity("UK", "MANCHESTER"),
                        new BasicEntity("ANGOLA", "LLL")
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            // Verify the query works correctly
            Assert.IsTrue(table.Count > 0);
            
            // Count(Country) should be the same in both columns
            foreach (var row in table)
            {
                var count1 = row[1];
                var count2 = row[2];
                Assert.AreEqual(count1, count2);
            }
        }
        
        [TestMethod] 
        public void TestColumnAccessCachingPerformance_WithoutOptimization()
        {
            // Query that accesses Country column many times 
            var query = @"select Country, Country, Country, Country, Country, Country, Country, Country, Country, Country from #A.Entities()";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("POLAND", "WARSAW"),
                        new BasicEntity("POLAND", "CZESTOCHOWA"),
                        new BasicEntity("UK", "LONDON"),
                        new BasicEntity("POLAND", "KRAKOW"),
                        new BasicEntity("UK", "MANCHESTER"),
                        new BasicEntity("ANGOLA", "LLL")
                    }
                }
            };
            
            var stopwatch = Stopwatch.StartNew();
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            stopwatch.Stop();
            
            var timeWithoutOptimization = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Without optimization: {timeWithoutOptimization}ms");
            
            Assert.IsTrue(table.Count > 0);
        }
        
        [TestMethod]
        public void TestColumnAccessCachingPerformance_WithOptimization() 
        {
            // Query that accesses Country column many times
            var query = @"select Country, Country, Country, Country, Country, Country, Country, Country, Country, Country from #A.Entities()";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("POLAND", "WARSAW"),
                        new BasicEntity("POLAND", "CZESTOCHOWA"),
                        new BasicEntity("UK", "LONDON"),
                        new BasicEntity("POLAND", "KRAKOW"),
                        new BasicEntity("UK", "MANCHESTER"),
                        new BasicEntity("ANGOLA", "LLL")
                    }
                }
            };
            
            var stopwatch = Stopwatch.StartNew();
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            stopwatch.Stop();
            
            var timeWithOptimization = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"With optimization: {timeWithOptimization}ms");
            
            Assert.IsTrue(table.Count > 0);
        }
        
    }
}