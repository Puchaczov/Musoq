using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticREC099RepresentabilityBoundaryTests
{
    private static readonly EnumDiagnosticCase[] CandidateCases =
    [
        new("REC-099-MAX01", "max-overflow", "byte", "enum EByte : byte { Value = 256 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "256", false),
        new("REC-099-MAX02", "max-overflow", "sbyte", "enum ESByte : sbyte { Value = 128 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "128", false),
        new("REC-099-MAX03", "max-overflow", "short", "enum EShort : short { Value = 32768 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "32768", false),
        new("REC-099-MAX04", "max-overflow", "ushort", "enum EUShort : ushort { Value = 65536 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "65536", false),
        new("REC-099-MAX05", "max-overflow", "int", "enum EInt : int { Value = 2147483648 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "2147483648", false),
        new("REC-099-MAX06", "max-overflow", "uint", "enum EUInt : uint { Value = 4294967296 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "4294967296", false),
        new("REC-099-MAX07", "max-overflow", "long", "enum ELong : long { Value = 9223372036854775808 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "9223372036854775808", false),
        new("REC-099-MAX08", "max-overflow", "ulong", "enum EULong : ulong { Value = 18446744073709551616 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "18446744073709551616", false),

        new("REC-099-MIN01", "min-underflow", "byte", "enum EByte : byte { Value = -1 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-1", false),
        new("REC-099-MIN02", "min-underflow", "sbyte", "enum ESByte : sbyte { Value = -129 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-129", false),
        new("REC-099-MIN03", "min-underflow", "short", "enum EShort : short { Value = -32769 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-32769", false),
        new("REC-099-MIN04", "min-underflow", "ushort", "enum EUShort : ushort { Value = -1 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-1", false),
        new("REC-099-MIN05", "min-underflow", "int", "enum EInt : int { Value = -2147483649 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-2147483649", false),
        new("REC-099-MIN06", "min-underflow", "uint", "enum EUInt : uint { Value = -1 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-1", false),
        new("REC-099-MIN07", "min-underflow", "long", "enum ELong : long { Value = -9223372036854775809 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-9223372036854775809", false),
        new("REC-099-MIN08", "min-underflow", "ulong", "enum EULong : ulong { Value = -1 };", DiagnosticCode.MQ2046_EnumMemberValueOutOfRange, "-1", false),

        new("REC-099-LIT01", "literal-forms", "decimal", "enum EDecimal : decimal { Value = 1 };", DiagnosticCode.MQ2043_InvalidEnumBackingType, "decimal", false),
        new("REC-099-LIT02", "literal-forms", "int", "enum EMissing : int { Value };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "Value", false),
        new("REC-099-LIT03", "literal-forms", "int", "enum EDecimalValue : int { Value = 1.5 };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "1.5", false),
        new("REC-099-LIT04", "literal-forms", "ulong", "enum EHex : ulong { Value = 0x1G };", DiagnosticCode.MQ1006_InvalidHexNumber, "0x1G", true),
        new("REC-099-LIT05", "literal-forms", "ulong", "enum EBinary : ulong { Value = 0b102 };", DiagnosticCode.MQ1007_InvalidBinaryNumber, "0b102", true),
        new("REC-099-LIT06", "literal-forms", "ulong", "enum EOctal : ulong { Value = 0o78 };", DiagnosticCode.MQ1008_InvalidOctalNumber, "0o78", true),
        new("REC-099-LIT07", "literal-forms", "int", "enum EBaseSuffix : int { Value = 0x10ui };", DiagnosticCode.MQ1006_InvalidHexNumber, "0x10ui", true),
        new("REC-099-LIT08", "literal-forms", "int", "enum EDecimalSuffix : int { Value = 1d };", DiagnosticCode.MQ2044_MissingEnumMemberValue, "1d", false)
    ];

    public static IEnumerable<object[]> CandidateData()
    {
        foreach (var testCase in CandidateCases)
            yield return [testCase];
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainEightCasesPerBoundaryFamily()
    {
        Assert.HasCount(24, CandidateCases);
        Assert.AreEqual(24, CandidateCases.Select(static testCase => testCase.CaseId).Distinct(StringComparer.Ordinal).Count());
        CollectionAssert.AreEquivalent(
            new[] { "literal-forms:8", "max-overflow:8", "min-underflow:8" },
            CandidateCases
                .GroupBy(static testCase => testCase.Family, StringComparer.Ordinal)
                .Select(static group => $"{group.Key}:{group.Count()}")
                .ToArray());
        CollectionAssert.AreEquivalent(
            new[] { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong" },
            CandidateCases
                .Where(static testCase => testCase.Family == "max-overflow")
                .Select(static testCase => testCase.BackingType)
                .ToArray());
        Assert.AreEqual(16, CandidateCases.Count(static testCase => testCase.ExpectedCode == DiagnosticCode.MQ2046_EnumMemberValueOutOfRange));
    }

    [TestMethod]
    [DynamicData(nameof(CandidateData))]
    public void EnumDeclarationDiagnostics_ShouldDistinguishRepresentabilityAndLiteralFaults(EnumDiagnosticCase testCase)
    {
        Diagnostic diagnostic;
        if (testCase.LexerOnly)
        {
            var lexer = new Lexer(testCase.Query, true, recoverOnError: true);
            while (lexer.Next().TokenType != TokenType.EndOfFile)
            {
            }

            Assert.HasCount(1, lexer.Diagnostics, testCase.CaseId);
            diagnostic = lexer.Diagnostics.ToSortedList().Single();
        }
        else
        {
            var result = ParseWithDiagnostics(testCase.Query);
            Assert.IsFalse(result.Success, testCase.CaseId + ": " + result.FormatDiagnostics());
            Assert.HasCount(1, result.Diagnostics, testCase.CaseId + ": " + result.FormatDiagnostics());
            diagnostic = result.Diagnostics.Single();
        }

        Assert.AreEqual(testCase.ExpectedCode, diagnostic.Code, testCase.CaseId);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, testCase.CaseId);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase, testCase.CaseId);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, testCase.CaseId);
        Assert.AreEqual(SpanOf(testCase.Query, testCase.ExpectedText), diagnostic.Span, testCase.CaseId);
        Assert.IsTrue(diagnostic.Location.IsValid, testCase.CaseId);
        Assert.IsTrue(diagnostic.EndLocation.IsValid, testCase.CaseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation), testCase.CaseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference), testCase.CaseId);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes, testCase.CaseId);
        Assert.IsNotNull(diagnostic.ContextSnippet, testCase.CaseId);
    }

    [TestMethod]
    public void ValidAllBackingBoundaries_ShouldRemainRepresentable()
    {
        const string query =
            "enum EByte : byte { Min = 0ub, Max = 255ub, Zero = 0ub };" +
            "enum ESByte : sbyte { Min = -128, Max = 127b, Zero = 0 };" +
            "enum EShort : short { Min = -32768, Max = 32767s, Zero = 0s };" +
            "enum EUShort : ushort { Min = 0us, Max = 65535us, Zero = 0us };" +
            "enum EInt : int { Min = -2147483648, Max = 2147483647i, Zero = 0i };" +
            "enum EUInt : uint { Min = 0ui, Max = 4294967295ui, Zero = 0ui };" +
            "enum ELong : long { Min = -9223372036854775808, Max = 9223372036854775807l, Zero = 0l };" +
            "enum EULong : ulong { Min = 0ul, Max = 18446744073709551615ul, Zero = 0ul };";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.HasCount(0, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var statements = (StatementsArrayNode)result.Root.Expression;
        Assert.HasCount(8, statements.Statements);
        Assert.IsTrue(statements.Statements.All(static statement => statement.Node is EnumDeclarationNode));
    }

    [TestMethod]
    public void NumericSuffixAndAlternateBaseForms_ShouldRemainValidWhenValueFits()
    {
        const string query =
            "enum Values : byte { LongValue = 1l, UnsignedLongValue = 1ul, Hex = 0x10, Binary = 0b10, Octal = 0o7, Native = 1ub };";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.HasCount(0, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var declaration = (EnumDeclarationNode)((StatementsArrayNode)result.Root.Expression).Statements.Single().Node;
        CollectionAssert.AreEqual(
            new ulong[] { 1, 1, 16, 2, 7, 1 },
            declaration.Members.Select(static member => member.RawValue).ToArray());
    }

    [TestMethod]
    public void FlagsAliasesAndTrailingComma_ShouldRemainValid()
    {
        const string query = "flags enum Access : uint { None = 0ui, Read = 1ui, Alias = 1ui, ReadWrite = 3ui, };";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.HasCount(0, result.Diagnostics, result.FormatDiagnostics());
        Assert.IsNotNull(result.Root);
        var declaration = (EnumDeclarationNode)((StatementsArrayNode)result.Root.Expression).Statements.Single().Node;
        Assert.IsTrue(declaration.IsFlags);
        Assert.AreEqual(3ul, declaration.Members[^1].RawValue);
        Assert.AreEqual(declaration.Members[1].RawValue, declaration.Members[2].RawValue);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        return new TextSpan(start, text.Length);
    }

    public sealed record EnumDiagnosticCase(
        string CaseId,
        string Family,
        string BackingType,
        string Query,
        DiagnosticCode ExpectedCode,
        string ExpectedText,
        bool LexerOnly);
}
