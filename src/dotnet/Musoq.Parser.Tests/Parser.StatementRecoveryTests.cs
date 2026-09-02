using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserStatementRecoveryTests
{
    [TestMethod]
    public void ExpressionFailureAtFrom_ShouldReportOnlyTheRootDiagnostic()
    {
        var result = ParseWithDiagnostics("select 1 + from #some.files() take 5");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2020_MissingOperand, result.Diagnostics[0].Code);
    }

    [TestMethod]
    public void Recovery_ShouldResumeAtNextSemicolonDelimitedStatement()
    {
        var result = ParseWithDiagnostics(
            "select 1 + from #some.files(); select 2 from #some.files()");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(1, statements.Statements);
    }

    [TestMethod]
    public void Recovery_ShouldReportOneRootCausePerMalformedStatement()
    {
        var result = ParseWithDiagnostics(
            "select 1 + from #some.files(); select 2 + from #some.files(); select 3 from #some.files()");

        Assert.HasCount(2, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsTrue(result.Diagnostics.All(diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ2020_MissingOperand));
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(1, statements.Statements);
    }

    [TestMethod]
    public void Recovery_ShouldKeepIndependentDiagnosticsInSourceOrder()
    {
        const string query =
            "select 1 + from #some.files(); select Name,, City from #some.files(); " +
            "select Name from #some.files()";

        var result = ParseWithDiagnostics(query);

        Assert.HasCount(2, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2020_MissingOperand, result.Diagnostics[0].Code);
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, result.Diagnostics[1].Code);
        Assert.IsTrue(
            result.Diagnostics[0].Location.Offset < result.Diagnostics[1].Location.Offset,
            result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(1, statements.Statements);
    }

    [TestMethod]
    public void Recovery_ShouldResumeAfterMalformedTrailingSetSlice()
    {
        var result = ParseWithDiagnostics(
            "select Name from #some.files() union select Name from #some.files() take invalid; select Name from #some.files()");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2038_InvalidSliceCount, result.Diagnostics[0].Code);
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(1, statements.Statements);
    }

    [TestMethod]
    [DataRow("select Name,, City from #some.files()")]
    [DataRow("select [Name from #some.files()")]
    public void InvalidExpressionToken_ShouldReportSingleUnexpectedToken(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(
            query.Contains("[Name", StringComparison.Ordinal)
                ? DiagnosticCode.MQ2011_MissingClosingBracket
                : DiagnosticCode.MQ2001_UnexpectedToken,
            result.Diagnostics[0].Code);
    }

    [TestMethod]
    [DataRow("select Name from #some.files() where Name =")]
    [DataRow("select Name from #some.files() where Name = 'test' and")]
    public void IncompleteExpressionAtEnd_ShouldReportSingleMissingOperand(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2020_MissingOperand, result.Diagnostics[0].Code);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var diagnostics = new DiagnosticBag();
        var parser = new Parser(new Lexer(query, true), diagnostics);
        return parser.ParseWithDiagnostics();
    }
}
