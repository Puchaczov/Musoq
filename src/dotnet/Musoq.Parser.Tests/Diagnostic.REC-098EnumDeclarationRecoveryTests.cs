using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticREC098EnumDeclarationRecoveryTests
{
    private static readonly EnumDiagnosticCase[] CandidateCases =
    [
        new("REC-098-S01", "structure", "enum : int { A = 1 };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, ":", 0),
        new("REC-098-S02", "structure", "enum MissingColon int { A = 1 };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "int", 0),
        new("REC-098-S03", "structure", "enum MissingBody : int A = 1;", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "A", 0),
        new("REC-098-S04", "structure", "enum MissingComma : int { A = 1 B = 2 };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "B", 0),

        new("REC-098-V01", "backing-and-value", "enum Unsupported : string { A = 1 };", DiagnosticCode.MQ2043_InvalidEnumBackingType, "string", 0),
        new("REC-098-V02", "backing-and-value", "enum MissingValue : int { A };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "A", 0),
        new("REC-098-V03", "backing-and-value", "enum DecimalValue : int { A = 1.5 };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "1.5", 0),
        new("REC-098-V04", "backing-and-value", "enum Overflow : byte { A = 256 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "256", 0),

        new("REC-098-D01", "duplicate-and-empty", "enum Empty : int {};", DiagnosticCode.MQ2047_EmptyEnumDeclaration, "}", 0),
        new("REC-098-D02", "duplicate-and-empty", "enum Duplicate : int { Ready = 1, Ready = 2 };", DiagnosticCode.MQ2045_DuplicateEnumMember, "Ready", 1),
        new("REC-098-D03", "duplicate-and-empty", "enum Duplicate : int { Ready = 1, READY = 2 };", DiagnosticCode.MQ2045_DuplicateEnumMember, "READY", 0),
        new("REC-098-D04", "duplicate-and-empty", "enum State : int { Ready = 1 }; enum STATE : int { Done = 2 };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "STATE", 0),

        new("REC-098-M01", "member-grammar", "enum MissingEquals : int { Ready 1 };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "Ready", 0),
        new("REC-098-M02", "member-grammar", "enum BadMember : int { 1 = 1 };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "1", 0),
        new("REC-098-M03", "member-grammar", "enum DoubleComma : int { A = 1,, B = 2 };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, ",", 1),
        new("REC-098-M04", "member-grammar", "enum MissingClose : int { A = 1", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "<eof>", 0),

        new("REC-098-B01", "borrowed-dialect-forms", "ENUM('Ready', 'Done');", DiagnosticCode.MQ2048_UnsupportedEnumSyntax, "ENUM", 0),
        new("REC-098-B02", "borrowed-dialect-forms", "CREATE TYPE state AS ENUM ('Ready', 'Done');", DiagnosticCode.MQ2048_UnsupportedEnumSyntax, "CREATE", 0),
        new("REC-098-B03", "borrowed-dialect-forms", "enum State : int { Ready, Done };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "Ready", 0),
        new("REC-098-B04", "borrowed-dialect-forms", "public enum State : int { Ready = 1 };", DiagnosticCode.MQ2001_UnexpectedToken, "public", 0),

        new("REC-098-C01", "contextual-boundaries", "enum NegativeUnsigned : uint { A = -1 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-1", 0),
        new("REC-098-C02", "contextual-boundaries", "enum PositiveSignedOverflow : long { A = 9223372036854775808 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "9223372036854775808", 0),
        new("REC-098-C03", "contextual-boundaries", "flags FileAccess : uint { Read = 1ui };", DiagnosticCode.MQ2042_InvalidEnumDeclaration, "FileAccess", 0),
        new("REC-098-C04", "contextual-boundaries", "enum StringValue : int { A = 'one' };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "'one'", 0)
    ];

    public static IEnumerable<object[]> CandidateData =>
        CandidateCases.Select(static testCase => new object[] { testCase });

    [TestMethod]
    public void CandidateMatrix_ShouldContainFourCasesPerFamily()
    {
        Assert.HasCount(24, CandidateCases);
        Assert.AreEqual(24, CandidateCases.Select(static testCase => testCase.CaseId).Distinct(StringComparer.Ordinal).Count());
        CollectionAssert.AreEquivalent(
            new[]
            {
                "backing-and-value:4",
                "borrowed-dialect-forms:4",
                "contextual-boundaries:4",
                "duplicate-and-empty:4",
                "member-grammar:4",
                "structure:4"
            },
            CandidateCases
                .GroupBy(static testCase => testCase.Family, StringComparer.Ordinal)
                .Select(static group => $"{group.Key}:{group.Count()}")
                .ToArray());

        var expectedCodes = new[]
        {
            DiagnosticCode.MQ2042_InvalidEnumDeclaration,
            DiagnosticCode.MQ2043_InvalidEnumBackingType,
            DiagnosticCode.MQ2044_MissingEnumMemberValue,
            DiagnosticCode.MQ2045_DuplicateEnumMember,
            DiagnosticCode.MQ2046_EnumMemberValueOutOfRange,
            DiagnosticCode.MQ2047_EmptyEnumDeclaration,
            DiagnosticCode.MQ2048_UnsupportedEnumSyntax
        };
        var actualCodes = CandidateCases
            .Select(static testCase => testCase.ExpectedCode)
            .Where(expectedCodes.Contains)
            .Distinct()
            .ToArray();

        CollectionAssert.AreEquivalent(expectedCodes, actualCodes);
    }

    [TestMethod]
    [DynamicData(nameof(CandidateData))]
    public void EnumDeclarationDiagnostics_ShouldPinContractedCodeAndSpan(EnumDiagnosticCase testCase)
    {
        var result = ParseWithDiagnostics(testCase.Query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(testCase.ExpectedCode, diagnostic.Code, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(ExpectedSpan(testCase), diagnostic.Span, result.FormatDiagnostics());
        Assert.IsTrue(diagnostic.Location.IsValid, result.FormatDiagnostics());
        Assert.IsTrue(diagnostic.EndLocation.IsValid, result.FormatDiagnostics());
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation), result.FormatDiagnostics());
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference), result.FormatDiagnostics());
        Assert.IsNotEmpty(diagnostic.SuggestedFixes, result.FormatDiagnostics());
        Assert.IsNotNull(diagnostic.ContextSnippet, result.FormatDiagnostics());
    }

    [TestMethod]
    public void ValidCanonicalAndFlagsDeclarations_ShouldRemainValid()
    {
        const string query =
            "enum JobStatus : int { Queued = 10, Running = 20, Finished = 30, };" +
            "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui, };";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.HasCount(0, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(2, statements.Statements);
        var declarations = statements.Statements.Select(static statement => (EnumDeclarationNode)statement.Node).ToArray();
        Assert.AreEqual("JobStatus", declarations[0].Name);
        Assert.IsFalse(declarations[0].IsFlags);
        Assert.AreEqual("FileAccess", declarations[1].Name);
        Assert.IsTrue(declarations[1].IsFlags);
        CollectionAssert.AreEqual(new ulong[] { 10, 20, 30 }, declarations[0].Members.Select(static member => member.RawValue).ToArray());
        CollectionAssert.AreEqual(new ulong[] { 0, 1, 2, 3 }, declarations[1].Members.Select(static member => member.RawValue).ToArray());
    }

    [TestMethod]
    public void EveryIntegralBackingTypeAndBoundary_ShouldRemainValid()
    {
        const string query =
            "enum EByte : byte { Min = 0ub, Max = 255ub };" +
            "enum ESByte : sbyte { Min = -128, Max = 127b };" +
            "enum EShort : short { Min = -32768, Max = 32767s };" +
            "enum EUShort : ushort { Min = 0us, Max = 65535us };" +
            "enum EInt : int { Min = -2147483648, Max = 2147483647i };" +
            "enum EUInt : uint { Min = 0ui, Max = 4294967295ui };" +
            "enum ELong : long { Min = -9223372036854775808, Max = 9223372036854775807l };" +
            "enum EULong : ulong { Min = 0ul, Max = 18446744073709551615ul };";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.HasCount(0, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(8, statements.Statements);
        CollectionAssert.AreEqual(
            new[] { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong" },
            statements.Statements.Select(static statement => ((EnumDeclarationNode)statement.Node).UnderlyingTypeName).ToArray());
    }

    [TestMethod]
    public void TrailingCommaAliasesAndContextualIdentifiers_ShouldRemainValid()
    {
        const string query =
            "enum State : int { Ready = 1, Alias = 1, };" +
            "select enum, flags from #schema.rows() r;";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.HasCount(0, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(2, statements.Statements);
        var declaration = (EnumDeclarationNode)statements.Statements[0].Node;
        Assert.AreEqual(2, declaration.Members.Count);
        Assert.AreEqual(declaration.Members[0].RawValue, declaration.Members[1].RawValue);
        Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[1].Node);
    }

    private static TextSpan ExpectedSpan(EnumDiagnosticCase testCase)
    {
        return testCase.ExpectedText == "<eof>"
            ? new TextSpan(testCase.Query.Length, 0)
            : SpanOf(testCase.Query, testCase.ExpectedText, testCase.Occurrence);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static TextSpan SpanOf(string query, string text, int occurrence)
    {
        var start = -1;
        for (var index = 0; index <= occurrence; index++)
            start = query.IndexOf(text, start + 1, StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, start);
        return new TextSpan(start, text.Length);
    }

    public sealed record EnumDiagnosticCase(
        string CaseId,
        string Family,
        string Query,
        DiagnosticCode ExpectedCode,
        string ExpectedText,
        int Occurrence);
}
