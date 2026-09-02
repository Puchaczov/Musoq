using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticRework078TypeAndCastContractTests
{
    [TestMethod]
    public void PostfixCastMatrix_ShouldPreserveTargetSpellingPrecedenceAndSourceSpans()
    {
        const string query =
            "select source.Value::Int32::string, (Value + 1)::Decimal from schema.method() source";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var fields = GetSelectFields(result);
        var outer = Assert.IsInstanceOfType<CastNode>(fields[0].Expression);
        var inner = Assert.IsInstanceOfType<CastNode>(outer.Expression);

        Assert.AreEqual("string", outer.TargetTypeName);
        Assert.AreEqual("Int32", inner.TargetTypeName);
        Assert.AreEqual("source.Value::Int32::string", outer.ToString());
        Assert.AreEqual(
            new TextSpan(query.IndexOf("Value::Int32::string", StringComparison.Ordinal), "Value::Int32::string".Length),
            outer.Span);
        Assert.AreEqual(
            new TextSpan(query.IndexOf("Value::Int32", StringComparison.Ordinal), "Value::Int32".Length),
            inner.Span);

        var parenthesized = Assert.IsInstanceOfType<CastNode>(fields[1].Expression);
        Assert.IsInstanceOfType<AddNode>(parenthesized.Expression);
        Assert.AreEqual("Decimal", parenthesized.TargetTypeName);
        Assert.AreEqual("(Value + 1)::Decimal", parenthesized.ToString());
        Assert.AreEqual(
            new TextSpan(query.IndexOf("Value + 1)::Decimal", StringComparison.Ordinal), "Value + 1)::Decimal".Length),
            parenthesized.Span);
    }

    [TestMethod]
    public void LiteralTypeMatrix_ShouldExposeDocumentedInferenceAndContextualNull()
    {
        const string query =
            "select 42, 42d, 3.14, .5, 0xFF, 0b1010, 0o77, true, 'text', null::DateTime from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var expressions = GetSelectFields(result).Select(static field => field.Expression).ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                typeof(int), typeof(decimal), typeof(decimal), typeof(decimal), typeof(long),
                typeof(long), typeof(long), typeof(bool), typeof(string), null
            },
            expressions.Select(static expression => expression.ReturnType).ToArray());

        var nullCast = Assert.IsInstanceOfType<CastNode>(expressions[^1]);
        Assert.AreEqual("DateTime", nullCast.TargetTypeName);
        Assert.IsInstanceOfType<NullNode>(nullCast.Expression);
    }

    [TestMethod]
    [DataRow(
        "select ::Int32 from schema.method()",
        "::",
        2,
        DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow(
        "select Value::1 from schema.method()",
        "1",
        1,
        DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow(
        "select Value:: from schema.method()",
        "from",
        4,
        DiagnosticCode.MQ2001_UnexpectedToken)]
    public void InvalidCastTargetSyntax_ShouldReportOnePreciseStructuredDiagnostic(
        string query,
        string offendingText,
        int expectedLength,
        DiagnosticCode expectedCode)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            new TextSpan(query.IndexOf(offendingText, StringComparison.Ordinal), expectedLength),
            diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(diagnostic.Span.Start, envelope.Offset);
        Assert.AreEqual(diagnostic.Span.Length, envelope.Length);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    public void NullableCastTarget_ShouldRemainSyntacticallyIdentifierLikeForSemanticValidation()
    {
        const string query = "select Value::Int32? from schema.method()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var cast = Assert.IsInstanceOfType<CastNode>(GetSelectFields(result).Single().Expression);
        Assert.AreEqual("Int32?", cast.TargetTypeName);
    }

    private static FieldNode[] GetSelectFields(ParseResult result)
    {
        var statements = (StatementsArrayNode)result.Root!.Expression;
        var statement = (SingleSetNode)statements.Statements.Single().Node;
        return statement.Query.Select.Fields;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
