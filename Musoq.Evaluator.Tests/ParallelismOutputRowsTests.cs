using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Comprehensive tests to verify that parallelism doesn't lose output rows.
/// Tests with large input datasets should produce correct amount of output rows.
/// Tests verify both the count of rows and the correctness of row values.
/// </summary>
[TestClass]
public class ParallelismOutputRowsTests
{
    private static ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    static ParallelismOutputRowsTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    #region Simple SELECT Tests

    [TestMethod]
    public void SimpleSelect_WithParallelization_ShouldReturnAllRows()
    {
        const int rowCount = 5000;
        const string query = "select Name, Id from #A.Entities()";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(rowCount, table.Count, $"Expected {rowCount} rows but got {table.Count}");
        
        // Verify all IDs are present
        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var expectedIds = Enumerable.Range(0, rowCount).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void SimpleSelect_WithoutParallelization_ShouldReturnAllRows()
    {
        const int rowCount = 5000;
        const string query = "select Name, Id from #A.Entities()";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.None));
        var table = vm.Run();

        Assert.AreEqual(rowCount, table.Count, $"Expected {rowCount} rows but got {table.Count}");
        
        // Verify all IDs are present
        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var expectedIds = Enumerable.Range(0, rowCount).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void SimpleSelect_BothModes_ShouldReturnSameResults()
    {
        const int rowCount = 3000;
        const string query = "select Name, Id from #A.Entities()";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };

        var vmParallel = CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelIds = tableParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds, "Result sets differ between parallel and non-parallel execution");
    }

    #endregion

    #region WHERE Clause Tests

    [TestMethod]
    public void WhereClause_WithParallelization_ShouldReturnCorrectFilteredRows()
    {
        const int totalRows = 5000;
        const string query = "select Name, Id from #A.Entities() where Id >= 2500";
        
        var entities = CreateBasicEntitiesWithIds(totalRows);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(2500, table.Count, $"Expected 2500 rows but got {table.Count}");
        
        // Verify all returned IDs are >= 2500
        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        Assert.IsTrue(resultIds.All(id => id >= 2500), "Some rows have Id < 2500");
        
        var expectedIds = Enumerable.Range(2500, 2500).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void WhereClause_WithoutParallelization_ShouldReturnCorrectFilteredRows()
    {
        const int totalRows = 5000;
        const string query = "select Name, Id from #A.Entities() where Id >= 2500";
        
        var entities = CreateBasicEntitiesWithIds(totalRows);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.None));
        var table = vm.Run();

        Assert.AreEqual(2500, table.Count, $"Expected 2500 rows but got {table.Count}");
        
        // Verify all returned IDs are >= 2500
        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        Assert.IsTrue(resultIds.All(id => id >= 2500), "Some rows have Id < 2500");
        
        var expectedIds = Enumerable.Range(2500, 2500).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void WhereClause_BothModes_ShouldReturnSameResults()
    {
        const int totalRows = 3000;
        const string query = "select Name, Id from #A.Entities() where Id % 3 = 0";
        
        var entities = CreateBasicEntitiesWithIds(totalRows);
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };

        var vmParallel = CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelIds = tableParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds, "Result sets differ between parallel and non-parallel execution");
    }

    [TestMethod]
    public void WhereClause_StringFilter_WithParallelization_ShouldReturnCorrectRows()
    {
        const int totalRows = 2000;
        const string query = "select Name, Id from #A.Entities() where Name like '%500%'";
        
        var entities = CreateBasicEntitiesWithIds(totalRows);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        // Count how many names contain "500"
        var expectedCount = entities.Count(e => e.Name.Contains("500"));
        Assert.AreEqual(expectedCount, table.Count, $"Expected {expectedCount} rows but got {table.Count}");
    }

    #endregion

    #region Aggregation Tests

    [TestMethod]
    public void GroupBy_WithParallelization_ShouldReturnCorrectAggregates()
    {
        const int rowsPerCity = 1000;
        var cities = new[] { "CityA", "CityB", "CityC", "CityD", "CityE" };
        const string query = "select City, Count(City), Sum(Population) from #A.Entities() group by City";
        
        var entities = cities.SelectMany(city => 
            Enumerable.Range(0, rowsPerCity).Select(i => new BasicEntity(city, "Country", i + 1))).ToList();
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(cities.Length, table.Count, $"Expected {cities.Length} groups but got {table.Count}");

        foreach (var row in table)
        {
            var city = (string)row[0];
            var count = Convert.ToInt32(row[1]);
            var sum = Convert.ToDecimal(row[2]);
            
            Assert.AreEqual(rowsPerCity, count, $"City {city} should have {rowsPerCity} rows but got {count}");
            
            var expectedSum = Enumerable.Range(1, rowsPerCity).Sum();
            Assert.AreEqual(expectedSum, sum, $"City {city} sum should be {expectedSum} but got {sum}");
        }
    }

    [TestMethod]
    public void GroupBy_WithoutParallelization_ShouldReturnCorrectAggregates()
    {
        const int rowsPerCity = 1000;
        var cities = new[] { "CityA", "CityB", "CityC", "CityD", "CityE" };
        const string query = "select City, Count(City), Sum(Population) from #A.Entities() group by City";
        
        var entities = cities.SelectMany(city => 
            Enumerable.Range(0, rowsPerCity).Select(i => new BasicEntity(city, "Country", i + 1))).ToList();
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.None));
        var table = vm.Run();

        Assert.AreEqual(cities.Length, table.Count, $"Expected {cities.Length} groups but got {table.Count}");

        foreach (var row in table)
        {
            var city = (string)row[0];
            var count = Convert.ToInt32(row[1]);
            var sum = Convert.ToDecimal(row[2]);
            
            Assert.AreEqual(rowsPerCity, count, $"City {city} should have {rowsPerCity} rows but got {count}");
            
            var expectedSum = Enumerable.Range(1, rowsPerCity).Sum();
            Assert.AreEqual(expectedSum, sum, $"City {city} sum should be {expectedSum} but got {sum}");
        }
    }

    [TestMethod]
    public void GroupBy_BothModes_ShouldReturnSameResults()
    {
        const int rowsPerCity = 500;
        var cities = new[] { "CityA", "CityB", "CityC", "CityD", "CityE", "CityF", "CityG", "CityH" };
        const string query = "select City, Count(City), Sum(Population), Avg(Population) from #A.Entities() group by City";
        
        var entities = cities.SelectMany(city => 
            Enumerable.Range(0, rowsPerCity).Select(i => new BasicEntity(city, "Country", i + 1))).ToList();
        
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };

        var vmParallel = CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        // Convert to dictionaries for comparison
        var parallelResults = tableParallel.ToDictionary(row => (string)row[0], row => (
            Count: Convert.ToInt32(row[1]), 
            Sum: Convert.ToDecimal(row[2]),
            Avg: Convert.ToDecimal(row[3])));
        var nonParallelResults = tableNonParallel.ToDictionary(row => (string)row[0], row => (
            Count: Convert.ToInt32(row[1]), 
            Sum: Convert.ToDecimal(row[2]),
            Avg: Convert.ToDecimal(row[3])));

        foreach (var city in cities)
        {
            Assert.IsTrue(parallelResults.ContainsKey(city), $"City {city} missing from parallel results");
            Assert.IsTrue(nonParallelResults.ContainsKey(city), $"City {city} missing from non-parallel results");
            
            Assert.AreEqual(nonParallelResults[city].Count, parallelResults[city].Count, 
                $"Count mismatch for {city}");
            Assert.AreEqual(nonParallelResults[city].Sum, parallelResults[city].Sum, 
                $"Sum mismatch for {city}");
            Assert.AreEqual(nonParallelResults[city].Avg, parallelResults[city].Avg, 
                $"Avg mismatch for {city}");
        }
    }

    #endregion

    #region Large Dataset Tests

    [TestMethod]
    public void LargeDataset_10000Rows_WithParallelization_ShouldReturnAllRows()
    {
        const int rowCount = 10000;
        const string query = "select Name, Id, City from #A.Entities()";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(rowCount, table.Count, $"Expected {rowCount} rows but got {table.Count}");
        
        // Verify all IDs are present and unique
        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        Assert.AreEqual(rowCount, resultIds.Distinct().Count(), "Duplicate IDs found in results");
        
        var expectedIds = Enumerable.Range(0, rowCount).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void LargeDataset_10000Rows_BothModes_ShouldReturnSameResults()
    {
        const int rowCount = 10000;
        const string query = "select Name, Id from #A.Entities()";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };

        var vmParallel = CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelIds = tableParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds, "Result sets differ between parallel and non-parallel execution");
    }

    [TestMethod]
    public void LargeDataset_WithFilter_BothModes_ShouldReturnSameResults()
    {
        const int rowCount = 10000;
        const string query = "select Name, Id from #A.Entities() where Id % 7 = 0 or Id % 11 = 0";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };

        var vmParallel = CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelIds = tableParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds, "Result sets differ between parallel and non-parallel execution");
        
        // Verify correctness
        Assert.IsTrue(parallelIds.All(id => id % 7 == 0 || id % 11 == 0), "Filter not correctly applied");
    }

    #endregion

    #region Join Tests

    [TestMethod]
    public void InnerJoin_WithParallelization_ShouldReturnCorrectRows()
    {
        const int size = 2000;
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = Enumerable.Range(0, size).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, size).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();
        
        var vm = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();
        
        Assert.AreEqual(size, table.Count, $"Expected {size} rows but got {table.Count}");
        
        // Verify content
        var resultPairs = table.Select(row => ((string)row[0], (string)row[1])).OrderBy(x => int.Parse(x.Item1)).ToList();
        for (int i = 0; i < size; i++)
        {
            Assert.AreEqual(i.ToString(), resultPairs[i].Item1, $"First item mismatch at index {i}");
            Assert.AreEqual(i.ToString(), resultPairs[i].Item2, $"Second item mismatch at index {i}");
        }
    }

    [TestMethod]
    public void InnerJoin_WithoutParallelization_ShouldReturnCorrectRows()
    {
        const int size = 2000;
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = Enumerable.Range(0, size).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, size).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();
        
        var vm = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.None));
        var table = vm.Run();
        
        Assert.AreEqual(size, table.Count, $"Expected {size} rows but got {table.Count}");
    }

    [TestMethod]
    public void InnerJoin_BothModes_ShouldReturnSameResults()
    {
        const int size = 3000;
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = Enumerable.Range(0, size).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, size).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();
        
        var vmParallel = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();
        
        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelItems = tableParallel.Select(row => (string)row[0]).OrderBy(x => int.Parse(x)).ToList();
        var nonParallelItems = tableNonParallel.Select(row => (string)row[0]).OrderBy(x => int.Parse(x)).ToList();
        CollectionAssert.AreEqual(nonParallelItems, parallelItems, "Result sets differ between parallel and non-parallel execution");
    }

    [TestMethod]
    public void LeftOuterJoin_WithParallelization_ShouldReturnCorrectRows()
    {
        const int leftSize = 2000;
        const int rightSize = 1000;
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first left outer join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = Enumerable.Range(0, leftSize).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, rightSize).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();
        
        var vm = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();
        
        Assert.AreEqual(leftSize, table.Count, $"Expected {leftSize} rows but got {table.Count}");
        
        // Verify matched and unmatched rows
        var matchedCount = table.Count(row => row[1] != null);
        var unmatchedCount = table.Count(row => row[1] == null);
        
        Assert.AreEqual(rightSize, matchedCount, $"Expected {rightSize} matched rows but got {matchedCount}");
        Assert.AreEqual(leftSize - rightSize, unmatchedCount, $"Expected {leftSize - rightSize} unmatched rows but got {unmatchedCount}");
    }

    [TestMethod]
    public void LeftOuterJoin_BothModes_ShouldReturnSameResults()
    {
        const int leftSize = 2000;
        const int rightSize = 1000;
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first left outer join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = Enumerable.Range(0, leftSize).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, rightSize).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();
        
        var vmParallel = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();
        
        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");
    }

    #endregion

    #region Complex Query Tests

    [TestMethod]
    public void ComplexQuery_MultipleConditions_BothModes_ShouldReturnSameResults()
    {
        const int rowCount = 5000;
        const string query = @"
            select 
                Name, 
                Id, 
                City, 
                Population 
            from #A.Entities() 
            where 
                (Id >= 1000 and Id < 3000) 
                or Population > 2000";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };

        var vmParallel = CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelIds = tableParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds, "Result sets differ between parallel and non-parallel execution");
    }

    [TestMethod]
    public void OrderBy_WithParallelization_ShouldReturnAllRows()
    {
        const int rowCount = 3000;
        const string query = "select Name, Id from #A.Entities() order by Id desc";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(rowCount, table.Count, $"Expected {rowCount} rows but got {table.Count}");
        
        // Verify order
        var resultIds = table.Select(row => (int)row[1]).ToList();
        var expectedIds = Enumerable.Range(0, rowCount).Reverse().ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Order is incorrect");
    }

    [TestMethod]
    public void OrderBy_BothModes_ShouldReturnSameResults()
    {
        const int rowCount = 3000;
        const string query = "select Name, Id from #A.Entities() order by Id asc";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };

        var vmParallel = CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelIds = tableParallel.Select(row => (int)row[1]).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds, "Result sets differ between parallel and non-parallel execution");
    }

    [TestMethod]
    public void Distinct_WithParallelization_ShouldReturnCorrectRows()
    {
        const int distinctValues = 100;
        const int duplicatesPerValue = 50;
        const string query = "select distinct City from #A.Entities()";
        
        var entities = Enumerable.Range(0, distinctValues)
            .SelectMany(i => Enumerable.Range(0, duplicatesPerValue)
                .Select(_ => new BasicEntity { City = $"City_{i:D3}" }))
            .ToList();
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities}
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(distinctValues, table.Count, $"Expected {distinctValues} distinct rows but got {table.Count}");
    }

    [TestMethod]
    public void Distinct_BothModes_ShouldReturnSameResults()
    {
        const int distinctValues = 100;
        const int duplicatesPerValue = 50;
        const string query = "select distinct City from #A.Entities()";
        
        var entities = Enumerable.Range(0, distinctValues)
            .SelectMany(i => Enumerable.Range(0, duplicatesPerValue)
                .Select(_ => new BasicEntity { City = $"City_{i:D3}" }))
            .ToList();
        
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", entities.ToList()}
        };

        var vmParallel = CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel, new CompilationOptions(ParallelizationMode.None));
        
        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count, 
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelCities = tableParallel.Select(row => (string)row[0]).OrderBy(x => x).ToList();
        var nonParallelCities = tableNonParallel.Select(row => (string)row[0]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelCities, parallelCities, "Result sets differ between parallel and non-parallel execution");
    }

    #endregion

    #region Stress Tests

    [TestMethod]
    public void StressTest_MultipleIterations_WithParallelization_ShouldAlwaysReturnSameRowCount()
    {
        const int rowCount = 2000;
        const int iterations = 10;
        const string query = "select Name, Id from #A.Entities()";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        
        for (int i = 0; i < iterations; i++)
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", entities.ToList()}
            };

            var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
            var table = vm.Run();

            Assert.AreEqual(rowCount, table.Count, $"Iteration {i}: Expected {rowCount} rows but got {table.Count}");
        }
    }

    [TestMethod]
    public void StressTest_MultipleIterations_WithFilter_ShouldAlwaysReturnConsistentResults()
    {
        const int rowCount = 2000;
        const int iterations = 10;
        const string query = "select Name, Id from #A.Entities() where Id % 5 = 0";
        
        var entities = CreateBasicEntitiesWithIds(rowCount);
        var expectedCount = entities.Count(e => e.Id % 5 == 0);
        
        for (int i = 0; i < iterations; i++)
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", entities.ToList()}
            };

            var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
            var table = vm.Run();

            Assert.AreEqual(expectedCount, table.Count, $"Iteration {i}: Expected {expectedCount} rows but got {table.Count}");
        }
    }

    #endregion

    #region Helper Methods

    private static List<BasicEntity> CreateBasicEntitiesWithIds(int count)
    {
        return Enumerable.Range(0, count).Select(i => new BasicEntity
        {
            Id = i,
            Name = $"Entity_{i:D5}",
            City = $"City_{i % 100}",
            Country = $"Country_{i % 10}",
            Population = i
        }).ToList();
    }

    private static CompiledQuery CreateVirtualMachineWithOptions(
        string script,
        IDictionary<string, IEnumerable<BasicEntity>> sources,
        CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            options);
    }

    private static CompiledQuery CreateJoinVirtualMachine(
        string script,
        FirstEntity[] first,
        SecondEntity[] second,
        CompilationOptions options)
    {
        var schema = new MultiSchema(new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>()
        {
            {"first", (new FirstEntityTable(), new MultiRowSource<FirstEntity>(first, FirstEntity.TestNameToIndexMap, FirstEntity.TestIndexToObjectAccessMap))},
            {"second", (new SecondEntityTable(), new MultiRowSource<SecondEntity>(second, SecondEntity.TestNameToIndexMap, SecondEntity.TestIndexToObjectAccessMap))}
        });
        
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new MultiSchemaProvider(new Dictionary<string, ISchema>()
            {
                {"#schema", schema}
            }),
            LoggerResolver,
            options);
    }

    #endregion
}
