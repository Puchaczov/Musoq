using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCouple036CoupleSyntaxTests
{
    [TestMethod]
    [DataRow(
        "couple #some.method with table Rows as Source",
        "Rows",
        "",
        "Source")]
    [DataRow(
        "couple #some.method with settings Profile as Source;",
        "",
        "Profile",
        "Source")]
    [DataRow(
        "couple #some.method with table Rows and settings Profile as Source;",
        "Rows",
        "Profile",
        "Source")]
    [DataRow(
        "couple #some.method with SETTINGS Profile and table Rows as Source;",
        "Rows",
        "Profile",
        "Source")]
    public void CoupleForms_ShouldParseWithoutMethodParenthesesAndPreserveOptions(
        string query,
        string expectedTableName,
        string expectedProfileName,
        string expectedAlias)
    {
        var couple = ParseCouple(query);

        Assert.AreEqual("#some", couple.SchemaMethodNode.Schema);
        Assert.AreEqual("method", couple.SchemaMethodNode.Method);
        Assert.AreEqual(
            string.IsNullOrEmpty(expectedTableName) ? null : expectedTableName,
            couple.TableName);
        Assert.AreEqual(
            string.IsNullOrEmpty(expectedProfileName) ? null : expectedProfileName,
            couple.ProfileName);
        Assert.AreEqual(expectedAlias, couple.MappedSchemaName);
        Assert.AreEqual(
            couple.TableName != null && couple.ProfileName != null
                ? $"couple #some.method with table {couple.TableName} and settings {couple.ProfileName} as {expectedAlias};"
                : couple.TableName != null
                    ? $"couple #some.method with table {couple.TableName} as {expectedAlias};"
                    : $"couple #some.method with settings {couple.ProfileName} as {expectedAlias};",
            couple.ToString());
    }

    [TestMethod]
    public void HashQualifiedCoupleSchemaMethod_WithParentheses_ShouldBeRejectedAtTheOpeningParenthesis()
    {
        const string query = "couple #some.method() with table Rows as Source;";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "Expected token is With but received LeftParenthesis.",
            SpanOf(query, "("));
    }

    [TestMethod]
    public void UnqualifiedCoupleSchemaMethod_WithParentheses_ShouldReportUnsupportedSyntaxAtSchema()
    {
        const string query = "couple some.method() with table Rows as Source;";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "COUPLE schema methods are declarations and must not include parentheses; pass arguments when invoking the coupled alias.",
            SpanOf(query, "some"));
    }

    [TestMethod]
    public void CoupleDuplicateTableOption_ShouldReportTheSecondOptionKeyword()
    {
        const string query = "couple #some.method with table First and table Second as Source;";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "Duplicate table option in couple statement.",
            LastSpanOf(query, "table"));
    }

    [TestMethod]
    public void CoupleDuplicateSettingsOption_ShouldReportTheSecondOptionKeyword()
    {
        const string query = "couple #some.method with settings First and settings Second as Source;";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "Duplicate settings option in couple statement.",
            LastSpanOf(query, "settings"));
    }

    [TestMethod]
    public void CoupleWithoutAnOption_ShouldReportTheWithFollower()
    {
        const string query = "couple #some.method with as Source;";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "Expected table or settings option in couple statement.",
            SpanOf(query, "as"));
    }

    [TestMethod]
    [DataRow(
        "couple #some.method with table 123 as Source;",
        "table",
        "123")]
    [DataRow(
        "couple #some.method with settings 'profile' as Source;",
        "settings",
        "'profile'")]
    public void CoupleOptionNames_MustBeIdentifiers(
        string query,
        string optionName,
        string invalidName)
    {
        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            $"Expected {optionName} name in couple statement.",
            SpanOf(query, invalidName));
    }

    private static CoupleNode ParseCouple(string query)
    {
        var lexer = new Lexer(query, true);
        var root = new Parser(lexer, lexer.Diagnostics).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        return (CoupleNode)statements.Statements.Single().Node;
    }

    private static void AssertParseDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        TextSpan expectedSpan)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code, result.FormatDiagnostics());
        StringAssert.StartsWith(diagnostic.Message, expectedMessage);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsTrue(diagnostic.Location.IsValid);
        Assert.IsTrue(diagnostic.EndLocation.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{query}'.");
        return new TextSpan(start, text.Length);
    }

    private static TextSpan LastSpanOf(string query, string text)
    {
        var start = query.LastIndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{query}'.");
        return new TextSpan(start, text.Length);
    }
}
