using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class MalformedQueryErrorTests
{
    #region Escape sequence errors

    [TestMethod]
    public void WhenInvalidEscapeSequence_ShouldBeTreatedAsLiteral()
    {
        var vm = CompileQuery("SELECT '\\q' FROM #test.single()");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenIncompleteUnicodeEscape_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT '\\u12' FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1004_InvalidEscapeSequence, DiagnosticPhase.Parse, "\\u12");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenIncompleteHexEscape_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT '\\x1' FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1004_InvalidEscapeSequence, DiagnosticPhase.Parse, "\\x1");
        AssertHasGuidance(ex);
    }

    #endregion

    #region Additional parse-level gaps

    [TestMethod]
    public void WhenJoinWithoutTableSource_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() a INNER JOIN ON a.Id = 1"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "cannot be used here");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenMultipleFromClauses_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() FROM #test.orders()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Select");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSelectWithOnlyKeyword_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2005_InvalidSelectList, DiagnosticPhase.Parse, "SELECT list cannot be empty");
    }

    [TestMethod]
    public void WhenDerivedTableInFrom_ShouldCompileAndRun()
    {
        var vm = CompileQuery(
            "SELECT sub.Name FROM (SELECT Name FROM #test.people()) AS sub ORDER BY sub.Name");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenHavingBeforeGroupBy_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT City, Count(1) FROM #test.people() HAVING Count(1) > 1 GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Having is not expected");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSkipBeforeTake_ShouldProperlyWork()
    {
        var vm = CompileQuery(
            "SELECT Name FROM #test.people() ORDER BY Name SKIP 1 TAKE 2");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenTakeWithZero_ShouldReturnEmpty()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() TAKE 0");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenSkipWithZero_ShouldReturnAll()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() SKIP 0 TAKE 100");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    #endregion

    #region Multiple statements / semicolons

    [TestMethod]
    public void WhenTwoSelectStatements_ShouldThrowError()
    {
        // Known quality gap: multiple statements still fall through to generated-code compilation,
        // but the surfaced message should clearly say query processing failed.
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT 1 FROM #test.single(); SELECT 2 FROM #test.single()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ9999_Unknown, DiagnosticPhase.Runtime, "Query processing failed:");
    }

    [TestMethod]
    public void WhenSemicolonOnly_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(";"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Semicolon is not expected");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenMultipleSemicolons_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(";;;"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Semicolon is not expected");
        AssertHasGuidance(ex);
    }

    #endregion

    #region ILIKE error via CompileQuery

    [TestMethod]
    public void WhenILikeUsed_ShouldThrowErrorSuggestingLike()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Name ILIKE '%ali%'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "is not expected here");
        AssertHasGuidance(ex);
    }

    #endregion
}
