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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)), ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", "B1", "C1"],
            ["A2", "B2", null]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)), ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", "B1", "C1"],
            ["A2", null, "C2"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)), ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", "B1", "C1"],
            ["A2", null, "C2"],
            ["A3", null, null]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)),
            ("c.Name", typeof(string)), ("d.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", "B1", "C1", "D1"],
            ["A2", null, null, null]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)), ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", "B1", "C1"],
            [null, "B2", "C2"],
            [null, null, "C3"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)), ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)), ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", null, "C1"],
            ["A2", null, null]);
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


        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)), ("b.Name", typeof(string)), ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", "B1", "C1"],
            ["A1", "B2", "C1"],
            ["A2", "B1", "C1"],
            ["A2", "B2", "C1"]);
    }

    #endregion
}
