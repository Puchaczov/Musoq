using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class EnumDeclarationDiagnosticTests
{
    [TestMethod]
    [DataRow("enum MissingBacking { A = 1 };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "{")]
    [DataRow("enum Unsupported : string { A = 1 };", DiagnosticCode.MQ2043_InvalidEnumBackingType, "string")]
    [DataRow("enum Empty : int {};", DiagnosticCode.MQ2047_EmptyEnumDeclaration, "}")]
    [DataRow("enum Missing : int { A };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "A")]
    [DataRow("enum DecimalValue : int { A = 1.5 };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "1.5")]
    [DataRow("enum MissingComma : int { A = 1 B = 2 };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "B")]
    [DataRow("enum Overflow : byte { A = 256 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "256")]
    [DataRow("enum NegativeUnsigned : uint { A = -1 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-1")]
    [DataRow("enum PositiveSignedOverflow : long { A = 9223372036854775808 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "9223372036854775808")]
    [DataRow("ENUM('Ready', 'Done');", DiagnosticCode.MQ2048_UnsupportedEnumSyntax, "ENUM")]
    [DataRow("CREATE TYPE state AS ENUM ('Ready', 'Done');", DiagnosticCode.MQ2048_UnsupportedEnumSyntax, "CREATE")]
    public void InvalidEnumDeclaration_ShouldReportStablePreciseDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string expectedText)
    {
        AssertParseDiagnostic(query, expectedCode, SpanOf(query, expectedText));
    }

    [TestMethod]
    public void DuplicateExactMemberName_ShouldReportDuplicateNameSpan()
    {
        const string query = "enum Duplicate : int { Ready = 1, Ready = 2 };";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2045_DuplicateEnumMember,
            SpanOf(query, "Ready", query.IndexOf("Ready", StringComparison.Ordinal) + 1));
    }

    [TestMethod]
    public void CaseOnlyMemberName_ShouldReportDuplicateNameSpan()
    {
        const string query = "enum Duplicate : int { Ready = 1, READY = 2 };";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2045_DuplicateEnumMember,
            SpanOf(query, "READY"));
    }

    [TestMethod]
    public void DuplicateEnumTypeName_ShouldBeCaseInsensitive()
    {
        const string query = "enum State : int { Ready = 1 }; enum STATE : int { Done = 2 };";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2042_InvalidEnumDeclaration,
            SpanOf(query, "STATE"));
    }

    [TestMethod]
    public void MissingMemberValue_ShouldRecoverAtNextStatement()
    {
        const string query = "enum Broken : int { Missing }; select 1 from #schema.rows() r;";

        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(1, statements.Statements);
        Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[0].Node);
    }

    [TestMethod]
    public void FlagsWithoutEnumKeyword_ShouldReportInvalidDeclaration()
    {
        const string query = "flags FileAccess : uint { Read = 1ui };";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2042_InvalidEnumDeclaration,
            SpanOf(query, "FileAccess"));
    }

    [TestMethod]
    public void ImplicitCSharpMemberValues_ShouldBeRejected()
    {
        const string query = "enum State : int { Ready, Done };";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2044_MissingEnumMemberValue,
            SpanOf(query, "Ready"));
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
        Assert.AreEqual("Core Spec - Enum Declarations", diagnostic.DocsReference);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    private static TextSpan SpanOf(string query, string text, int startIndex = 0)
    {
        var start = query.IndexOf(text, startIndex, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        return new TextSpan(start, text.Length);
    }
}
