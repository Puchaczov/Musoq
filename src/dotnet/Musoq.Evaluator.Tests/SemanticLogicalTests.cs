using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SemanticLogicalTests : NegativeTestsBase
{
    #region 6.1 Empty / Vacuous Queries

    [TestMethod]
    public void SL001_WhereAlwaysFalse_ShouldReturnZeroRows()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() WHERE 1 = 0");
        var table = vm.Run(CancellationToken.None);
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void SL002_WhereAlwaysTrue_ShouldReturnAllRows()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() WHERE 1 = 1");
        var table = vm.Run(CancellationToken.None);
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice"],
            ["Bob"],
            ["Charlie"],
            ["Diana"],
            ["Eve"]);
    }

    #endregion

    #region 6.2 Nonsensical but Valid Combinations

    [TestMethod]
    public void SL011_GroupByAllColumns_ShouldReturnAllRows()
    {
        var vm = CompileQuery("SELECT Name, Age, City FROM #test.people() GROUP BY Name, Age, City");
        var table = vm.Run(CancellationToken.None);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Age", typeof(int)),
            ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 25, "London"],
            ["Bob", 35, "Paris"],
            ["Charlie", 28, "London"],
            ["Diana", 42, "Berlin"],
            ["Eve", 31, "Paris"]);
    }

    [TestMethod]
    public void SL013_SkipMoreRowsThanExist_ShouldReturnZeroRows()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() ORDER BY Name SKIP 999999");
        var table = vm.Run(CancellationToken.None);
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    #endregion

    #region 6.3 Cross-Source Edge Cases

    [TestMethod]
    public void SL020_InnerJoinWhereNoRowsMatch_ShouldReturnZeroRows()
    {
        var vm = CompileQuery(
            "SELECT p.Name AS PersonName, e.Name AS EmptyName FROM #test.people() p INNER JOIN #test.empty() e ON p.Id = e.Id");
        var table = vm.Run(CancellationToken.None);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("PersonName", typeof(string)),
            ("EmptyName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    [FeatureEvidence("outer-join-empty-results", FeatureEvidenceKind.RuntimePositive)]
    public void SL021_LeftOuterJoinWithEmptyRightSide_ShouldReturnAllLeftRows()
    {
        var vm = CompileQuery(
            "SELECT p.Name AS PersonName, e.Name AS EmptyName FROM #test.people() p LEFT OUTER JOIN #test.empty() e ON p.Id = e.Id");
        var table = vm.Run(CancellationToken.None);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("PersonName", typeof(string)),
            ("EmptyName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null],
            ["Bob", null],
            ["Charlie", null],
            ["Diana", null],
            ["Eve", null]);
    }

    [TestMethod]
    public void SL022_SelfJoin_ShouldProduceCorrectResults()
    {
        var vm = CompileQuery(
            "SELECT a.Name AS LeftName, b.Name AS RightName FROM #test.people() a INNER JOIN #test.people() b ON a.City = b.City WHERE a.Name <> b.Name");
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("LeftName", typeof(string)),
            ("RightName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Charlie"],
            ["Charlie", "Alice"],
            ["Bob", "Eve"],
            ["Eve", "Bob"]);
    }

    #endregion
}
