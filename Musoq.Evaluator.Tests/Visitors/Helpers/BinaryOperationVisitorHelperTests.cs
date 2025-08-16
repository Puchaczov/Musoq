using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class BinaryOperationVisitorHelperTests
{
    [TestMethod]
    public void ProcessStarOperation_WhenTwoNodesOnStack_ShouldCreateStarNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("5");
        var rightNode = new IntegerNode("3");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        BinaryOperationVisitorHelper.ProcessStarOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(StarNode));
        var starNode = (StarNode)result;
        Assert.AreEqual(leftNode, starNode.Left);
        Assert.AreEqual(rightNode, starNode.Right);
    }

    [TestMethod]
    public void ProcessFSlashOperation_WhenTwoNodesOnStack_ShouldCreateFSlashNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("10");
        var rightNode = new IntegerNode("2");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        BinaryOperationVisitorHelper.ProcessFSlashOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(FSlashNode));
        var fSlashNode = (FSlashNode)result;
        Assert.AreEqual(leftNode, fSlashNode.Left);
        Assert.AreEqual(rightNode, fSlashNode.Right);
    }

    [TestMethod]
    public void ProcessModuloOperation_WhenTwoNodesOnStack_ShouldCreateModuloNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("7");
        var rightNode = new IntegerNode("3");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        BinaryOperationVisitorHelper.ProcessModuloOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(ModuloNode));
        var moduloNode = (ModuloNode)result;
        Assert.AreEqual(leftNode, moduloNode.Left);
        Assert.AreEqual(rightNode, moduloNode.Right);
    }

    [TestMethod]
    public void ProcessAddOperation_WhenTwoNodesOnStack_ShouldCreateAddNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("4");
        var rightNode = new IntegerNode("6");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        BinaryOperationVisitorHelper.ProcessAddOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(AddNode));
        var addNode = (AddNode)result;
        Assert.AreEqual(leftNode, addNode.Left);
        Assert.AreEqual(rightNode, addNode.Right);
    }

    [TestMethod]
    public void ProcessHyphenOperation_WhenTwoNodesOnStack_ShouldCreateHyphenNode()
    {
        // Arrange
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("8");
        var rightNode = new IntegerNode("3");
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        // Act
        BinaryOperationVisitorHelper.ProcessHyphenOperation(nodes);

        // Assert
        Assert.AreEqual(1, nodes.Count);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(HyphenNode));
        var hyphenNode = (HyphenNode)result;
        Assert.AreEqual(leftNode, hyphenNode.Left);
        Assert.AreEqual(rightNode, hyphenNode.Right);
    }

    [TestMethod]
    public void ProcessStarOperation_WhenNullStack_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            BinaryOperationVisitorHelper.ProcessStarOperation(null));
    }

    [TestMethod]
    public void ProcessStarOperation_WhenInsufficientNodes_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nodes = new Stack<Node>();
        nodes.Push(new IntegerNode("5")); // Only one node

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            BinaryOperationVisitorHelper.ProcessStarOperation(nodes));
    }
}