using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ImplicitElseParserTests : SchemaParserTestsBase
{
    [TestMethod]
    [DataRow("select case when Flag = 1 then 7 end from #test.rows()")]
    [DataRow("select case Flag when 1 then 7 end from #test.rows()")]
    public void OmittedElse_ShouldNormalizeToNullAtEnd(string sql)
    {
        var expression = ParseCase(sql);
        var otherwise = (ElseNode)expression.Else;
        var missing = (NullNode)otherwise.Expression;
        var expected = new TextSpan(sql.IndexOf("end", StringComparison.Ordinal), 0);

        Assert.AreEqual(expected, missing.Span);
        Assert.AreEqual(expected, otherwise.Span);
        Assert.IsInstanceOfType<NullNode.NullType>(missing.ReturnType);
        Assert.HasCount(1, expression.WhenThenPairs);
    }

    [TestMethod]
    public void NestedOmittedElse_ShouldAnchorEachNullAtItsOwnEnd()
    {
        const string sql = "select case when Flag = 1 then case when Other = 2 then 7 end end from #test.rows()";
        var outer = ParseCase(sql);
        var inner = (CaseNode)((ThenNode)outer.WhenThenPairs[0].Then).Expression;

        Assert.AreEqual(new TextSpan(sql.IndexOf("end", StringComparison.Ordinal), 0),
            ((ElseNode)inner.Else).Expression.Span);
        Assert.AreEqual(new TextSpan(sql.LastIndexOf("end", StringComparison.Ordinal), 0),
            ((ElseNode)outer.Else).Expression.Span);
    }

    [TestMethod]
    [DataRow("CASE WHEN Flag = 1 THEN 7 END")]
    [DataRow("CASE Flag WHEN 1 THEN 7 END")]
    public void EmbeddedSchemaCase_ShouldUseTheSameSyntheticNullSpan(string expression)
    {
        var sql = $"binary Rec {{ Flag: byte, Derived: {expression} }}";
        var schema = ParseBinarySchema(sql);
        var conditional = (CaseNode)((ComputedFieldNode)schema.Fields[1]).Expression;
        var otherwise = (ElseNode)conditional.Else;

        Assert.IsInstanceOfType<NullNode>(otherwise.Expression);
        Assert.AreEqual(new TextSpan(sql.IndexOf("END", StringComparison.Ordinal), 0), otherwise.Expression.Span);
        Assert.AreEqual(otherwise.Expression.Span, otherwise.Span);
    }

    [TestMethod]
    [DataRow("select case 1 end from #test.rows()")]
    [DataRow("select case 1 else 2 end from #test.rows()")]
    public void OptionalElse_ShouldStillRequireAWhenBranch(string sql)
    {
        var error = Assert.Throws<SyntaxException>(() => ParseCase(sql));

        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, error.Code);
    }

    private static CaseNode ParseCase(string sql)
    {
        var root = new Parser(new Lexer(sql, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var query = ((SingleSetNode)statements.Statements[0].Node).Query;
        return (CaseNode)query.Select.Fields[0].Expression;
    }
}
