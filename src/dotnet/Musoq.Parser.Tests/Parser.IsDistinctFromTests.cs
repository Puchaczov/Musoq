using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserIsDistinctFromTests
{
    [TestMethod]
    public void IsDistinctFrom_InWhere_ShouldParseAsNullSafeComparison()
    {
        var expression = ParseWhereExpression("City is distinct from Country");

        var node = Assert.IsInstanceOfType<IsDistinctFromNode>(expression);
        Assert.IsFalse(node.IsNegated);
        Assert.AreEqual("City is distinct from Country", node.ToString());
    }

    [TestMethod]
    public void IsNotDistinctFrom_InWhere_ShouldParseAsNegatedNullSafeComparison()
    {
        var expression = ParseWhereExpression("City is not distinct from Country");

        var node = Assert.IsInstanceOfType<IsDistinctFromNode>(expression);
        Assert.IsTrue(node.IsNegated);
        Assert.AreEqual("City is not distinct from Country", node.ToString());
    }

    [TestMethod]
    public void IsDistinctFrom_InSelectExpression_ShouldParse()
    {
        var expression = ParseSingleSelectExpression("City is distinct from Country");

        var node = Assert.IsInstanceOfType<IsDistinctFromNode>(expression);
        Assert.IsFalse(node.IsNegated);
    }

    private static Node ParseWhereExpression(string expression)
    {
        var root = Parse($"select 1 from #some.a() where {expression}");
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;

        return singleSet.Query.Where?.Expression ??
               throw new InvalidOperationException("Expected the parsed query to contain a WHERE clause.");
    }

    private static Node ParseSingleSelectExpression(string expression)
    {
        var root = Parse($"select {expression} from #some.a()");
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;

        return singleSet.Query.Select.Fields[0].Expression;
    }

    private static RootNode Parse(string query)
    {
        return new Parser(new Lexer(query, true)).ComposeAll();
    }
}
