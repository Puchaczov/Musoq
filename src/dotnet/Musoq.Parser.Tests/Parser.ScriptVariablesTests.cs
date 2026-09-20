using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserScriptVariablesTests
{
    [TestMethod]
    public void Parser_LetDeclaration_ShouldParseAsStatement()
    {
        const string query = "let topic: string = 'important'; select $topic from #test.rows()";

        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;

        Assert.HasCount(2, statements.Statements);

        var declaration = (ScriptVariableDeclarationNode)statements.Statements[0].Node;
        Assert.AreEqual("topic", declaration.Name);
        Assert.AreEqual("string", declaration.TypeName);
        Assert.AreEqual("string", declaration.DeclaredTypeName);
        Assert.IsInstanceOfType<WordNode>(declaration.Initializer);
        Assert.AreEqual("important", ((WordNode)declaration.Initializer).Value);
    }

    [TestMethod]
    public void Parser_LetDeclarationWithNullableType_ShouldPreserveDeclaredType()
    {
        const string query = "let limit: int? = null; select $limit from #test.rows()";

        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;
        var declaration = (ScriptVariableDeclarationNode)statements.Statements[0].Node;

        Assert.AreEqual("limit", declaration.Name);
        Assert.AreEqual("int", declaration.TypeName);
        Assert.IsTrue(declaration.IsNullable);
        Assert.AreEqual("int?", declaration.DeclaredTypeName);
        Assert.IsInstanceOfType<NullNode>(declaration.Initializer);
    }

    [TestMethod]
    public void Parser_LetReference_ShouldParseAsParameterReferenceExpression()
    {
        const string query = "let topic: string = 'important'; select $topic from #test.rows()";

        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;
        var queryNode = ((SingleSetNode)statements.Statements[1].Node).Query;

        Assert.IsInstanceOfType<ParameterReferenceNode>(queryNode.Select.Fields[0].Expression);
        Assert.AreEqual("topic", ((ParameterReferenceNode)queryNode.Select.Fields[0].Expression).Name);
    }

    [TestMethod]
    [DataRow(
        "let string topic = 'important'; select 1 from #test.rows()",
        "string topic",
        DisplayName = "C# style order")]
    [DataRow(
        "let topic: string; select 1 from #test.rows()",
        "topic: string;",
        DisplayName = "missing initializer")]
    [DataRow(
        "let $topic: string = 'important'; select 1 from #test.rows()",
        "$topic",
        DisplayName = "PowerShell style name")]
    public void Parser_InvalidLetDeclaration_ShouldReportStructuredDiagnostic(
        string query,
        string offendingText)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);

        var start = query.IndexOf(offendingText, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        Assert.AreEqual(new TextSpan(start, offendingText.Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }
}
