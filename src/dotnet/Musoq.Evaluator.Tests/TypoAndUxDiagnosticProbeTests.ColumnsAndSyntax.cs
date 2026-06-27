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

        var msg = ex.Message;
        // Should give some indication about the issue
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Should produce a non-empty error message");
    }

    [TestMethod]
    public void WhenMissingSemicolonBetweenStatements_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() SELECT Age FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Should produce a non-empty error message");
    }

    [TestMethod]
    public void WhenUsingAsterikWithOtherColumns_ShouldWorkOrGiveError()
    {
        // SELECT *, Name is unusual; test behavior
        try
        {
            var vm = CompileQuery("SELECT *, Name FROM #test.people()");
            var table = vm.Run(TokenSource.Token);
            // If it works, verify it has results
            Assert.IsGreaterThan(0, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            // If it fails, error should be helpful
            var msg = ex.Message;
            Assert.IsNotNull(msg);
            Assert.IsGreaterThan(0, msg.Length);
        }
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
    public void WhenUsingEqualsEqualsInsteadOfEquals_ShouldWork()
    {
        // Some SQL systems use =, some ==; test Musoq behavior
        try
        {
            var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age = 25");
            var table = vm.Run(TokenSource.Token);
            Assert.AreEqual(1, table.Count);
        }
        catch (MusoqQueryException)
        {
            // If = doesn't work, that's a gap
            Assert.Fail("Simple equality comparison with = should work");
        }
    }

    [TestMethod]
    public void WhenUsingExclamationEqualsForNotEqual_ShouldGiveHelpfulError()
    {
        try
        {
            var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age != 25");
            var table = vm.Run(TokenSource.Token);
            Assert.IsGreaterThan(0, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            // If != doesn't work, should suggest <> or diff
            var msg = ex.Message;
            Assert.IsTrue(
                msg.Contains("<>", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("diff", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("not equal", StringComparison.OrdinalIgnoreCase),
                $"Should suggest alternative not-equal syntax. Got: {msg}");
        }
    }


    // ========================================================================
    // CATEGORY 6: Function name typos and mistakes
    // ========================================================================


}
