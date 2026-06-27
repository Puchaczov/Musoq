using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserDiagnosticCommandTests
{
    [TestMethod]
    public void Parser_ProfileCommand_ShouldCreateDiagnosticCommandNode()
    {
        const string query = "profile select 1 from #test.rows()";

        var command = ParseSingleDiagnosticCommand(query);

        Assert.AreEqual(DiagnosticCommandKind.Profile, command.Kind);
        Assert.AreEqual(query.IndexOf("profile", StringComparison.Ordinal), command.CommandStart);
        Assert.AreEqual(query.IndexOf("select", StringComparison.Ordinal), command.InnerStart);
        Assert.AreEqual("select 1 from #test.rows()", command.InnerQueryText);
    }

    [TestMethod]
    public void Parser_ExplainAnalyzeCommand_ShouldCreateDiagnosticCommandNodeWithCaseInsensitiveWords()
    {
        const string query = "ExPlAiN AnAlYzE from #test.rows() select 1";

        var command = ParseSingleDiagnosticCommand(query);

        Assert.AreEqual(DiagnosticCommandKind.ExplainAnalyze, command.Kind);
        Assert.AreEqual(query.IndexOf("ExPlAiN", StringComparison.Ordinal), command.CommandStart);
        Assert.AreEqual(query.IndexOf("from", StringComparison.Ordinal), command.InnerStart);
        Assert.AreEqual("from #test.rows() select 1", command.InnerQueryText);
    }

    [TestMethod]
    public void Parser_DiagnosticCommandAfterParams_ShouldKeepCommandAsSecondStatement()
    {
        const string query = "param(expected: string); PROFILE select d.Dummy from #system.dual() d";

        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;

        Assert.HasCount(2, statements.Statements);
        Assert.IsInstanceOfType<ParameterBlockNode>(statements.Statements[0].Node);

        var command = (DiagnosticCommandNode)statements.Statements[1].Node;
        Assert.AreEqual(DiagnosticCommandKind.Profile, command.Kind);
        Assert.AreEqual(query.IndexOf("PROFILE", StringComparison.Ordinal), command.CommandStart);
        Assert.AreEqual(query.IndexOf("select", StringComparison.Ordinal), command.InnerStart);
    }

    [TestMethod]
    public void Parser_DiagnosticCommand_ShouldSupportPivotAsInnerForm()
    {
        const string query = "profile pivot #sales.orders() on Quarter in ('Q1') using Count(Amount)";

        var command = ParseSingleDiagnosticCommand(query);

        Assert.AreEqual(DiagnosticCommandKind.Profile, command.Kind);
        Assert.AreEqual(query.IndexOf("pivot", StringComparison.Ordinal), command.InnerStart);
    }

    [TestMethod]
    public void Parser_ExplainWithoutAnalyze_ShouldReportUnsupportedSyntax()
    {
        var exception = Assert.Throws<SyntaxException>(() => Parse("explain select 1 from #test.rows()"));

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, exception.Code);
        StringAssert.Contains(exception.Message, "EXPLAIN without ANALYZE is not supported");
    }

    [TestMethod]
    public void Parser_StandaloneAnalyze_ShouldReportUnsupportedSyntax()
    {
        var exception = Assert.Throws<SyntaxException>(() => Parse("analyze select 1 from #test.rows()"));

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, exception.Code);
        StringAssert.Contains(exception.Message, "Standalone ANALYZE is not implemented");
    }

    [TestMethod]
    public void Parser_DiagnosticCommandWithoutInnerQuery_ShouldReportRequiredInnerForm()
    {
        var exception = Assert.Throws<SyntaxException>(() => Parse("profile"));

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, exception.Code);
        StringAssert.Contains(exception.Message, "Diagnostic command requires an inner SELECT, FROM, WITH, PIVOT, or UNPIVOT query");
    }

    [TestMethod]
    public void Parser_DiagnosticCommandWithUnsupportedInnerForm_ShouldReportSupportedForms()
    {
        var exception = Assert.Throws<SyntaxException>(() => Parse("profile table temp {Name: string}"));

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, exception.Code);
        StringAssert.Contains(exception.Message, "Diagnostic command does not support inner query starting with 'table'");
    }

    private static DiagnosticCommandNode ParseSingleDiagnosticCommand(string query)
    {
        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;

        Assert.HasCount(1, statements.Statements);
        return (DiagnosticCommandNode)statements.Statements[0].Node;
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }
}
