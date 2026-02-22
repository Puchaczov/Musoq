using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests to verify that hash join optimization is applied when joining CTEs.
///     Issue: JoinInMemoryWithSourceTableFromNode (used for CTE joins) was not using
///     hash join or merge sort join, falling back to O(n²) nested loop joins.
/// </summary>
[TestClass]
public class CteHashJoinTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    /// <summary>
    ///     Minimal reproducible test case for CTE hash join.
    ///     This tests joining two CTEs with an equality condition which should use hash join.
    /// </summary>
    [TestMethod]
    public void JoinTwoCtes_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH 
                cteA AS (SELECT Name, Population FROM A.Entities()),
                cteB AS (SELECT Name, Population FROM B.Entities())
            SELECT a.Name, b.Name 
            FROM cteA a 
            INNER JOIN cteB b ON a.Population = b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "CountryA", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "CountryA", Population = 200 },
                    new BasicEntity { Name = "A3", Country = "CountryA", Population = 300 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "CountryB", Population = 100 },
                    new BasicEntity { Name = "B2", Country = "CountryB", Population = 400 }
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
    }

    /// <summary>
    ///     Tests left outer join between two CTEs with hash join enabled.
    /// </summary>
    [TestMethod]
    public void LeftOuterJoinTwoCtes_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH 
                cteA AS (SELECT Name, Population FROM A.Entities()),
                cteB AS (SELECT Name, Population FROM B.Entities())
            SELECT a.Name, b.Name 
            FROM cteA a 
            LEFT OUTER JOIN cteB b ON a.Population = b.Population
            ORDER BY a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "CountryA", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "CountryA", Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "CountryB", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows (all from left side)");
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("A2", table[1][0]);
        Assert.IsNull(table[1][1]);
    }

    /// <summary>
    ///     Tests right outer join between two CTEs with hash join enabled.
    /// </summary>
    [TestMethod]
    public void RightOuterJoinTwoCtes_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH 
                cteA AS (SELECT Name, Population FROM A.Entities()),
                cteB AS (SELECT Name, Population FROM B.Entities())
            SELECT a.Name, b.Name 
            FROM cteA a 
            RIGHT OUTER JOIN cteB b ON a.Population = b.Population
            ORDER BY b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "CountryA", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "CountryB", Population = 100 },
                    new BasicEntity { Name = "B2", Country = "CountryB", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows (all from right side)");
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.IsNull(table[1][0]);
        Assert.AreEqual("B2", table[1][1]);
    }

    /// <summary>
    ///     Tests joining CTE with a regular source table with hash join enabled.
    /// </summary>
    [TestMethod]
    public void JoinCteWithSourceTable_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH 
                cteA AS (SELECT Name, Population FROM A.Entities())
            SELECT a.Name, b.Name 
            FROM cteA a 
            INNER JOIN B.Entities() b ON a.Population = b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "CountryA", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "CountryA", Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "CountryB", Population = 100 },
                    new BasicEntity { Name = "B2", Country = "CountryB", Population = 300 }
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
    }

    /// <summary>
    ///     Tests multiple CTE joins with hash join enabled.
    /// </summary>
    [TestMethod]
    public void JoinMultipleCtes_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH 
                cteA AS (SELECT Name, Population FROM A.Entities()),
                cteB AS (SELECT Name, Population FROM B.Entities()),
                cteC AS (SELECT Name, Population FROM C.Entities())
            SELECT a.Name, b.Name, c.Name 
            FROM cteA a 
            INNER JOIN cteB b ON a.Population = b.Population
            INNER JOIN cteC c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Country = "CountryA", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Country = "CountryB", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Country = "CountryC", Population = 100 }] }
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
    ///     Tests CTE join with sort-merge join enabled instead of hash join.
    /// </summary>
    [TestMethod]
    public void JoinTwoCtes_WithSortMergeJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH 
                cteA AS (SELECT Name, Population FROM A.Entities()),
                cteB AS (SELECT Name, Population FROM B.Entities())
            SELECT a.Name, b.Name 
            FROM cteA a 
            INNER JOIN cteB b ON a.Population = b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "CountryA", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "CountryA", Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "CountryB", Population = 100 },
                    new BasicEntity { Name = "B2", Country = "CountryB", Population = 300 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Should have 1 matching row (Population = 100)");
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
    }
}
