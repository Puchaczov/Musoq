using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticRework076NumericLiteralContractTests
{
    [TestMethod]
    public void IntegerSuffixMatrix_ShouldInferEveryDocumentedClrType()
    {
        const string query =
            "select 1, 127b, 255ub, 32767s, 65535us, 42i, 4294967295ui, " +
            "9223372036854775807l, 18446744073709551615ul from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var expressions = GetSelectExpressions(result);
        CollectionAssert.AreEqual(
            new[]
            {
                typeof(int), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long), typeof(ulong)
            },
            expressions.Select(static expression => expression.ReturnType).ToArray());
        CollectionAssert.AreEqual(
            new object[]
            {
                1, (sbyte)127, (byte)255, (short)32767, (ushort)65535, 42,
                (uint)4294967295, long.MaxValue, ulong.MaxValue
            },
            expressions.Select(GetConstantValue).ToArray());
    }

    [TestMethod]
    public void DecimalMatrix_ShouldUseDecimalForPointAndDecimalSuffixForms()
    {
        const string query =
            "select 42d, 42D, 3.14, 1.0d, .5, .5D, " +
            "79228162514264337593543950335d from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var expressions = GetSelectExpressions(result);
        CollectionAssert.AreEqual(
            Enumerable.Repeat(typeof(decimal), expressions.Length).ToArray(),
            expressions.Select(static expression => expression.ReturnType).ToArray());
        CollectionAssert.AreEqual(
            new object[]
            {
                42m, 42m, 3.14m, 1.0m, .5m, .5m,
                79228162514264337593543950335m
            },
            expressions.Select(GetConstantValue).ToArray());
    }

    [TestMethod]
    public void SignedBoundaryMatrix_ShouldAcceptDocumentedNegativeRanges()
    {
        const string query =
            "select -128b, -32768s, -2147483648, -2147483648i, " +
            "-9223372036854775808l, -1.5, -.5 from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var expressions = GetSelectExpressions(result);
        CollectionAssert.AreEqual(
            new[]
            {
                typeof(sbyte), typeof(short), typeof(int), typeof(int), typeof(long),
                typeof(decimal), typeof(decimal)
            },
            expressions.Select(static expression => expression.ReturnType).ToArray());
        CollectionAssert.AreEqual(
            new object[]
            {
                (sbyte)-128, (short)-32768, -2147483648, -2147483648,
                long.MinValue, -1.5m, -.5m
            },
            expressions.Select(GetConstantValue).ToArray());
    }

    [TestMethod]
    public void AlternativeBaseMatrix_ShouldProduceLongValuesAndFullSourceSpans()
    {
        var literals = new[] { "0x0", "0XFF", "0b0", "0B1010", "0o0", "0O77" };
        var input = string.Join(" ", literals);
        var lexer = new Lexer(input, true, recoverOnError: true);

        var expectedTypes = new[]
        {
            TokenType.HexadecimalInteger, TokenType.HexadecimalInteger,
            TokenType.BinaryInteger, TokenType.BinaryInteger,
            TokenType.OctalInteger, TokenType.OctalInteger
        };
        var expectedValues = new[] { 0L, 255L, 0L, 10L, 0L, 63L };
        var offset = 0;

        for (var i = 0; i < literals.Length; i++)
        {
            var token = lexer.Next();
            Assert.AreEqual(expectedTypes[i], token.TokenType, literals[i]);
            Assert.AreEqual(literals[i], token.Value);
            Assert.AreEqual(new TextSpan(offset, literals[i].Length), token.Span, literals[i]);
            Assert.AreEqual(expectedValues[i], ((ConstantValueNode)ParseTokenNode(token)).ObjValue, literals[i]);
            offset += literals[i].Length + 1;
        }

        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void AlternativeBaseQuery_ShouldPreserveLongArithmeticValues()
    {
        const string query =
            "select 0xFF, 0b1010, 0o77, 0xFF + 0b1010 + 0o77 + 42 from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var expressions = GetSelectExpressions(result);
        CollectionAssert.AreEqual(
            new[] { typeof(long), typeof(long), typeof(long), typeof(long) },
            expressions.Select(static expression => expression.ReturnType).ToArray());
        CollectionAssert.AreEqual(
            new object[] { 255L, 10L, 63L },
            expressions.Take(3).Select(GetConstantValue).ToArray());
        Assert.AreEqual(370L, EvaluateConstantLong(expressions[3]));
    }

    [TestMethod]
    public void SuffixCasing_ShouldPreserveSourceSpanAndNormalizeIntegerAbbreviation()
    {
        const string input = "1UB 2Us 3UI 4UL";
        var lexer = new Lexer(input, true, recoverOnError: true);
        var expected = new[] { ("1", "UB"), ("2", "US"), ("3", "UI"), ("4", "UL") };
        var offset = 0;

        foreach (var (value, abbreviation) in expected)
        {
            var token = (IntegerToken)lexer.Next();
            Assert.AreEqual(TokenType.Integer, token.TokenType);
            Assert.AreEqual(value, token.Value);
            Assert.AreEqual(abbreviation, token.Abbreviation);
            Assert.AreEqual(new TextSpan(offset, value.Length + abbreviation.Length), token.Span);
            offset += value.Length + abbreviation.Length + 1;
        }

        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void NumericAliases_ShouldRequireWhitespaceWithoutRejectingValidAliases()
    {
        const string query =
            "select 1 u, 1.0 f, 0x10 hexValue, 1ub byteValue from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
        CollectionAssert.AreEqual(
            new[] { typeof(int), typeof(decimal), typeof(long), typeof(byte) },
            GetSelectExpressions(result).Select(static expression => expression.ReturnType).ToArray());
    }

    [TestMethod]
    [DataRow("0x", DiagnosticCode.MQ1006_InvalidHexNumber)]
    [DataRow("0xG", DiagnosticCode.MQ1006_InvalidHexNumber)]
    [DataRow("0x1G", DiagnosticCode.MQ1006_InvalidHexNumber)]
    [DataRow("0x1l", DiagnosticCode.MQ1006_InvalidHexNumber)]
    [DataRow("0b", DiagnosticCode.MQ1007_InvalidBinaryNumber)]
    [DataRow("0b2", DiagnosticCode.MQ1007_InvalidBinaryNumber)]
    [DataRow("0b1010l", DiagnosticCode.MQ1007_InvalidBinaryNumber)]
    [DataRow("0o", DiagnosticCode.MQ1008_InvalidOctalNumber)]
    [DataRow("0o8", DiagnosticCode.MQ1008_InvalidOctalNumber)]
    [DataRow("0o77ul", DiagnosticCode.MQ1008_InvalidOctalNumber)]
    public void InvalidAlternativeBaseLiterals_ShouldReportTheCompleteLiteralWithStructuredGuidance(
        string literal,
        DiagnosticCode expectedCode)
    {
        var query = $"select {literal} from system.dual()";
        var lexer = new Lexer(query, true, recoverOnError: true);

        Drain(lexer);

        var diagnostics = lexer.Diagnostics.ToSortedList();
        Assert.HasCount(1, diagnostics);
        var diagnostic = diagnostics[0];
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            new TextSpan(query.IndexOf(literal, StringComparison.Ordinal), literal.Length),
            diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, literal);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    [DataRow("1..2")]
    [DataRow("1...2")]
    public void RepeatedDecimalPoints_ShouldReportOneExactNumericDiagnostic(string literal)
    {
        var query = $"select {literal} from system.dual()";
        var lexer = new Lexer(query, true, recoverOnError: true);

        Drain(lexer);

        var diagnostics = lexer.Diagnostics.ToSortedList();
        Assert.HasCount(1, diagnostics);
        var diagnostic = diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ1003_InvalidNumericLiteral, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            new TextSpan(query.IndexOf(literal, StringComparison.Ordinal), literal.Length),
            diagnostic.Span);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    [DataRow("2147483648")]
    [DataRow("128b")]
    [DataRow("256ub")]
    [DataRow("32768s")]
    [DataRow("65536us")]
    [DataRow("4294967296ui")]
    [DataRow("9223372036854775808l")]
    [DataRow("18446744073709551616ul")]
    [DataRow("79228162514264337593543950336d")]
    [DataRow("0x10000000000000000")]
    public void NumericOverflow_ShouldReportTheCompleteLiteralWithActionableGuidance(string literal)
    {
        var query = $"select {literal} from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ1009_NumericLiteralOutOfRange, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            new TextSpan(query.IndexOf(literal, StringComparison.Ordinal), literal.Length),
            diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, literal);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    [DataRow("1u")]
    [DataRow("1uu")]
    [DataRow("1ubx")]
    [DataRow("1.0f")]
    public void UnsupportedNumericSuffixes_ShouldNotBecomeImplicitAliases(string literal)
    {
        var query = $"select {literal} from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ1003_InvalidNumericLiteral, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            new TextSpan(query.IndexOf(literal, StringComparison.Ordinal), literal.Length),
            diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, literal);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static Node[] GetSelectExpressions(ParseResult result)
    {
        var statements = (StatementsArrayNode)result.Root!.Expression;
        var statement = (SingleSetNode)statements.Statements.Single().Node;
        return statement.Query.Select.Fields.Select(static field => field.Expression).ToArray();
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static object GetConstantValue(Node expression)
    {
        if (expression is ConstantValueNode constant)
            return constant.ObjValue;

        if (expression is StarNode { Left: ConstantValueNode left, Right: ConstantValueNode right })
        {
            return Convert.ToDecimal(left.ObjValue, CultureInfo.InvariantCulture) *
                   Convert.ToDecimal(right.ObjValue, CultureInfo.InvariantCulture);
        }

        Assert.Fail($"Expected a constant numeric expression but got {expression.GetType().Name}.");
        return null!;
    }

    private static long EvaluateConstantLong(Node expression)
    {
        return expression switch
        {
            ConstantValueNode constant => Convert.ToInt64(constant.ObjValue, CultureInfo.InvariantCulture),
            AddNode add => EvaluateConstantLong(add.Left) + EvaluateConstantLong(add.Right),
            _ => throw new AssertFailedException(
                $"Expected a constant integer expression but got {expression.GetType().Name}.")
        };
    }

    private static Node ParseTokenNode(Token token)
    {
        return token switch
        {
            HexIntegerToken hex => new HexIntegerNode(hex.Value, hex.Span),
            BinaryIntegerToken binary => new BinaryIntegerNode(binary.Value, binary.Span),
            OctalIntegerToken octal => new OctalIntegerNode(octal.Value, octal.Span),
            _ => throw new AssertFailedException($"Unexpected token type {token.TokenType}.")
        };
    }

    private static void Drain(Lexer lexer)
    {
        while (lexer.Next().TokenType != TokenType.EndOfFile)
        {
        }
    }
}
