using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class ComparisonOperationVisitorHelperTests
{
    [TestMethod]
    public void ProcessEqualityOperation_WhenTwoNodesOnStack_ShouldCreateEqualityNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("5");
        var rightNode = new IntegerNode("5");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        ComparisonOperationVisitorHelper.ProcessEqualityOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(EqualityNode));
        var equalityNode = (EqualityNode)result;
        Assert.AreEqual(leftNode, equalityNode.Left);
        Assert.AreEqual(rightNode, equalityNode.Right);
    }

    [TestMethod]
    public void ProcessGreaterOrEqualOperation_WhenTwoNodesOnStack_ShouldCreateGreaterEqualNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("10");
        var rightNode = new IntegerNode("5");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        ComparisonOperationVisitorHelper.ProcessGreaterOrEqualOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(GreaterOrEqualNode));
        var greaterEqualNode = (GreaterOrEqualNode)result;
        Assert.AreEqual(leftNode, greaterEqualNode.Left);
        Assert.AreEqual(rightNode, greaterEqualNode.Right);
    }

    [TestMethod]
    public void ProcessLessOrEqualOperation_WhenTwoNodesOnStack_ShouldCreateLessEqualNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("3");
        var rightNode = new IntegerNode("7");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        ComparisonOperationVisitorHelper.ProcessLessOrEqualOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(LessOrEqualNode));
        var lessEqualNode = (LessOrEqualNode)result;
        Assert.AreEqual(leftNode, lessEqualNode.Left);
        Assert.AreEqual(rightNode, lessEqualNode.Right);
    }

    [TestMethod]
    public void ProcessGreaterOperation_WhenTwoNodesOnStack_ShouldCreateGreaterNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("8");
        var rightNode = new IntegerNode("3");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        ComparisonOperationVisitorHelper.ProcessGreaterOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(GreaterNode));
        var greaterNode = (GreaterNode)result;
        Assert.AreEqual(leftNode, greaterNode.Left);
        Assert.AreEqual(rightNode, greaterNode.Right);
    }

    [TestMethod]
    public void ProcessLessOperation_WhenTwoNodesOnStack_ShouldCreateLessNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("2");
        var rightNode = new IntegerNode("9");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        ComparisonOperationVisitorHelper.ProcessLessOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(LessNode));
        var lessNode = (LessNode)result;
        Assert.AreEqual(leftNode, lessNode.Left);
        Assert.AreEqual(rightNode, lessNode.Right);
    }

    [TestMethod]
    public void ProcessDiffOperation_WhenTwoNodesOnStack_ShouldCreateDiffNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("4");
        var rightNode = new IntegerNode("6");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        ComparisonOperationVisitorHelper.ProcessDiffOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(DiffNode));
        var diffNode = (DiffNode)result;
        Assert.AreEqual(leftNode, diffNode.Left);
        Assert.AreEqual(rightNode, diffNode.Right);
    }

    [TestMethod]
    public void ProcessLikeOperation_WhenTwoNodesOnStack_ShouldCreateLikeNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new StringNode("Hello World");
        var rightNode = new StringNode("Hello%");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        ComparisonOperationVisitorHelper.ProcessLikeOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(LikeNode));
        var likeNode = (LikeNode)result;
        Assert.AreEqual(leftNode, likeNode.Left);
        Assert.AreEqual(rightNode, likeNode.Right);
    }
}