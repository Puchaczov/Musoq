using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class NamedDatasourceArgumentParserTests
{
    [TestMethod]
    public void DirectSchemaCall_ParsesReorderedAndMixedArguments()
    {
        var source = GetSchemaSource(
            "select a.Value from #schema.method('first', middle: 2, THIRD: 3) a");

        Assert.IsTrue(source.Parameters.HasNamedArguments);
        Assert.AreEqual("'first', middle: 2, THIRD: 3", source.Parameters.ToString());
        Assert.IsNull(source.Parameters.ArgumentNames[0]);
        Assert.AreEqual("middle", source.Parameters.ArgumentNames[1]!.Value.Name);
        Assert.AreEqual("THIRD", source.Parameters.ArgumentNames[2]!.Value.Name);
    }

    [TestMethod]
    public void SchemaCall_RejectsPositionalArgumentAfterNamedArgument()
    {
        var exception = Assert.Throws<SyntaxException>(() =>
            new Parser(new Lexer("select a.Value from #schema.method(first: 1, 2) a", true)).ComposeAll());

        Assert.AreEqual(DiagnosticCode.MQ2034_InvalidNamedSourceArgument, exception.Code);
    }

    [TestMethod]
    public void ScalarFunctionCall_RejectsNamedArguments()
    {
        var exception = Assert.Throws<SyntaxException>(() =>
            new Parser(new Lexer("select Length(value: 1) from #system.dual() d", true)).ComposeAll());

        Assert.AreEqual(DiagnosticCode.MQ2034_InvalidNamedSourceArgument, exception.Code);
    }

    [TestMethod]
    public void RowAccessMethodInApply_RejectsNamedArguments()
    {
        var exception = Assert.Throws<SyntaxException>(() =>
            new Parser(new Lexer(
                "select b.Value from #schema.first() a cross apply a.Split(value: 'x') b", true)).ComposeAll());

        Assert.AreEqual(DiagnosticCode.MQ2034_InvalidNamedSourceArgument, exception.Code);
    }

    [TestMethod]
    public void DescFunctions_RejectsNamedArguments()
    {
        var exception = Assert.Throws<SyntaxException>(() =>
            new Parser(new Lexer("desc functions #schema.method(value: 1)", true)).ComposeAll());

        Assert.AreEqual(DiagnosticCode.MQ2034_InvalidNamedSourceArgument, exception.Code);
    }

    [TestMethod]
    public void NamedArgument_MissingValueReportsParserDiagnostic()
    {
        var exception = Assert.Throws<SyntaxException>(() =>
            new Parser(new Lexer("select 1 from #schema.method(value: )", true)).ComposeAll());

        Assert.AreEqual(DiagnosticCode.MQ2034_InvalidNamedSourceArgument, exception.Code);
    }

    [TestMethod]
    public void NamedArguments_AllowCommentsAndMultilineWhitespace()
    {
        var source = GetSchemaSource(
            "select 1 from #schema.method(\n  /* first */ value: 1,\n  other: 2\n) a");

        Assert.IsTrue(source.Parameters.HasNamedArguments);
        Assert.AreEqual("value", source.Parameters.ArgumentNames[0]!.Value.Name);
    }

    private static SchemaFromNode GetSchemaSource(string query)
    {
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(root.Expression);
        var singleSet = Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[0].Node);
        var queryNode = singleSet.Query;
        var expressionFrom = Assert.IsInstanceOfType<ExpressionFromNode>(queryNode.From);
        return Assert.IsInstanceOfType<SchemaFromNode>(expressionFrom.Expression);
    }
}
