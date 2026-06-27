using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class GroupByAllParsingTests
{
    [TestMethod]
    public void Parse_WhenGroupByAll_ShouldCreateAllGroupByNode()
    {
        var query = ParseSingleQuery("select Col, Count(Col) from schema.method() group by all");

        Assert.IsNotNull(query.GroupBy);
        Assert.IsTrue(query.GroupBy.IsAll);
        Assert.HasCount(0, query.GroupBy.Fields);
        Assert.IsNull(query.GroupBy.Having);
    }

    [TestMethod]
    public void Parse_WhenGroupByAllWithHaving_ShouldKeepHaving()
    {
        var query = ParseSingleQuery(
            "select Col, Count(Col) from schema.method() group by all having Count(Col) > 1");

        Assert.IsNotNull(query.GroupBy);
        Assert.IsTrue(query.GroupBy.IsAll);
        Assert.IsNotNull(query.GroupBy.Having);
        Assert.AreEqual("group by all having Count(Col) > 1", query.GroupBy.ToString());
    }

    [TestMethod]
    public void Parse_WhenGroupByAllMixedWithExplicitField_ShouldThrow()
    {
        var lexer = new Lexer("select Col, Count(Col) from schema.method() group by all, Col", true);
        var parser = new Parser(lexer);

        var exception = Assert.Throws<SyntaxException>(parser.ComposeAll);

        Assert.Contains("GROUP BY ALL cannot be combined", exception.Message);
    }

    [TestMethod]
    public void Parse_WhenAllSelectedOutsideGroupBy_ShouldRemainExpression()
    {
        var query = ParseSingleQuery("select all from schema.method()");

        Assert.IsNull(query.GroupBy);
        Assert.AreEqual("select all from #schema.method()", query.ToString());
    }

    private static QueryNode ParseSingleQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var root = parser.ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;
        return singleSet.Query;
    }
}
