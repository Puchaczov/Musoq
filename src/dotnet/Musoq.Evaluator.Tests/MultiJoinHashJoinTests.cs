using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests to verify hash join optimization works correctly with multiple joins (more than 2 tables).
///     These tests are designed to expose potential issues where hash join may not be properly applied
///     to subsequent joins after the first join in a chain.
/// </summary>
[TestClass]
public class MultiJoinHashJoinTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

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
        Assert.IsGreaterThan(0, (int)table[0][0], "Should have some matching rows");
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

    #region Mixed Join Type Tests (Inner + Outer)

    /// <summary>
    ///     Tests mixed join: A INNER JOIN B LEFT OUTER JOIN C
    /// </summary>
    [TestMethod]
    public void InnerJoinThenLeftOuterJoin_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            LEFT OUTER JOIN #C.Entities() c ON b.Population = c.Population
            ORDER BY a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 200 }
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
                    new BasicEntity { Name = "C1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows");


        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);


        Assert.AreEqual("A2", table[1][0]);
        Assert.AreEqual("B2", table[1][1]);
        Assert.IsNull(table[1][2]);
    }

    /// <summary>
    ///     Tests mixed join: A LEFT OUTER JOIN B INNER JOIN C
    /// </summary>
    [TestMethod]
    public void LeftOuterJoinThenInnerJoin_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            LEFT OUTER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON a.Population = c.Population
            ORDER BY a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 100 },
                    new BasicEntity { Name = "C2", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows");


        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);


        Assert.AreEqual("A2", table[1][0]);
        Assert.IsNull(table[1][1]);
        Assert.AreEqual("C2", table[1][2]);
    }

    /// <summary>
    ///     Tests mixed join: A LEFT OUTER JOIN B LEFT OUTER JOIN C
    /// </summary>
    [TestMethod]
    public void DoubleLeftOuterJoin_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            LEFT OUTER JOIN #B.Entities() b ON a.Population = b.Population
            LEFT OUTER JOIN #C.Entities() c ON a.Population = c.Population
            ORDER BY a.Name";

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
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 100 },
                    new BasicEntity { Name = "C2", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Should have 3 rows (all from left side)");


        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);


        Assert.AreEqual("A2", table[1][0]);
        Assert.IsNull(table[1][1]);
        Assert.AreEqual("C2", table[1][2]);


        Assert.AreEqual("A3", table[2][0]);
        Assert.IsNull(table[2][1]);
        Assert.IsNull(table[2][2]);
    }

    /// <summary>
    ///     Tests triple outer join: A LEFT OUTER JOIN B LEFT OUTER JOIN C LEFT OUTER JOIN D
    /// </summary>
    [TestMethod]
    public void TripleLeftOuterJoin_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name, d.Name
            FROM #A.Entities() a
            LEFT OUTER JOIN #B.Entities() b ON a.Population = b.Population
            LEFT OUTER JOIN #C.Entities() c ON a.Population = c.Population
            LEFT OUTER JOIN #D.Entities() d ON a.Population = d.Population
            ORDER BY a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 200 }
                ]
            },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] },
            { "#D", [new BasicEntity { Name = "D1", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows");


        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
        Assert.AreEqual("D1", table[0][3]);


        Assert.AreEqual("A2", table[1][0]);
        Assert.IsNull(table[1][1]);
        Assert.IsNull(table[1][2]);
        Assert.IsNull(table[1][3]);
    }

    /// <summary>
    ///     Tests right outer join: A RIGHT OUTER JOIN B RIGHT OUTER JOIN C
    /// </summary>
    [TestMethod]
    public void DoubleRightOuterJoin_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            RIGHT OUTER JOIN #B.Entities() b ON a.Population = b.Population
            RIGHT OUTER JOIN #C.Entities() c ON b.Population = c.Population
            ORDER BY c.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
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
                    new BasicEntity { Name = "C2", Population = 200 },
                    new BasicEntity { Name = "C3", Population = 300 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Should have 3 rows (all from right side C)");


        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);


        Assert.IsNull(table[1][0]);
        Assert.AreEqual("B2", table[1][1]);
        Assert.AreEqual("C2", table[1][2]);


        Assert.IsNull(table[2][0]);
        Assert.IsNull(table[2][1]);
        Assert.AreEqual("C3", table[2][2]);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    ///     Tests three-way join where one table is empty.
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_WithEmptyMiddleTable_ShouldReturnEmpty()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "Should have 0 rows when middle table is empty");
    }

    /// <summary>
    ///     Tests three-way left outer join where middle table is empty.
    /// </summary>
    [TestMethod]
    public void ThreeWayLeftOuterJoin_WithEmptyMiddleTable_ShouldPreserveLeftRows()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            LEFT OUTER JOIN #B.Entities() b ON a.Population = b.Population
            LEFT OUTER JOIN #C.Entities() c ON a.Population = c.Population
            ORDER BY a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 200 }
                ]
            },
            { "#B", [] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows (from left side)");

        Assert.AreEqual("A1", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("C1", table[0][2]);

        Assert.AreEqual("A2", table[1][0]);
        Assert.IsNull(table[1][1]);
        Assert.IsNull(table[1][2]);
    }

    /// <summary>
    ///     Tests multi-join with duplicate key values in one table.
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_WithDuplicateKeys_ShouldProduceCartesianProduct()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population
            ORDER BY a.Name, b.Name, c.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 100 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(4, table.Count, "Should have 4 rows (2 A x 2 B x 1 C)");
    }

    #endregion

    #region CTE-Based Multi-Join Tests

    /// <summary>
    ///     Tests three-way join using CTEs.
    /// </summary>
    [TestMethod]
    public void ThreeWayCteInnerJoin_WithHashJoinEnabled_ShouldProduceCorrectResults()
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
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
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
    ///     Tests four-way CTE join with mixed join types.
    /// </summary>
    [TestMethod]
    public void FourWayCteJoin_MixedTypes_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH 
                cteA AS (SELECT Name, Population FROM A.Entities()),
                cteB AS (SELECT Name, Population FROM B.Entities()),
                cteC AS (SELECT Name, Population FROM C.Entities()),
                cteD AS (SELECT Name, Population FROM D.Entities())
            SELECT a.Name, b.Name, c.Name, d.Name
            FROM cteA a
            INNER JOIN cteB b ON a.Population = b.Population
            LEFT OUTER JOIN cteC c ON b.Population = c.Population
            LEFT OUTER JOIN cteD d ON a.Population = d.Population
            ORDER BY a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A1", Population = 100 }, new BasicEntity { Name = "A2", Population = 200 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { Name = "B1", Population = 100 }, new BasicEntity { Name = "B2", Population = 200 }
                ]
            },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] },
            { "#D", [new BasicEntity { Name = "D1", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows");


        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
        Assert.AreEqual("D1", table[0][3]);


        Assert.AreEqual("A2", table[1][0]);
        Assert.AreEqual("B2", table[1][1]);
        Assert.IsNull(table[1][2]);
        Assert.IsNull(table[1][3]);
    }

    #endregion

    #region Code Generation Verification Tests - Hash Join Works for All Joins

    /// <summary>
    ///     Verifies that hash join is used for ALL joins in a multi-join query.
    ///     For a query like: A JOIN B ON a.x = b.x JOIN C ON b.y = c.y
    ///     - The A-B join uses hash join (creates bHashed dictionary)
    ///     - The (AB)-C join also uses hash join (creates abHashed dictionary)
    ///     This was fixed by allowing chained joins with prefixed column names in
    ///     JoinInMemoryWithSourceTableNodeProcessor.TryGetHashJoinKeys.
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_GeneratedCode_AllJoinsUseHashJoin()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code for Three-Way Inner Join ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");


        var hashDictionaryCount =
            CountOccurrences(generatedCode, "Hashed = new System.Collections.Generic.Dictionary<");


        Assert.AreEqual(2, hashDictionaryCount,
            $"Expected 2 hash dictionaries for a three-way join, but found {hashDictionaryCount}. " +
            "Both the A-B join and the AB-C join should use hash join.");


        Assert.Contains("bHashed = new System.Collections.Generic.Dictionary<",
generatedCode, "First join should create bHashed dictionary");
        Assert.Contains("abHashed = new System.Collections.Generic.Dictionary<",
generatedCode, "Second join should create abHashed dictionary for the intermediate result");
    }

    /// <summary>
    ///     Verifies that hash join is used for ALL joins in a four-way join query.
    /// </summary>
    [TestMethod]
    public void FourWayInnerJoin_GeneratedCode_AllJoinsUseHashJoin()
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

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code for Four-Way Inner Join ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");

        var hashDictionaryCount =
            CountOccurrences(generatedCode, "Hashed = new System.Collections.Generic.Dictionary<");


        Assert.AreEqual(3, hashDictionaryCount,
            $"Expected 3 hash dictionaries for a four-way join, but found {hashDictionaryCount}. " +
            "All three joins should use hash join.");
    }

    /// <summary>
    ///     Verifies that hash join is used for ALL joins in a three-way left outer join query.
    ///     Now that hash join is properly implemented for chained outer joins, all joins use hash join.
    /// </summary>
    [TestMethod]
    public void ThreeWayLeftOuterJoin_GeneratedCode_AllJoinsUseHashJoin()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            LEFT OUTER JOIN #B.Entities() b ON a.Population = b.Population
            LEFT OUTER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code for Three-Way Left Outer Join ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");

        var hashDictionaryCount =
            CountOccurrences(generatedCode, "Hashed = new System.Collections.Generic.Dictionary<");


        Assert.AreEqual(2, hashDictionaryCount,
            $"Expected 2 hash dictionaries (all joins use hash join), but found {hashDictionaryCount}.");
    }

    /// <summary>
    ///     Verifies that for mixed joins (inner + outer), inner joins use hash join but chained outer joins fall back.
    /// </summary>
    [TestMethod]
    public void MixedJoins_GeneratedCode_AllJoinsUseHashJoin()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            LEFT OUTER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code for Mixed Join Types ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");

        var hashDictionaryCount =
            CountOccurrences(generatedCode, "Hashed = new System.Collections.Generic.Dictionary<");


        Assert.AreEqual(2, hashDictionaryCount,
            $"Expected 2 hash dictionaries (all joins use hash join), but found {hashDictionaryCount}.");
    }

    /// <summary>
    ///     Verifies that WITHOUT hash join enabled, the code uses nested loops instead.
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_WithoutHashJoin_ShouldNotContainHashDictionaries()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code WITHOUT Hash Join ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");

        var hashDictionaryCount =
            CountOccurrences(generatedCode, "Hashed = new System.Collections.Generic.Dictionary<");

        Assert.AreEqual(0, hashDictionaryCount,
            $"Expected 0 hash dictionaries when hash join is disabled, but found {hashDictionaryCount}. " +
            $"\n\nGenerated code:\n{generatedCode}");
    }

    #endregion

    #region Helper Methods

    private string CompileAndGetGeneratedCode(
        string query,
        Dictionary<string, IEnumerable<BasicEntity>> sources,
        CompilationOptions compilationOptions)
    {
        RuntimeLibraries.CreateReferences();


        var items = new BuildItems
        {
            SchemaProvider = new BasicSchemaProvider<BasicEntity>(sources),
            RawQuery = query,
            AssemblyName = Guid.NewGuid().ToString(),
            CompilationOptions = compilationOptions,
            CreateBuildMetadataAndInferTypesVisitor = null
        };


        var chain = new CreateTree(
            new CompileInterpretationSchemas(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null), LoggerResolver)));

        chain.Build(items);


        var builder = new StringBuilder();
        if (items.Compilation?.SyntaxTrees != null)
            foreach (var tree in items.Compilation.SyntaxTrees)
            {
                using var writer = new StringWriter();
                tree.GetRoot().WriteTo(writer);
                builder.AppendLine(writer.ToString());
                builder.AppendLine();
            }

        return builder.ToString();
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    #endregion
}
