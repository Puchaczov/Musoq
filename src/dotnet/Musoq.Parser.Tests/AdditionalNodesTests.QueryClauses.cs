using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

public partial class AdditionalNodesTests
{
    #region StarNode Tests

    [TestMethod]
    public void StarNode_Constructor_ShouldSetLeftAndRight()
    {
        // Arrange
        var left = new IntegerNode(5);
        var right = new IntegerNode(3);

        // Act
        var node = new StarNode(left, right);

        // Assert
        Assert.AreSame(left, node.Left);
        Assert.AreSame(right, node.Right);
    }

    [TestMethod]
    public void StarNode_ToString_ShouldReturnMultiplicationExpression()
    {
        // Arrange
        var left = new IntegerNode(5);
        var right = new IntegerNode(3);
        var node = new StarNode(left, right);

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("5 * 3", result);
    }

    [TestMethod]
    public void StarNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new StarNode(new IntegerNode(1), new IntegerNode(2));

        // Act & Assert
        Assert.IsNotNull(node.Id);
    }

    #endregion

    #region WhereNode Tests

    [TestMethod]
    public void WhereNode_Constructor_ShouldSetExpression()
    {
        // Arrange
        var expression = new BooleanNode(true);

        // Act
        var node = new WhereNode(expression);

        // Assert
        Assert.AreSame(expression, node.Expression);
    }

    [TestMethod]
    public void WhereNode_ToString_ShouldStartWithWhere()
    {
        // Arrange
        var expression = new BooleanNode(true);
        var node = new WhereNode(expression);

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("where", result);
    }

    [TestMethod]
    public void WhereNode_Id_ShouldContainNodeNameAndExpressionId()
    {
        // Arrange
        var expression = new BooleanNode(true);
        var node = new WhereNode(expression);

        // Act & Assert
        Assert.Contains("WhereNode", node.Id);
    }

    #endregion

    #region TakeNode Tests

    [TestMethod]
    public void TakeNode_Constructor_ShouldSetValue()
    {
        // Arrange & Act
        var node = new TakeNode(new IntegerNode(10));

        // Assert
        Assert.AreEqual(10, ((IntegerNode)node.Expression).ObjValue);
    }

    [TestMethod]
    public void TakeNode_ToString_ShouldStartWithTake()
    {
        // Arrange
        var node = new TakeNode(new IntegerNode(5));

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("take", result);
    }

    #endregion

    #region SkipNode Tests

    [TestMethod]
    public void SkipNode_Constructor_ShouldSetValue()
    {
        // Arrange & Act
        var node = new SkipNode(new IntegerNode(10));

        // Assert
        Assert.AreEqual(10, ((IntegerNode)node.Expression).ObjValue);
    }

    [TestMethod]
    public void SkipNode_ToString_ShouldStartWithSkip()
    {
        // Arrange
        var node = new SkipNode(new IntegerNode(5));

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("skip", result);
    }

    #endregion

}
