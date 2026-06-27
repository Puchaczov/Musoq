using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserUnpivotDiagnosticsTests
{
    [TestMethod]
    public void Unpivot_WithoutOn_ShouldReportMissingOn()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() in (Q1 as Q1) using Sales",
            DiagnosticCode.MQ2002_MissingToken,
            "requires an ON clause");
    }

    [TestMethod]
    public void Unpivot_WithoutIn_ShouldReportMissingIn()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter using Sales",
            DiagnosticCode.MQ2002_MissingToken,
            "requires an IN");
    }

    [TestMethod]
    public void Unpivot_WithoutUsing_ShouldReportMissingUsing()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter in (Q1 as Q1) keep Region",
            DiagnosticCode.MQ2002_MissingToken,
            "requires a USING clause");
    }

    [TestMethod]
    public void Unpivot_WithEmptyIn_ShouldReportExpressionRequirement()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter in () using Sales",
            DiagnosticCode.MQ2003_InvalidExpression,
            "requires at least one");
    }

    [TestMethod]
    public void Unpivot_WithTrailingInComma_ShouldReportTrailingComma()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter in (Q1 as Q1,) using Sales",
            DiagnosticCode.MQ2014_TrailingComma,
            "trailing comma");
    }

    [TestMethod]
    public void Unpivot_WithComplexEntryWithoutAlias_ShouldReportAliasRequirement()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter in (Q1 + Q2) using Sales",
            DiagnosticCode.MQ2022_InvalidAlias,
            "require an alias");
    }

    [TestMethod]
    public void Unpivot_WithComplexKeepWithoutAlias_ShouldReportAliasRequirement()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter in (Q1 as Q1) using Sales keep Region + ':' + Country",
            DiagnosticCode.MQ2022_InvalidAlias,
            "KEEP expressions require an alias");
    }

    [TestMethod]
    public void Unpivot_WithDuplicateOutputName_ShouldReportDuplicateAlias()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter in (Q1 as Q1) using Quarter",
            DiagnosticCode.MQ2008_DuplicateAlias,
            "duplicate output column name");
    }

    [TestMethod]
    public void Unpivot_WithDuplicateKeepOutputName_ShouldReportDuplicateAlias()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter in (Q1 as Q1) using Sales keep Region as Quarter",
            DiagnosticCode.MQ2008_DuplicateAlias,
            "duplicate output column name");
    }

    [TestMethod]
    public void Unpivot_WithDuplicateEntryName_ShouldReportDuplicateAlias()
    {
        AssertUnpivotSyntaxError(
            "unpivot #sales.wide() on Quarter in (Q1 as Q, Q2 as Q) using Sales",
            DiagnosticCode.MQ2008_DuplicateAlias,
            "duplicate name value");
    }

    private static void AssertUnpivotSyntaxError(string query, DiagnosticCode code, string messageFragment)
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
