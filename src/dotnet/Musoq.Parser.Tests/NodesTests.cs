using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class NodesTests
{
    [TestMethod]
    public void WhenBetweenNode_ShouldReturnString()
    {
        var expression = new IntegerNode("5");
        var min = new IntegerNode("1");
        var max = new IntegerNode("10");
        var node = new BetweenNode(expression, min, max);

        Assert.AreEqual("5 between 1 and 10", node.ToString());
    }

    [TestMethod]
    public void WhenBetweenNodeWithStrings_ShouldReturnString()
    {
        var expression = new StringNode("value");
        var min = new StringNode("A");
        var max = new StringNode("Z");
        var node = new BetweenNode(expression, min, max);

        Assert.AreEqual("'value' between 'A' and 'Z'", node.ToString());
    }

    [TestMethod]
    public void WhenBetweenNodeReturnType_ShouldBeBoolean()
    {
        var expression = new IntegerNode("5");
        var min = new IntegerNode("1");
        var max = new IntegerNode("10");
        var node = new BetweenNode(expression, min, max);

        Assert.AreEqual(typeof(bool), node.ReturnType);
    }

    [TestMethod]
    public void WhenOrderByNode_ShouldReturnString()
    {
        var node = new OrderByNode([
            new FieldOrderedNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, null, Order.Ascending)
        ]);

        Assert.AreEqual("order by col1", node.ToString());
    }

    [TestMethod]
    public void WhenOrderByDescendingNode_ShouldReturnString()
    {
        var node = new OrderByNode([
            new FieldOrderedNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, null, Order.Descending)
        ]);

        Assert.AreEqual("order by col1 desc", node.ToString());
    }

    [TestMethod]
    public void WhenOrderByMultipleNodes_ShouldReturnString()
    {
        var node = new OrderByNode([
            new FieldOrderedNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, null, Order.Ascending),
            new FieldOrderedNode(new AccessColumnNode("col2", string.Empty, TextSpan.Empty), 1, null, Order.Ascending)
        ]);

        Assert.AreEqual("order by col1, col2", node.ToString());
    }

    [TestMethod]
    public void WhenOrderByMultipleNodesWithDifferentOrder_ShouldReturnString()
    {
        var node = new OrderByNode([
            new FieldOrderedNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, null, Order.Ascending),
            new FieldOrderedNode(new AccessColumnNode("col2", string.Empty, TextSpan.Empty), 1, null, Order.Descending)
        ]);

        Assert.AreEqual("order by col1, col2 desc", node.ToString());
    }
}
