using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class RawStringLiteralParserRecoveryTests
{
    [TestMethod]
    public void UnterminatedRawLiteral_InParserRecovery_ShouldReportOneDiagnosticAndRetainFollowingStatement()
    {
        const string query = "select r'not closed; select 1 from #system.dual()";
        var result = ParseWithRecovery(query);

        Assert.IsNotNull(result.Root, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics[0];
        var rawStart = query.IndexOf("r'", StringComparison.Ordinal);
        var semicolon = query.IndexOf(';');
        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, diagnostic.Code);
        Assert.AreEqual(new TextSpan(rawStart, semicolon - rawStart), diagnostic.Span);

        var statements = (StatementsArrayNode)result.Root!.Expression;
        Assert.HasCount(1, statements.Statements);
        var statement = (SingleSetNode)statements.Statements[0].Node;
        var value = (IntegerNode)statement.Query.Select.Fields[0].Expression;
        Assert.AreEqual(1, value.ObjValue);
    }

    [TestMethod]
    public void UnterminatedRawLiteral_AtCrLfBeforeSemicolon_ShouldRecoverToFollowingStatement()
    {
        const string query = "select r'not closed\r\n; select 2 from #system.dual()";
        var result = ParseWithRecovery(query);

        Assert.IsNotNull(result.Root, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, result.Diagnostics[0].Code);

        var statements = (StatementsArrayNode)result.Root!.Expression;
        Assert.HasCount(1, statements.Statements);
        var value = ((SingleSetNode)statements.Statements[0].Node).Query.Select.Fields[0].Expression;
        Assert.AreEqual(2, ((IntegerNode)value).ObjValue);
    }

    [TestMethod]
    public void ClosedRawLiteral_WithSemicolonContent_ShouldNotTriggerRecovery()
    {
        const string query = "select r'a;b' from #system.dual(); select 2 from #system.dual()";
        var result = ParseWithRecovery(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
        Assert.HasCount(2, ((StatementsArrayNode)result.Root!.Expression).Statements);
    }

    [TestMethod]
    public void UnterminatedRawLiteral_InStrictLexerMode_ShouldThrowMQ1002()
    {
        var exception = Assert.ThrowsExactly<LexerException>(() => new Lexer("r'C:\\Temp", true).Next());

        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, exception.Code);
    }

    [TestMethod]
    public void UppercaseRawEmptyLiteral_ShouldPreserveValueAndFullSpan()
    {
        var token = new Lexer("R''", true).Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(string.Empty, token.Value);
        Assert.AreEqual(new TextSpan(0, 3), token.Span);
    }

    [TestMethod]
    public void UppercaseSeparatedPrefix_ShouldRemainIdentifierAndOrdinaryString()
    {
        var lexer = new Lexer("R 'value'", true);

        var identifier = lexer.Next();
        var literal = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, identifier.TokenType);
        Assert.AreEqual("R", identifier.Value);
        Assert.AreEqual(TokenType.StringLiteral, literal.TokenType);
        Assert.AreEqual("value", literal.Value);
    }

    private static ParseResult ParseWithRecovery(string query)
    {
        var diagnostics = new DiagnosticBag();
        return new Parser(new Lexer(query, true, true), diagnostics).ParseWithDiagnostics();
    }
}
