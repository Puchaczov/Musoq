using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore003NumericLiteralTests
{
    [TestMethod]
    public void NumericLiteralBoundariesAndSuffixes_ShouldInferDocumentedTypes()
    {
        const string query =
            "select 0, 2147483647, -2147483648, .5d, 1.25, 1d, 1ub, 1UL, 0XFF, 0B1010, 0O77 from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var expressions = GetSelectExpressions(result);
        CollectionAssert.AreEqual(
            new[]
            {
                typeof(int), typeof(int), typeof(int), typeof(decimal), typeof(decimal), typeof(decimal),
                typeof(byte), typeof(ulong), typeof(long), typeof(long), typeof(long)
            },
            expressions.Select(expression => expression.ReturnType).ToArray());
        Assert.AreEqual(255L, ((ConstantValueNode)expressions[8]).ObjValue);
        Assert.AreEqual(10L, ((ConstantValueNode)expressions[9]).ObjValue);
        Assert.AreEqual(63L, ((ConstantValueNode)expressions[10]).ObjValue);
    }

    [TestMethod]
    public void LeadingDotDecimalWithSuffix_ShouldExcludeTheSuffixFromItsValue()
    {
        var token = new Lexer(".5D", true).Next();

        Assert.AreEqual(TokenType.Decimal, token.TokenType);
        Assert.AreEqual(".5", token.Value);
        Assert.AreEqual(new TextSpan(0, 3), token.Span);
    }

    [TestMethod]
    public void SignedAlternativeBaseLiterals_ShouldPreserveTheUnaryOperator()
    {
        var lexer = new Lexer("-0xFF -0b101 -0o77", true);

        Assert.AreEqual(TokenType.Hyphen, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.HexadecimalInteger, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.Hyphen, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.BinaryInteger, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.Hyphen, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.OctalInteger, lexer.Next().TokenType);

        var result = ParseWithDiagnostics("select -0xFF, -0b101, -0o77 from system.dual()");
        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        var expressions = GetSelectExpressions(result);
        var first = (StarNode)expressions[0];
        Assert.AreEqual((short)-1, ((ConstantValueNode)first.Left).ObjValue);
        Assert.AreEqual(255L, ((ConstantValueNode)first.Right).ObjValue);
    }

    [TestMethod]
    [DataRow("0x1G", DiagnosticCode.MQ1006_InvalidHexNumber)]
    [DataRow("0b102", DiagnosticCode.MQ1007_InvalidBinaryNumber)]
    [DataRow("0o78", DiagnosticCode.MQ1008_InvalidOctalNumber)]
    public void InvalidAlternativeBaseLiteral_ShouldReportTheWholeLiteralWithGuidance(
        string literal,
        DiagnosticCode expectedCode)
    {
        var query = $"select {literal} from system.dual()";
        var lexer = new Lexer(query, true, recoverOnError: true);

        Drain(lexer);

        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf(literal, StringComparison.Ordinal), literal.Length),
            diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void InvalidDecimalWithRepeatedPoint_ShouldReportMQ1003WithExactSpan()
    {
        const string literal = "1..2";
        var query = $"select {literal} from system.dual()";
        var lexer = new Lexer(query, true, recoverOnError: true);

        Drain(lexer);

        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ1003_InvalidNumericLiteral, diagnostic.Code);
        Assert.AreEqual(new TextSpan(query.IndexOf(literal, StringComparison.Ordinal), literal.Length),
            diagnostic.Span);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    [DataRow("2147483648")]
    [DataRow("128b")]
    public void NumericOverflow_ShouldReportMQ1009ForTheCompleteLiteral(string literal)
    {
        var query = $"select {literal} from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success);
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ1009_NumericLiteralOutOfRange, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf(literal, StringComparison.Ordinal), literal.Length),
            diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static Node[] GetSelectExpressions(ParseResult result)
    {
        var statements = (StatementsArrayNode)result.Root!.Expression;
        var statement = (SingleSetNode)statements.Statements.Single().Node;
        return statement.Query.Select.Fields.Select(field => field.Expression).ToArray();
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static void Drain(Lexer lexer)
    {
        while (lexer.Next().TokenType != TokenType.EndOfFile)
        {
        }
    }
}
