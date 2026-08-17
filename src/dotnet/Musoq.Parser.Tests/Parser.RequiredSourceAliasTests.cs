using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserRequiredSourceAliasTests
{
    [TestMethod]
    public void ReportedCrossApplyWithoutAlias_ShouldPointAfterPropertyAndRetainBoundary()
    {
        const string query = "select a1.Name from a.b() a1 cross apply a1.Column take 10";
        var diagnostic = GetSingleDiagnostic(query);

        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual("The CROSS APPLY source requires an alias before TAKE.", diagnostic.Message);
        Assert.AreEqual(query.IndexOf("Column", StringComparison.Ordinal) + "Column".Length, diagnostic.Span.Start);
        Assert.AreEqual(0, diagnostic.Span.Length);
        Assert.IsTrue(new DiagnosticFormatter { UseColor = false }.Format(diagnostic).Contains("MQ2035_MissingRequiredAlias"));

        var metadata = ErrorMetadataCatalog.Get(diagnostic.Code);
        Assert.IsNotNull(metadata);
        Assert.IsTrue(metadata!.SuggestedFixes.Length > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(metadata.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(metadata.DocsReference));
    }

    [TestMethod]
    [DataRow("inner join", "INNER JOIN")]
    [DataRow("left join", "LEFT OUTER JOIN")]
    [DataRow("right outer join", "RIGHT OUTER JOIN")]
    [DataRow("full join", "FULL OUTER JOIN")]
    [DataRow("semi join", "SEMI JOIN")]
    [DataRow("anti join", "ANTI JOIN")]
    [DataRow("asof join", "ASOF JOIN")]
    public void MissingRightConditionalJoinAlias_ShouldBeReportedBeforeOn(string operatorText, string displayText)
    {
        var query = $"select 1 from a.first() first {operatorText} b.second() on 1 = 1";
        var diagnostic = GetSingleDiagnostic(query);

        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual($"The {displayText} source requires an alias before ON.", diagnostic.Message);
        Assert.AreEqual(query.IndexOf("second()", StringComparison.Ordinal) + "second()".Length, diagnostic.Span.Start);
        Assert.AreEqual(0, diagnostic.Span.Length);
        Assert.IsFalse(query[diagnostic.Span.Start..].StartsWith("on", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [DataRow("cross join", "CROSS JOIN")]
    [DataRow("outer apply", "OUTER APPLY")]
    public void MissingRightUnconditionalSourceAlias_ShouldBeReportedBeforeNextClause(string operatorText,
        string displayText)
    {
        var query = $"select 1 from a.first() first {operatorText} b.second() take 1";
        var diagnostic = GetSingleDiagnostic(query);

        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual($"The {displayText} source requires an alias before TAKE.", diagnostic.Message);
        Assert.AreEqual(query.IndexOf("second()", StringComparison.Ordinal) + "second()".Length, diagnostic.Span.Start);
        Assert.AreEqual(0, diagnostic.Span.Length);
    }

    [TestMethod]
    [DataRow("inner join")]
    [DataRow("left outer join")]
    [DataRow("right join")]
    [DataRow("full outer join")]
    [DataRow("semi join")]
    [DataRow("anti join")]
    [DataRow("cross join")]
    [DataRow("cross apply")]
    [DataRow("outer apply")]
    [DataRow("asof left join")]
    public void MissingFirstSourceAlias_ShouldBeReportedBeforeEveryMultiSourceOperator(string operatorText)
    {
        var suffix = operatorText.Contains("join", StringComparison.OrdinalIgnoreCase)
            ? " b.second() b on 1 = 1"
            : " b.second() b";
        var query = $"select 1 from a.first() {operatorText}{suffix}";
        var diagnostic = GetSingleDiagnostic(query);

        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual($"The first source in a multi-source query requires an alias before {GetDisplay(operatorText)}.",
            diagnostic.Message);
        Assert.AreEqual(query.IndexOf("first()", StringComparison.Ordinal) + "first()".Length, diagnostic.Span.Start);
        Assert.AreEqual(0, diagnostic.Span.Length);
    }

    [TestMethod]
    public void MultipleMissingAliases_ShouldReportOnlyTheFirstStructuralError()
    {
        const string query = "select 1 from a.first() cross apply a.Column cross apply a.Other";
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, result.Diagnostics[0].Code);
        Assert.IsFalse(result.Diagnostics.Any(d => d.Code is DiagnosticCode.MQ2030_UnsupportedSyntax or DiagnosticCode.MQ2001_UnexpectedToken));
    }

    [TestMethod]
    public void UnaliasedSingleSchemaSource_ShouldRemainBackwardCompatible()
    {
        var result = ParseWithDiagnostics("select 1 from a.first()");

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    [TestMethod]
    public void NaturalInMemorySource_ShouldSatisfyMultiSourceAliasRequirement()
    {
        var result = ParseWithDiagnostics("select item.Value from source cross apply source.Column item");

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    [TestMethod]
    public void ExplicitAliasAfterAsBoundary_ShouldUseRequiredAliasDiagnosticAfterAs()
    {
        const string query = "select 1 from a.first() first cross apply first.Column as take";
        var result = ParseWithDiagnostics(query);
        var diagnostic = result.Diagnostics[0];
        var asEnd = query.IndexOf("as", StringComparison.OrdinalIgnoreCase) + 2;

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual("The source requires an alias identifier after AS.", diagnostic.Message);
        Assert.AreEqual(asEnd, diagnostic.Span.Start);
        Assert.AreEqual(0, diagnostic.Span.Length);
    }

    [TestMethod]
    public void InvalidTokenAfterSourceAs_ShouldRetainMalformedAliasDiagnostic()
    {
        const string query = "select 1 from a.first() first cross apply first.Column as 123";
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2022_InvalidAlias, result.Diagnostics[0].Code);
        Assert.AreEqual(query.IndexOf("123", StringComparison.Ordinal), result.Diagnostics[0].Span.Start);
        Assert.AreEqual(3, result.Diagnostics[0].Span.Length);
    }

    [TestMethod]
    public void DerivedAndValuesSources_ShouldRequireAliasesEvenWhenAlone()
    {
        var derived = GetSingleDiagnostic("select * from (select 1 from source)");
        var values = GetSingleDiagnostic("select * from values { { Name: 'A' } }");

        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, derived.Code);
        Assert.AreEqual("The derived table source requires an alias after the closing parenthesis.", derived.Message);
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, values.Code);
        Assert.AreEqual("The VALUES source requires an alias after the closing brace.", values.Message);
        Assert.AreEqual(0, derived.Span.Length);
        Assert.AreEqual(0, values.Span.Length);
    }

    [TestMethod]
    public void Recovery_ShouldRetainStatementAfterMalformedSourceAlias()
    {
        const string query = "select 1 from a.first() first cross apply first.Column; select 1 from source";
        var result = ParseWithDiagnostics(query);

        Assert.IsNotNull(result.Root);
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, result.Diagnostics[0].Code);

        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(result.Root!.Expression);
        Assert.HasCount(1, statements.Statements);
    }

    [TestMethod]
    public void StrictAndRecoveryParsing_ShouldUseTheSameMissingAliasContract()
    {
        const string query = "select 1 from a.first() first cross apply first.Column take 1";
        var recovery = GetSingleDiagnostic(query);
        var strict = Assert.Throws<SyntaxException>(() => new Parser(new Lexer(query, true)).ComposeAll());

        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, strict.Code);
        Assert.AreEqual(recovery.Message, strict.Message);
        Assert.AreEqual(recovery.Span, strict.Span);
    }

    private static Diagnostic GetSingleDiagnostic(string query)
    {
        var result = ParseWithDiagnostics(query);
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        return result.Diagnostics[0];
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var diagnostics = new DiagnosticBag();
        return new Parser(new Lexer(query, true), diagnostics).ParseWithDiagnostics();
    }

    private static string GetDisplay(string operatorText)
    {
        return operatorText.ToUpperInvariant() switch
        {
            "LEFT JOIN" or "LEFT OUTER JOIN" => "LEFT OUTER JOIN",
            "RIGHT JOIN" or "RIGHT OUTER JOIN" => "RIGHT OUTER JOIN",
            "FULL JOIN" or "FULL OUTER JOIN" => "FULL OUTER JOIN",
            "ASOF LEFT JOIN" => "ASOF LEFT JOIN",
            _ => operatorText.ToUpperInvariant()
        };
    }
}
