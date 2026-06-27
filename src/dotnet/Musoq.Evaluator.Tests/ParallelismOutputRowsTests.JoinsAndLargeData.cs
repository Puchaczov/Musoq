using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;

namespace Musoq.Evaluator.Tests;

public partial class ParallelismOutputRowsTests
{
    #region Large Dataset Tests

    [TestMethod]
    public void LargeDataset_10000Rows_WithParallelization_ShouldReturnAllRows()
    {
        const int rowCount = 10000;
        const string query = "select Name, Id, City from #A.Entities()";

        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(rowCount, table.Count, $"Expected {rowCount} rows but got {table.Count}");


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
    public void LargeDataset_WithFilter_BothModes_ShouldReturnSameResults()
    {
        const int rowCount = 10000;
        const string query = "select Name, Id from #A.Entities() where Id % 7 = 0 or Id % 11 = 0";

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


        Assert.IsTrue(parallelIds.All(id => id % 7 == 0 || id % 11 == 0), "Filter not correctly applied");
    }

    #endregion

    #region Join Tests

    [TestMethod]
    public void InnerJoin_WithParallelization_ShouldReturnCorrectRows()
    {
        const int size = 2000;
        const string query =
            "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem";

        var first = Enumerable.Range(0, size).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, size).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();

        var vm = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(size, table.Count, $"Expected {size} rows but got {table.Count}");


        var resultPairs = table.Select(row => ((string)row[0], (string)row[1])).OrderBy(x => int.Parse(x.Item1))
            .ToList();
        for (var i = 0; i < size; i++)
        {
            Assert.AreEqual(i.ToString(), resultPairs[i].Item1, $"First item mismatch at index {i}");
            Assert.AreEqual(i.ToString(), resultPairs[i].Item2, $"Second item mismatch at index {i}");
        }
    }

    [TestMethod]
    public void InnerJoin_WithoutParallelization_ShouldReturnCorrectRows()
    {
        const int size = 2000;
        const string query =
            "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem";

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
        const string query =
            "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem";

        var first = Enumerable.Range(0, size).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, size).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();

        var vmParallel =
            CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel =
            CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.None));

        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count,
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelItems = tableParallel.Select(row => (string)row[0]).OrderBy(int.Parse).ToList();
        var nonParallelItems = tableNonParallel.Select(row => (string)row[0]).OrderBy(int.Parse).ToList();
        CollectionAssert.AreEqual(nonParallelItems, parallelItems,
            "Result sets differ between parallel and non-parallel execution");
    }

    [TestMethod]
    public void LeftOuterJoin_WithParallelization_ShouldReturnCorrectRows()
    {
        const int leftSize = 2000;
        const int rightSize = 1000;
        const string query =
            "select first.FirstItem, second.FirstItem from #schema.first() first left outer join #schema.second() second on first.FirstItem = second.FirstItem";

        var first = Enumerable.Range(0, leftSize).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, rightSize).Select(i => new SecondEntity { FirstItem = i.ToString() })
            .ToArray();

        var vm = CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(leftSize, table.Count, $"Expected {leftSize} rows but got {table.Count}");


        var matchedCount = table.Count(row => row[1] != null);
        var unmatchedCount = table.Count(row => row[1] == null);

        Assert.AreEqual(rightSize, matchedCount, $"Expected {rightSize} matched rows but got {matchedCount}");
        Assert.AreEqual(leftSize - rightSize, unmatchedCount,
            $"Expected {leftSize - rightSize} unmatched rows but got {unmatchedCount}");
    }

    [TestMethod]
    public void LeftOuterJoin_BothModes_ShouldReturnSameResults()
    {
        const int leftSize = 2000;
        const int rightSize = 1000;
        const string query =
            "select first.FirstItem, second.FirstItem from #schema.first() first left outer join #schema.second() second on first.FirstItem = second.FirstItem";

        var first = Enumerable.Range(0, leftSize).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, rightSize).Select(i => new SecondEntity { FirstItem = i.ToString() })
            .ToArray();

        var vmParallel =
            CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel =
            CreateJoinVirtualMachine(query, first, second, new CompilationOptions(ParallelizationMode.None));

        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count,
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");
    }

    #endregion
}
