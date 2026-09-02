using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticTable034TableSyntaxTests
{
    [TestMethod]
    public void TableDefinition_ShouldPreserveColumnOrderNullabilityAndModifierSpans()
    {
        const string query =
            "table Contract { First: int?, Second: System.String encoding 'utf-8' trim, " +
            "Payload: string source Codec 'base64' source Mode 'strict', };";

        var table = ParseTable(query);

        Assert.AreEqual("Contract", table.Name);
        CollectionAssert.AreEqual(
            new[] { "First", "Second", "Payload" },
            table.Columns.Select(static column => column.ColumnName).ToArray());
        CollectionAssert.AreEqual(
            new[] { "int?", "System.String", "string" },
            table.Columns.Select(static column => column.TypeName).ToArray());

        var first = table.Columns[0];
        Assert.AreEqual(SpanOf(query, "First: int?"), first.Span);
        Assert.AreEqual(SpanOf(query, "First"), first.ColumnNameSpan);

        var second = table.Columns[1];
        Assert.AreEqual(SpanOf(query, "Second: System.String encoding 'utf-8' trim"), second.Span);
        Assert.AreEqual(SpanOf(query, "Second"), second.ColumnNameSpan);
        Assert.AreEqual(new TextSpan(SpanOf(query, "encoding 'utf-8'").Start, "encoding 'utf-8'".Length),
            second.ReadModifierSpans["encoding"]);
        Assert.AreEqual(SpanOf(query, "trim"), second.ReadModifierSpans["trim"]);

        var payload = table.Columns[2];
        Assert.AreEqual(SpanOf(query, "Payload: string source Codec 'base64' source Mode 'strict'"), payload.Span);
        CollectionAssert.AreEqual(new[] { "source.codec", "source.mode" },
            payload.ReadModifiers.Select(static modifier => modifier.Key).ToArray());
        Assert.AreEqual("base64", payload.ReadModifiers[0].Value);
        Assert.AreEqual("strict", payload.ReadModifiers[1].Value);
        Assert.AreEqual(SpanOf(query, "source Codec 'base64'"), payload.ReadModifierSpans["source.codec"]);
        Assert.AreEqual(SpanOf(query, "source Mode 'strict'"), payload.ReadModifierSpans["source.mode"]);

        Assert.AreEqual(new TextSpan(0, query.IndexOf('}') + 1), table.Span);
        Assert.AreEqual(
            "table Contract { First: int?, Second: System.String encoding 'utf-8' trim, Payload: string source codec 'base64' source mode 'strict' };",
            table.ToString());
    }

    [TestMethod]
    public void TableDefinition_WithoutSemicolon_ShouldRemainAValidStatement()
    {
        var table = ParseTable("table Contract { Value: string, }");

        Assert.AreEqual("Contract", table.Name);
        Assert.AreEqual("Value", table.Columns.Single().ColumnName);
        Assert.AreEqual("string", table.Columns.Single().TypeName);
    }

    [TestMethod]
    public void TableDefinition_DuplicateSourceModifierKey_ShouldReportTheDuplicateModifierSpan()
    {
        const string query =
            "table Contract { Payload: string source codec 'base64' source CODEC 'hex' };";

        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2012_InvalidSchemaDefinition, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(SpanOf(query, "source CODEC 'hex'"), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.AreEqual("TABLE/COUPLE Spec - TABLE Statement", diagnostic.DocsReference);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void TableDefinition_LeadingColumnSeparator_ShouldReportUnexpectedToken()
    {
        const string query = "table Contract { , First: int };";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            SpanOf(query, ","));
    }

    [TestMethod]
    public void TableDefinition_MissingClosingBrace_ShouldReportUnexpectedEndAtEof()
    {
        const string query = "table Contract { First: int";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile,
            new TextSpan(query.Length, 0));
    }

    private static CreateTableNode ParseTable(string query)
    {
        var lexer = new Lexer(query, true);
        var root = new Parser(lexer).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        return (CreateTableNode)statements.Statements.Single().Node;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static void AssertParseDiagnostic(string query, DiagnosticCode expectedCode, TextSpan expectedSpan)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code, result.FormatDiagnostics());
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
}
