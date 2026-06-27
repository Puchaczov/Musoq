using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class MultiJoinHashJoinTests
{
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
}
