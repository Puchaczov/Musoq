using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserNullCoalescingTests
{
    [TestMethod]
    public void WhenNullCoalescingExpression_ShouldParseCoalesceNode()
    {
        var expression = ParseFirstSelectExpression("select a ?? b from #some.a()");

        Assert.IsInstanceOfType<CoalesceNode>(expression);
    }

    [TestMethod]
    public void WhenChainedNullCoalescingExpression_ShouldParseRightAssociative()
    {
        var expression = ParseFirstSelectExpression("select a ?? b ?? c from #some.a()");

        var coalesce = Assert.IsInstanceOfType<CoalesceNode>(expression);
        Assert.IsInstanceOfType<IdentifierNode>(coalesce.Left);
        Assert.IsInstanceOfType<CoalesceNode>(coalesce.Right);
    }

    [TestMethod]
    public void WhenAddBeforeNullCoalescingExpression_ShouldKeepAddOnLeft()
    {
        var expression = ParseFirstSelectExpression("select a + b ?? c from #some.a()");

        var coalesce = Assert.IsInstanceOfType<CoalesceNode>(expression);
        Assert.IsInstanceOfType<AddNode>(coalesce.Left);
        Assert.IsInstanceOfType<IdentifierNode>(coalesce.Right);
    }

    [TestMethod]
    public void WhenParenthesizedNullCoalescingComparedToValue_ShouldParseComparison()
    {
        var expression = ParseFirstSelectExpression("select (a ?? b) = c from #some.a()");

        var equality = Assert.IsInstanceOfType<EqualityNode>(expression);
        Assert.IsInstanceOfType<CoalesceNode>(equality.Left);
        Assert.IsInstanceOfType<IdentifierNode>(equality.Right);
    }

    private static Node ParseFirstSelectExpression(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var root = parser.ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var statement = statements.Statements[0];
        var queryNode = ((SingleSetNode)statement.Node).Query;

        return queryNode.Select.Fields[0].Expression;
    }
}