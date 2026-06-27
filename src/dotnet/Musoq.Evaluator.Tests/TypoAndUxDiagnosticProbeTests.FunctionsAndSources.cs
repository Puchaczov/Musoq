using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class TypoAndUxDiagnosticProbeTests
{
    [TestMethod]
    public void WhenFunctionNameTypo_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Coutn(Name) FROM #test.people() GROUP BY Name"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("Count", StringComparison.OrdinalIgnoreCase),
            $"Should suggest 'Count'. Got: {msg}");
    }

    [TestMethod]
    public void WhenUsingLenInsteadOfLength_ShouldGiveHelpfulError()
    {
        // MySQL uses LENGTH, SQL Server uses LEN
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Len(Name) FROM #test.people()"));

        var msg = ex.Message;
        // Should either work or suggest the right function name
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, $"Error should be meaningful. Got: {msg}");
    }

    [TestMethod]
    public void WhenCallingCountWithoutGroupBy_ShouldWork()
    {
        // Common confusion: forgetting GROUP BY with aggregate
        var vm = CompileQuery("SELECT Count(Name) FROM #test.people()");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenCallingNonExistentFunction_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT FakeFunc(Name) FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("FakeFunc", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("unknown", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("resolve", StringComparison.OrdinalIgnoreCase),
            $"Should mention the function or say it's unknown. Got: {msg}");
    }


    // ========================================================================
    // CATEGORY 7: Unterminated/malformed literals
    // ========================================================================


    [TestMethod]
    public void WhenUnterminatedString_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE City = 'London"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("unterminated", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("string", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("quote", StringComparison.OrdinalIgnoreCase),
            $"Should mention unterminated string. Got: {msg}");
    }

    [TestMethod]
    public void WhenUnterminatedBlockComment_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name /* this is a comment FROM #test.people()"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("comment", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("unterminated", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("*/", StringComparison.OrdinalIgnoreCase),
            $"Should mention unterminated comment. Got: {msg}");
    }


    // ========================================================================
    // CATEGORY 8: Join mistakes
    // ========================================================================


    [TestMethod]
    public void WhenJoinMissingOnClause_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT a.Name, b.Amount FROM #test.people() a INNER JOIN #test.orders() b"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void WhenJoinUsingWrongColumnName_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT a.Name, b.Amount FROM #test.people() a INNER JOIN #test.orders() b ON a.Idd = b.PersonId"));

        AssertMessageContains(ex, "Id");
    }

    [TestMethod]
    public void WhenCrossJoinSyntax_ShouldProduceCartesianProduct()
    {
        var vm = CompileQuery("SELECT a.Name, b.Amount FROM #test.people() a CROSS JOIN #test.orders() b");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(25, table.Count);
    }


    // ========================================================================
    // CATEGORY 9: Empty/minimal queries
    // ========================================================================


    [TestMethod]
    public void WhenEmptyQuery_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(""));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Empty query should give a meaningful error");
    }

    [TestMethod]
    public void WhenWhitespaceOnlyQuery_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("   \t\n  "));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Whitespace-only query should give a meaningful error");
    }

    [TestMethod]
    public void WhenJustSemicolon_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(";"));

        var msg = ex.Message;
        Assert.IsNotNull(msg);
        Assert.IsGreaterThan(0, msg.Length, "Semicolon-only should give a meaningful error");
    }

    [TestMethod]
    public void WhenRandomGarbage_ShouldGiveHelpfulError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("asdf qwerty 123"));

        var msg = ex.Message;
        Assert.IsTrue(
            msg.Contains("SELECT", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("FROM", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("query", StringComparison.OrdinalIgnoreCase),
            $"Random garbage should hint at valid query structure. Got: {msg}");
    }


    // ========================================================================
    // CATEGORY 10: Alias-related confusion
    // ========================================================================


}
