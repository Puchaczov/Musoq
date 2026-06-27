using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class MultiJoinHashJoinTests
{
    #region Four-Way Inner Join Tests

    /// <summary>
    ///     Tests four-way inner join: A JOIN B JOIN C JOIN D - chain of joins
    /// </summary>
    [TestMethod]
    public void FourWayInnerJoin_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name, d.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population
            INNER JOIN #D.Entities() d ON c.Population = d.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] },
            { "#D", [new BasicEntity { Name = "D1", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Should have 1 matching row");
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
        Assert.AreEqual("D1", table[0][3]);
    }

    #endregion

    #region Large Dataset Multi-Join Tests

    /// <summary>
    ///     Tests three-way inner join with larger datasets to verify performance
    ///     and correctness with hash join optimization.
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_WithLargeDatasets_ShouldProduceCorrectResults()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                Enumerable.Range(1, 1000).Select(i => new BasicEntity { Name = $"A{i}", Population = i % 100 })
                    .ToArray()
            },
            {
                "#B",
                Enumerable.Range(1, 500).Select(i => new BasicEntity { Name = $"B{i}", Population = i % 100 }).ToArray()
            },
            {
                "#C",
                Enumerable.Range(1, 200).Select(i => new BasicEntity { Name = $"C{i}", Population = i % 100 }).ToArray()
            }
        };

        const string query = @"
            SELECT a.Count(a.Name)
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population";

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count, "Should have 1 row with count");
        Assert.IsGreaterThan(0L, (long)table[0][0], "Should have some matching rows");
    }

    #endregion

    #region Three-Way Inner Join Tests

    /// <summary>
    ///     Tests three-way inner join: A INNER JOIN B ON a.Id = b.Id INNER JOIN C ON b.Id = c.Id
    ///     All joins should use hash join optimization when enabled.
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_WithHashJoinEnabled_AllJoinOnSameKey_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 200 },
                    new BasicEntity { Name = "A3", Population = 300 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 200 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 100 },
                    new BasicEntity { Name = "C2", Population = 400 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count, "Should have 1 matching row (Population = 100)");
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
    }

    /// <summary>
    ///     Tests three-way inner join with different join keys for each join:
    ///     A INNER JOIN B ON a.Population = b.Population INNER JOIN C ON a.City = c.City
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_WithHashJoinEnabled_DifferentKeysEachJoin_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON a.City = c.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100, City = "NYC" },
                    new BasicEntity { Name = "A2", Population = 200, City = "LA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100, City = "Chicago" }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 500, City = "NYC" },
                    new BasicEntity { Name = "C2", Population = 600, City = "Boston" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count, "Should have 1 matching row");
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
    }

    /// <summary>
    ///     Tests three-way inner join where the third join references the second table:
    ///     A INNER JOIN B ON a.Population = b.Population INNER JOIN C ON b.City = c.City
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_WithHashJoinEnabled_ThirdJoinReferencesSecondTable_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.City = c.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100, City = "NYC" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100, City = "LA" }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 500, City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Should have 1 matching row");
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
    }

    #endregion
}
