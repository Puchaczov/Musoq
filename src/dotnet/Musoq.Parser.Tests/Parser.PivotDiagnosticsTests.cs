using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserPivotDiagnosticsTests
{
    [TestMethod]
    public void Pivot_WithoutOn_ShouldReportMissingOn()
    {
        AssertPivotSyntaxError(
            "pivot #sales.orders() in ('Q1' as Q1) using Sum(Amount) as Sales",
            DiagnosticCode.MQ2002_MissingToken,
            "requires an ON clause");
    }

    [TestMethod]
    public void Pivot_WithoutIn_ShouldReportStaticInRequirement()
    {
        AssertPivotSyntaxError(
            "pivot #sales.orders() on Quarter using Sum(Amount) as Sales",
            DiagnosticCode.MQ2002_MissingToken,
            "requires a static IN");
    }

    [TestMethod]
    public void Pivot_WithoutUsing_ShouldReportMissingUsing()
    {
        AssertPivotSyntaxError(
            "pivot #sales.orders() on Quarter in ('Q1' as Q1) group by Region",
            DiagnosticCode.MQ2002_MissingToken,
            "requires a USING clause");
    }

    [TestMethod]
    public void Pivot_WithNonConstantValue_ShouldReportConstantRequirement()
    {
        AssertPivotSyntaxError(
            "pivot #sales.orders() on Quarter in (Region as Region) using Sum(Amount) as Sales",
            DiagnosticCode.MQ2003_InvalidExpression,
            "must be constants");
    }

    [TestMethod]
    public void Pivot_WithDuplicateGeneratedColumnName_ShouldReportDuplicateAlias()
    {
        AssertPivotSyntaxError(
            "pivot #sales.orders() on Quarter in ('Q1' as Q, 'Q2' as Q) using Sum(Amount) as Sales",
            DiagnosticCode.MQ2008_DuplicateAlias,
            "duplicate output column name");
    }

    [TestMethod]
    public void Pivot_WithCombinedAliasesColliding_ShouldReportDuplicateAlias()
    {
        AssertPivotSyntaxError(
            "pivot #sales.orders() on Quarter in ('Q1' as A_B, 'Q2' as A) using Sum(Amount) as C, Count(*) as B_C",
            DiagnosticCode.MQ2008_DuplicateAlias,
            "duplicate output column name");
    }

    [TestMethod]
    public void Pivot_WithNonCallUsingExpression_ShouldReportAggregateRequirement()
    {
        AssertPivotSyntaxError(
            "pivot #sales.orders() on Quarter in ('Q1' as Q1) using Amount as Sales",
            DiagnosticCode.MQ2003_InvalidExpression,
            "USING accepts aggregate function calls only");
    }

    [TestMethod]
    public void Pivot_WithMultiColumnValueTupleLengthMismatch_ShouldReportMismatch()
    {
        AssertPivotSyntaxError(
            "pivot #sales.orders() on Year, Country in ((2000) as y2000) using Sum(Amount) as Sales",
            DiagnosticCode.MQ2003_InvalidExpression,
            "tuple length mismatch");
    }

    private static void AssertPivotSyntaxError(string query, DiagnosticCode code, string messageFragment)
    {
        var exception = Assert.Throws<SyntaxException>(() => Parse(query));

        Assert.AreEqual(code, exception.Code);
        StringAssert.Contains(exception.Message, messageFragment);
    }

    private static void Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        parser.ComposeAll();
    }
}
