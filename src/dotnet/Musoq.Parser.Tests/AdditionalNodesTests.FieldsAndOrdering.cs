using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

public partial class AdditionalNodesTests
{
    #region FieldNode Tests

    [TestMethod]
    public void FieldNode_ConstructorWithExplicitName_ShouldTrackExplicitFieldName()
    {
        var node = new FieldNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, "alias");

        Assert.AreEqual("alias", node.FieldName);
        Assert.IsTrue(node.HasExplicitFieldName);
    }

    [TestMethod]
    public void FieldNode_ConstructorWithDerivedName_ShouldKeepDisplayNameWithoutExplicitAlias()
    {
        var node = new FieldNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, "col1", false);

        Assert.AreEqual("col1", node.FieldName);
        Assert.IsFalse(node.HasExplicitFieldName);
    }

    #endregion

    #region GroupByNode Tests

    [TestMethod]
    public void GroupByNode_Constructor_ShouldSetFields()
    {
        // Arrange
        var fields = new[]
        {
            new FieldNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, "col1"),
            new FieldNode(new AccessColumnNode("col2", string.Empty, TextSpan.Empty), 1, "col2")
        };

        // Act
        var node = new GroupByNode(fields, null);

        // Assert
        Assert.HasCount(2, node.Fields);
    }

    [TestMethod]
    public void GroupByNode_ToString_ShouldStartWithGroupBy()
    {
        // Arrange
        var fields = new[]
        {
            new FieldNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, "col1")
        };
        var node = new GroupByNode(fields, null);

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("group by", result);
    }

    [TestMethod]
    public void GroupByNode_WhenAll_ShouldStringifyAsGroupByAll()
    {
        var node = new GroupByNode([], null, true);

        Assert.IsTrue(node.IsAll);
        Assert.AreEqual("group by all", node.ToString());
    }

    #endregion

    #region HavingNode Tests

    [TestMethod]
    public void HavingNode_Constructor_ShouldSetExpression()
    {
        // Arrange
        var expression = new BooleanNode(true);

        // Act
        var node = new HavingNode(expression);

        // Assert
        Assert.AreSame(expression, node.Expression);
    }

    [TestMethod]
    public void HavingNode_ToString_ShouldStartWithHaving()
    {
        // Arrange
        var expression = new BooleanNode(true);
        var node = new HavingNode(expression);

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("having", result);
    }

    #endregion

    #region AccessColumnNode Tests

    [TestMethod]
    public void AccessColumnNode_Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var node = new AccessColumnNode("columnName", "alias", TextSpan.Empty);

        // Assert
        Assert.AreEqual("columnName", node.Name);
        Assert.AreEqual("alias", node.Alias);
    }

    [TestMethod]
    public void AccessColumnNode_ToString_ShouldReturnColumnName()
    {
        // Arrange
        var node = new AccessColumnNode("myColumn", string.Empty, TextSpan.Empty);

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("myColumn", result);
    }

    [TestMethod]
    public void AccessColumnNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new AccessColumnNode("col", "", TextSpan.Empty);

        // Act & Assert
        Assert.Contains("AccessColumnNode", node.Id);
    }

    #endregion

    #region BetweenNode Tests

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

    #endregion

    #region OrderByNode Tests

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

    #endregion
}
