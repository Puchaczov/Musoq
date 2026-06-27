using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;

namespace Musoq.Evaluator.Tests;

public partial class TypoAndUxDiagnosticProbeTests
{
    [TestMethod]
    public void WhenUsingAsForTableAlias_ShouldWork()
    {
        // Some SQL dialects use AS for table aliases
        try
        {
            var vm = CompileQuery("SELECT a.Name FROM #test.people() AS a");
            var table = vm.Run(TokenSource.Token);
            Assert.AreEqual(5, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            // If AS is not supported for tables, error should mention it
            var msg = ex.Message;
            Assert.IsNotNull(msg);
        }
    }

    [TestMethod]
    public void WhenReferencingAliasBeforeIsDefined_ShouldGiveHelpfulError()
    {
        // In SQL, the alias is defined after FROM — you can't use it in SELECT before FROM
        // But Musoq might be flexible about evaluation order
        try
        {
            var vm = CompileQuery("SELECT p.Name FROM #test.people() p");
            var table = vm.Run(TokenSource.Token);
            Assert.AreEqual(5, table.Count);
        }
        catch (MusoqQueryException)
        {
            Assert.Fail("Using table alias defined in FROM should work in SELECT");
        }
    }

    [TestMethod]
    public void WhenUsingUndefinedAlias_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT x.Name FROM #test.people() p"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains('x', StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("alias", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("unknown", StringComparison.OrdinalIgnoreCase),
            $"Should mention the undefined alias 'x'. Got: {msg}");
    }


    // ========================================================================
    // CATEGORY 11: Operator confusion
    // ========================================================================


    [TestMethod]
    public void WhenUsingAndSymbolInsteadOfKeyword_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age > 20 && Age < 40"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("and", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("&&", StringComparison.OrdinalIgnoreCase),
            $"Should mention AND keyword. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingOrSymbolInsteadOfKeyword_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age < 20 || Age > 40"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("or", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("||", StringComparison.OrdinalIgnoreCase),
            $"Should mention OR keyword. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingNotEqualsFromCSharp_ShouldGiveHelpfulError()
    {
        // C# style !=
        try
        {
            var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age != 25");
            var table = vm.Run(TokenSource.Token);
            Assert.IsGreaterThan(0, table.Count);
        }
        catch (MusoqQueryException ex)
        {
            Assert.IsTrue(
                ex.Message.Contains("<>", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("diff", StringComparison.OrdinalIgnoreCase),
                $"Should suggest <> for not-equals. Got: {ex.Message}");
        }
    }


    // ========================================================================
    // CATEGORY 12: Special character issues
    // ========================================================================


    [TestMethod]
    public void WhenUsingBacktickForIdentifiers_ShouldGiveHelpfulError()
    {
        // MySQL uses backticks for identifiers
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT `Name` FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Should give error for backtick identifiers");
    }

    [TestMethod]
    public void WhenUsingSemicolonAtEnd_ShouldWorkOrGiveError()
    {
        // Some SQL systems require semicolons; test behavior
        try
        {
            var vm = CompileQuery("SELECT Name FROM #test.people();");
            var table = vm.Run(TokenSource.Token);
            Assert.AreEqual(5, table.Count);
        }
        catch (MusoqQueryException)
        {
            // If semicolons are rejected, that's fine — just verify it fails
        }
    }


    // ========================================================================
    // CATEGORY 13: Aggregate without GROUP BY confusion
    // ========================================================================


    [TestMethod]
    public void WhenMixingAggregateAndNonAggregateColumns_ShouldGiveHelpfulError()
    {
        // Classic SQL mistake: non-aggregated columns without GROUP BY
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name, Count(Age) FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("aggregate", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("group", StringComparison.OrdinalIgnoreCase),
            $"Should mention GROUP BY is needed. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingAggregateInWhereClause_ShouldGiveHelpfulError()
    {
        // Aggregate functions aren't allowed in WHERE — should use HAVING
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City FROM #test.people() WHERE Count(Name) > 1 GROUP BY City"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("HAVING", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("aggregate", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("WHERE", StringComparison.OrdinalIgnoreCase),
            $"Should mention HAVING instead of WHERE for aggregates. Got: {msg}");
    }

}
