using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ParallelismOutputRowsTests
{
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
            { "#A", entities }
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
            { "#A", entities }
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
        const string query =
            "select City, Count(City), Sum(Population), Avg(Population) from #A.Entities() group by City";

        var entities = cities.SelectMany(city =>
            Enumerable.Range(0, rowsPerCity).Select(i => new BasicEntity(city, "Country", i + 1))).ToList();

        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities.ToList() }
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities.ToList() }
        };

        var vmParallel =
            CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel,
            new CompilationOptions(ParallelizationMode.None));

        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count,
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");


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

    [TestMethod]
    public void GroupBy_LargeLowCardinalityInput_ShouldMatchSerial()
    {
        const int rowCount = 12288;
        const int cityCount = 8;
        const string query = "select City, Count(City), Sum(Population) from #A.Entities() group by City";

        var entities = CreateGroupedEntities(rowCount, cityCount);

        AssertBothModesReturnSameRows(query, entities);
    }

    [TestMethod]
    public void GroupBy_LargeInputWithNullKeys_ShouldMatchSerial()
    {
        const int rowCount = 12288;
        const int cityCount = 8;
        const int nullEvery = 17;
        const string query = "select City, Count(City), Sum(Population) from #A.Entities() group by City";

        var entities = CreateGroupedEntitiesWithNullKeys(rowCount, cityCount, nullEvery);

        AssertBothModesReturnSameRows(query, entities);
    }

    [TestMethod]
    public void GroupBy_LargeInputWithMultipleAggregates_ShouldMatchSerial()
    {
        const int rowCount = 12288;
        const int cityCount = 8;
        const string query = @"
            select
                City,
                Count(City),
                Sum(Population),
                Min(Population),
                Max(Population),
                Avg(Population)
            from #A.Entities()
            group by City";

        var entities = CreateGroupedEntities(rowCount, cityCount);

        AssertBothModesReturnSameRows(query, entities);
    }

    [TestMethod]
    public void GroupBy_LargeHighCardinalityInput_ShouldMatchSerial()
    {
        const int rowCount = 8192;
        const string query = "select City, Count(City), Sum(Population) from #A.Entities() group by City";

        var entities = CreateHighCardinalityGroupedEntities(rowCount);

        AssertBothModesReturnSameRows(query, entities);
    }

    [TestMethod]
    public void GroupBy_BelowParallelThreshold_ShouldMatchSerial()
    {
        const int rowCount = 4095;
        const int cityCount = 8;
        const string query = "select City, Count(City), Sum(Population) from #A.Entities() group by City";

        var entities = CreateGroupedEntities(rowCount, cityCount);

        AssertBothModesReturnSameRows(query, entities);
    }

    #endregion
}
