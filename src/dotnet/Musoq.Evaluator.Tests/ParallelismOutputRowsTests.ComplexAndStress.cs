using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ParallelismOutputRowsTests
{
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

        var parallelIds = tableParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds,
            "Result sets differ between parallel and non-parallel execution");
    }

    [TestMethod]
    public void OrderBy_WithParallelization_ShouldReturnAllRows()
    {
        const int rowCount = 3000;
        const string query = "select Name, Id from #A.Entities() order by Id desc";

        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(rowCount, table.Count, $"Expected {rowCount} rows but got {table.Count}");


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

        var parallelIds = tableParallel.Select(row => (int)row[1]).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds,
            "Result sets differ between parallel and non-parallel execution");
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
            { "#A", entities }
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

        var parallelCities = tableParallel.Select(row => (string)row[0]).OrderBy(x => x).ToList();
        var nonParallelCities = tableNonParallel.Select(row => (string)row[0]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelCities, parallelCities,
            "Result sets differ between parallel and non-parallel execution");
    }

    #endregion

}
