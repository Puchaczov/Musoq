using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserAliasGrammarTests
{
    [TestMethod]
    [DataRow("select Name as take from #some.files()")]
    [DataRow("select Name as where from #some.files()")]
    [DataRow("select Name as 1 from #some.files()")]
    public void ExplicitAliasWithInvalidToken_ShouldReportOneTypedDiagnostic(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2022_InvalidAlias, result.Diagnostics[0].Code);
    }

    [TestMethod]
    public void ExplicitAliasAtClauseBoundary_ShouldUseInsertionSpanAfterAs()
    {
        const string query = "select Name as take from #some.files()";
        var result = ParseWithDiagnostics(query);
        var diagnostic = result.Diagnostics[0];
        var asEnd = query.IndexOf("as", StringComparison.OrdinalIgnoreCase) + 2;

        Assert.AreEqual(DiagnosticCode.MQ2022_InvalidAlias, diagnostic.Code);
        Assert.AreEqual(asEnd + 1, diagnostic.Span.Start);
        Assert.AreEqual(0, diagnostic.Span.Length);
    }

    [TestMethod]
    [DataRow("select Name as [take] from #some.files()")]
    [DataRow("select Name as [where] from #some.files()")]
    [DataRow("select Name from #some.files() as source")]
    [DataRow("select Name from #some.files() source")]
    public void ValidAliases_ShouldRemainAccepted(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    [TestMethod]
    public void ExplicitAliasAtEndOfSource_ShouldReportOneDiagnostic()
    {
        const string query = "select Name from #some.files() as";
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, result.Diagnostics[0].Code);
        Assert.AreEqual(query.Length, result.Diagnostics[0].Span.Start);
        Assert.AreEqual(0, result.Diagnostics[0].Span.Length);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var diagnostics = new DiagnosticBag();
        var parser = new Parser(new Lexer(query, true), diagnostics);
        return parser.ParseWithDiagnostics();
    }
}
