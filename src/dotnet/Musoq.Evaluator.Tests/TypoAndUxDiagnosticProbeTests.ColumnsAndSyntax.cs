using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class TypoAndUxDiagnosticProbeTests
{
    [TestMethod]
    public void WhenColumnNameTypoInSelect_ShouldSuggestCorrectName()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Naame FROM #test.people()"));

        AssertMessageContains(ex, "Name");
    }

    [TestMethod]
    public void WhenColumnNameTypoInWhere_ShouldSuggestCorrectName()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Agee > 30"));

        AssertMessageContains(ex, "Age");
    }

    [TestMethod]
    public void WhenColumnNameCaseMismatch_ShouldSuggestCorrectCase()
    {
        // Musoq is case-sensitive for column names but provides a helpful suggestion
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT name FROM #test.people()"));

        AssertMessageContains(ex, "Name");
    }

    [TestMethod]
    public void WhenColumnNameTypoInGroupBy_ShouldSuggestCorrectName()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Ciy, Count(Ciy) FROM #test.people() GROUP BY Ciy"));

        AssertMessageContains(ex, "City");
    }

    [TestMethod]
    public void WhenColumnNameTypoInOrderBy_ShouldSuggestCorrectName()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() ORDER BY Nmae ASC"));

        AssertMessageContains(ex, "Name");
    }


    // ========================================================================
    // CATEGORY 4: Schema/table reference mistakes
    // Users get the FROM clause wrong.
    // ========================================================================


    [TestMethod]
    public void WhenSchemaNameTypo_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #tset.people()"));

        AssertMessageContains(ex, "Unknown schema");
    }

    [TestMethod]
    public void WhenTableMethodTypo_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.poeple()"));

        // Should indicate the table/method is unknown
        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("Unknown table", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("poeple", StringComparison.OrdinalIgnoreCase),
            $"Error message should mention unknown table or the typo 'poeple'. Got: {msg}");
    }

    [TestMethod]
    public void WhenMissingHashInSchemaRef_ShouldSucceed()
    {
        // Design choice: # is being phased out, so test.people() without # is valid
        var vm = CompileQuery("SELECT Name FROM test.people()");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenMissingParensOnTableMethod_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
    }


    // ========================================================================
    // CATEGORY 5: Common structural mistakes
    // ========================================================================


    [TestMethod]
    public void WhenForgettingQuotesAroundString_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE City = London"));

        // "London" should be treated as unknown column; error should mention it
        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("London", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("column", StringComparison.OrdinalIgnoreCase),
            $"Error should mention 'London' as unknown column or suggest quoting. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingDoubleQuotesInsteadOfSingle_ShouldGiveHelpfulError()
    {
        // Standard SQL uses single quotes; double quotes are identifiers
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE City = \"London\""));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1001_UnknownToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "unrecognized");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenMissingSemicolonBetweenStatements_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() SELECT Age FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenUsingAsteriskWithOtherColumns_ShouldReturnBothProjections()
    {
        var vm = CompileQuery("SELECT *, Name FROM #test.people()");
        var table = vm.Run(TokenSource.Token);

        var columnNames = table.Columns.Select(static column => column.ColumnName).ToArray();
        Assert.HasCount(9, columnNames);
        Assert.AreEqual("Id", columnNames[0]);
        Assert.IsTrue(columnNames[1].EndsWith(".Name", StringComparison.Ordinal));
        Assert.AreEqual("Age", columnNames[2]);
        Assert.AreEqual("City", columnNames[3]);
        Assert.AreEqual("Salary", columnNames[4]);
        Assert.AreEqual("BirthDate", columnNames[5]);
        Assert.AreEqual("ManagerId", columnNames[6]);
        Assert.AreEqual("Email", columnNames[7]);
        Assert.IsTrue(columnNames[8].EndsWith(".Name", StringComparison.Ordinal));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, "Alice", 25, "London", 50000m, new DateTime(1999, 1, 15), null, "alice@test.com", "Alice"],
            [2, "Bob", 35, "Paris", 60000m, new DateTime(1989, 6, 20), 1, "bob@test.com", "Bob"],
            [3, "Charlie", 28, "London", 55000m, new DateTime(1996, 3, 10), 1, "charlie@test.com", "Charlie"],
            [4, "Diana", 42, "Berlin", 75000m, new DateTime(1982, 11, 5), 2, "diana@test.com", "Diana"],
            [5, "Eve", 31, "Paris", 62000m, new DateTime(1993, 8, 25), 2, "eve@test.com", "Eve"]);
    }

    [TestMethod]
    public void WhenForgettingCommaInSelectList_ShouldTreatAsAlias()
    {
        // Known limitation: SQL allows implicit aliases (SELECT Name Age means SELECT Name AS Age).
        // Without parser-level AS tracking, we can't detect missing commas vs intentional aliases
        // without producing false positives on UNIONs, JOINs, and CTEs.
        var vm = CompileQuery("SELECT Name Age FROM #test.people()");
        var table = vm.Run(TokenSource.Token);

        // "Age" is treated as an alias for the "Name" column — one column, not two
        Assert.AreEqual(1, table.Columns.Count());
    }

    [TestMethod]
    public void WhenUsingEqualsForEquality_ShouldReturnMatchingRow()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age = 25");
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Alice"]);
    }

    [TestMethod]
    public void WhenUsingExclamationEqualsForNotEqual_ShouldReturnNonMatchingRows()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age != 25");
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob"],
            ["Charlie"],
            ["Diana"],
            ["Eve"]);
    }


    // ========================================================================
    // CATEGORY 6: Function name typos and mistakes
    // ========================================================================


}
